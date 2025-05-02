// using Domain.Entities;
// using Domain.Enums;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using System.Net;
// using System.Net.Mail;
// using System.Text;
// using Domain.DTOs.Notification;
// using Domain.Filters;
//
// namespace Infrastructure.Services.NotificationService;
//
// public class NotificationService : INotificationService
// {
//     private readonly IConfiguration _configuration;
//     private readonly DataContext _context;
//     private readonly HttpClient _httpClient;
//
//     public NotificationService(IConfiguration configuration, DataContext context)
//     {
//         _configuration = configuration;
//         _context = context;
//         _httpClient = new HttpClient();
//     }
//
//     public async Task<Response<string>> SendEmailAsync(string toEmail, string subject, string message)
//     {
//         try
//         {
//             var emailFrom = _configuration["EmailSettings:From"] ?? "";
//             var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
//             var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
//             var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
//             
//             var mail = new MailMessage
//             {
//                 From = new MailAddress(emailFrom),
//                 Subject = subject,
//                 Body = message,
//                 IsBodyHtml = true
//             };
//
//             mail.To.Add(toEmail);
//
//             using var smtp = new SmtpClient(smtpHost, smtpPort)
//             {
//                 EnableSsl = true,
//                 Credentials = new NetworkCredential(emailFrom, smtpPassword)
//             };
//
//             await smtp.SendMailAsync(mail);
//             
//             // Запись лога уведомления
//             await LogNotification(subject, message, NotificationType.General, null, null, null, true, false, true);
//
//             return new Response<string>
//             {
//                 StatusCode = 200,
//                 Message = "Паёми почтаи электронӣ бо муваффақият фиристода шуд",
//                 Data = "Success"
//             };
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification(subject, message, NotificationType.General, null, null, null, true, false, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани почтаи электронӣ: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendTelegramMessageAsync(string chatId, string message)
//     {
//         try
//         {
//             var telegramBotToken = _configuration["TelegramSettings:BotToken"] ?? "";
//             
//             if (string.IsNullOrEmpty(telegramBotToken))
//                 return new Response<string>
//                 {
//                     StatusCode = 500,
//                     Message = "Токени боти Telegram танзим нашудааст",
//                     Data = null
//                 };
//
//             var apiUrl = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage";
//             
//             var parameters = new Dictionary<string, string>
//             {
//                 { "chat_id", chatId },
//                 { "text", message },
//                 { "parse_mode", "HTML" }
//             };
//
//             var content = new FormUrlEncodedContent(parameters);
//             var response = await _httpClient.PostAsync(apiUrl, content);
//             
//             if (response.IsSuccessStatusCode)
//             {
//                 // Запись лога уведомления
//                 await LogNotification("Telegram message", message, NotificationType.General, null, null, null, false, true, true);
//                 
//                 return new Response<string>
//                 {
//                     StatusCode = 200,
//                     Message = "Паёми Telegram бо муваффақият фиристода шуд",
//                     Data = "Success"
//                 };
//             }
//             else
//             {
//                 var responseContent = await response.Content.ReadAsStringAsync();
//                 
//                 // Запись лога ошибки
//                 await LogNotification("Telegram message", message, NotificationType.General, null, null, null, false, true, false, responseContent);
//                 
//                 return new Response<string>
//                 {
//                     StatusCode = (int)response.StatusCode,
//                     Message = $"Хатогӣ ҳангоми фиристодани паёми Telegram: {responseContent}",
//                     Data = null
//                 };
//             }
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification("Telegram message", ex.Message, NotificationType.General, null, null, null, false, true, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани паёми Telegram: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendStudentNotificationAsync(int studentId, string subject, string message, bool sendEmail = true, bool sendTelegram = true)
//     {
//         try
//         {
//             var student = await _context.Students
//                 .Include(s => s.User)
//                 .FirstOrDefaultAsync(s => s.Id == studentId);
//                 
//             if (student == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Донишҷӯ ёфт нашуд",
//                     Data = null
//                 };
//
//             var success = true;
//             var errorMessage = "";
//
//             if (sendEmail && !string.IsNullOrEmpty(student.Email) && (student.User?.EmailNotificationsEnabled ?? true))
//             {
//                 var emailResult = await SendEmailAsync(student.Email, subject, message);
//                 if (emailResult.StatusCode != 200)
//                 {
//                     success = false;
//                     errorMessage += emailResult.Message + "; ";
//                 }
//             }
//
//             if (sendTelegram && !string.IsNullOrEmpty(student.User?.TelegramChatId) && (student.User?.TelegramNotificationsEnabled ?? true))
//             {
//                 var telegramResult = await SendTelegramMessageAsync(student.User.TelegramChatId, message);
//                 if (telegramResult.StatusCode != 200)
//                 {
//                     success = false;
//                     errorMessage += telegramResult.Message;
//                 }
//             }
//
//             // Запись лога уведомления
//             await LogNotification(subject, message, NotificationType.General, studentId, null, null, sendEmail, sendTelegram, success, errorMessage);
//
//             if (success)
//             {
//                 return new Response<string>
//                 {
//                     StatusCode = 200,
//                     Message = "Огоҳинома бо муваффақият фиристода шуд",
//                     Data = "Success"
//                 };
//             }
//             else
//             {
//                 return new Response<string>
//                 {
//                     StatusCode = 500,
//                     Message = $"Хатогӣ ҳангоми фиристодани огоҳинома: {errorMessage}",
//                     Data = null
//                 };
//             }
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification(subject, message, NotificationType.General, studentId, null, null, sendEmail, sendTelegram, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани огоҳинома: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendGroupNotificationAsync(int groupId, string subject, string message, bool sendEmail = true, bool sendTelegram = true)
//     {
//         try
//         {
//             var group = await _context.Groups
//                 .Include(g => g.StudentGroups)
//                 .ThenInclude(sg => sg.Student)
//                 .ThenInclude(s => s.User)
//                 .FirstOrDefaultAsync(g => g.Id == groupId);
//                 
//             if (group == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Гурӯҳ ёфт нашуд",
//                     Data = null
//                 };
//
//             var success = true;
//             var errorMessage = "";
//             var taskResults = new List<Task<Response<string>>>();
//
//             foreach (var studentGroup in group.StudentGroups)
//             {
//                 var student = studentGroup.Student;
//                 
//                 if (sendEmail && !string.IsNullOrEmpty(student.Email) && (student.User?.EmailNotificationsEnabled ?? true))
//                 {
//                     taskResults.Add(SendEmailAsync(student.Email, subject, message));
//                 }
//
//                 if (sendTelegram && !string.IsNullOrEmpty(student.User?.TelegramChatId) && (student.User?.TelegramNotificationsEnabled ?? true))
//                 {
//                     taskResults.Add(SendTelegramMessageAsync(student.User.TelegramChatId, message));
//                 }
//             }
//
//             var results = await Task.WhenAll(taskResults);
//             
//             foreach (var result in results)
//             {
//                 if (result.StatusCode != 200)
//                 {
//                     success = false;
//                     errorMessage += result.Message + "; ";
//                 }
//             }
//
//             // Запись лога уведомления
//             await LogNotification(subject, message, NotificationType.General, null, groupId, null, sendEmail, sendTelegram, success, errorMessage);
//
//             if (success)
//             {
//                 return new Response<string>
//                 {
//                     StatusCode = 200,
//                     Message = "Огоҳиномаҳо ба гурӯҳ бо муваффақият фиристода шуданд",
//                     Data = "Success"
//                 };
//             }
//             else
//             {
//                 return new Response<string>
//                 {
//                     StatusCode = 500,
//                     Message = $"Хатогӣ ҳангоми фиристодани огоҳиномаҳо ба гурӯҳ: {errorMessage}",
//                     Data = null
//                 };
//             }
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification(subject, message, NotificationType.General, null, groupId, null, sendEmail, sendTelegram, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани огоҳиномаҳо ба гурӯҳ: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendPaymentReminderAsync(int studentId)
//     {
//         try
//         {
//             var student = await _context.Students
//                 .Include(s => s.User)
//                 .FirstOrDefaultAsync(s => s.Id == studentId);
//                 
//             if (student == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Донишҷӯ ёфт нашуд",
//                     Data = null
//                 };
//
//             var subject = "Ёдоварии пардохт";
//             var message = $@"
//                 <h2>Ёдоварии пардохт</h2>
//                 <p>Салом, {student.FullName}!</p>
//                 <p>Ба шумо хотиррасон мекунем, ки мӯҳлати пардохти навбатии шумо наздик аст:</p>
//                 <p><strong>Мӯҳлати пардохт:</strong> {student.NextPaymentDueDate?.ToString("dd.MM.yyyy")}</p>
//                 <p>Лутфан, пардохти худро сари вақт иҷро кунед, то раванди таҳсилатон бефосила идома ёбад.</p>
//                 <p>Бо эҳтиром,<br>Маркази таълимӣ</p>
//             ";
//
//             var result = await SendStudentNotificationAsync(studentId, subject, message);
//             
//             // Запись лога уведомления
//             await LogNotification(subject, message, NotificationType.PaymentReminder, studentId, null, null, true, true, 
//                 result.StatusCode == 200, result.StatusCode != 200 ? result.Message : null);
//
//             return result;
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification("Ёдоварии пардохт", ex.Message, NotificationType.PaymentReminder, studentId, null, null, true, true, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани ёдоварии пардохт: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendGradeNotificationAsync(Student student, Grade grade)
//     {
//         try
//         {
//             if (student == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Донишҷӯ ёфт нашуд",
//                     Data = null
//                 };
//
//             var subject = "Огоҳиномаи баҳои нав";
//             var message = $@"
//                 <h2>Огоҳиномаи баҳои нав</h2>
//                 <p>Салом, {student.FullName}!</p>
//                 <p>Ба шумо баҳои нав гузошта шуд:</p>
//                 <p><strong>Гурӯҳ:</strong> {grade.Group?.Name}</p>
//                 <p><strong>Дарс:</strong> Ҳафтаи {grade.WeekIndex}, рӯзи {grade.Lesson?.DayOfWeekIndex}</p>
//                 <p><strong>Баҳо:</strong> {grade.Value}</p>
//                 <p><strong>Бонусҳо:</strong> {grade.BonusPoints}</p>
//                 <p><strong>Тафсири муаллим:</strong> {grade.Comment}</p>
//                 <p>Бо эҳтиром,<br>Маркази таълимӣ</p>
//             ";
//
//             var result = await SendStudentNotificationAsync(student.Id, subject, message);
//             
//             // Запись лога уведомления
//             await LogNotification(subject, message, NotificationType.GradeNotification, student.Id, grade.GroupId, null, true, true, 
//                 result.StatusCode == 200, result.StatusCode != 200 ? result.Message : null);
//
//             return result;
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             var studentId = student?.Id ?? 0;
//             var groupId = grade?.GroupId ?? 0;
//             
//             await LogNotification("Огоҳиномаи баҳои нав", ex.Message, NotificationType.GradeNotification, studentId, groupId, null, true, true, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани огоҳиномаи баҳои нав: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendAttendanceReportAsync(int studentId, int weekIndex)
//     {
//         try
//         {
//             var student = await _context.Students
//                 .Include(s => s.Attendances.Where(a => a.Lesson.WeekIndex == weekIndex))
//                 .ThenInclude(a => a.Lesson)
//                 .Include(s => s.StudentGroups)
//                 .ThenInclude(sg => sg.Group)
//                 .FirstOrDefaultAsync(s => s.Id == studentId);
//                 
//             if (student == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Донишҷӯ ёфт нашуд",
//                     Data = null
//                 };
//
//             var presentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present);
//             var absentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Absent);
//             var lateCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Late);
//             var totalCount = student.Attendances.Count;
//             var attendanceRate = totalCount > 0 ? (double)presentCount / totalCount * 100 : 0;
//
//             var attendanceDetails = new StringBuilder();
//             foreach (var attendance in student.Attendances.OrderBy(a => a.Lesson.DayOfWeekIndex))
//             {
//                 var status = attendance.Status switch
//                 {
//                     AttendanceStatus.Present => "Ҳозир",
//                     AttendanceStatus.Absent => "Ғоиб",
//                     AttendanceStatus.Late => "Дер",
//                     _ => "Номуайян"
//                 };
//                 
//                 attendanceDetails.AppendLine($"<tr><td>Рӯзи {attendance.Lesson.DayOfWeekIndex}</td><td>{status}</td></tr>");
//             }
//
//             var groups = student.StudentGroups.Select(sg => sg.Group.Name).ToList();
//             var groupsText = string.Join(", ", groups);
//
//             var subject = $"Ҳисоботи ҳафтагии ҳузур барои ҳафтаи {weekIndex}";
//             var message = $@"
//                 <h2>Ҳисоботи ҳафтагии ҳузур</h2>
//                 <p>Салом, {student.FullName}!</p>
//                 <p>Ҳисоботи ҳузури шумо барои ҳафтаи {weekIndex}:</p>
//                 <p><strong>Гурӯҳҳо:</strong> {groupsText}</p>
//                 <p><strong>Шумораи ҳозирӣ:</strong> {presentCount} аз {totalCount} дарс ({attendanceRate:F1}%)</p>
//                 <p><strong>Шумораи ғоибӣ:</strong> {absentCount}</p>
//                 <p><strong>Шумораи деромадан:</strong> {lateCount}</p>
//                 
//                 <h3>Тафсилоти ҳозирӣ:</h3>
//                 <table border='1' cellpadding='5' style='border-collapse: collapse;'>
//                 <tr><th>Дарс</th><th>Вазъият</th></tr>
//                 {attendanceDetails}
//                 </table>
//                 
//                 <p>Бо эҳтиром,<br>Маркази таълимӣ</p>
//             ";
//
//             var result = await SendStudentNotificationAsync(studentId, subject, message);
//             
//             // Запись лога уведомления
//             await LogNotification(subject, "Ҳисоботи ҳафтагии ҳузур", NotificationType.AttendanceReport, studentId, null, null, true, true, 
//                 result.StatusCode == 200, result.StatusCode != 200 ? result.Message : null);
//
//             return result;
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification("Ҳисоботи ҳафтагии ҳузур", ex.Message, NotificationType.AttendanceReport, studentId, null, null, true, true, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани ҳисоботи ҳафтагии ҳузур: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public async Task<Response<string>> SendExamNotificationAsync(int studentId, int examId)
//     {
//         try
//         {
//             var exam = await _context.Exams
//                 .Include(e => e.Student)
//                 .Include(e => e.Group)
//                 .FirstOrDefaultAsync(e => e.Id == examId && e.StudentId == studentId);
//                 
//             if (exam == null)
//                 return new Response<string>
//                 {
//                     StatusCode = 404,
//                     Message = "Имтиҳон ёфт нашуд",
//                     Data = null
//                 };
//
//             var examType = exam.IsWeeklyExam ? "ҳафтагӣ" : (exam.IsFinalExam ? "ниҳоӣ" : "муқаррарӣ");
//
//             var subject = $"Натиҷаи имтиҳони {examType}";
//             var message = $@"
//                 <h2>Натиҷаи имтиҳон</h2>
//                 <p>Салом, {exam.Student.FullName}!</p>
//                 <p>Натиҷаи имтиҳони {examType} барои ҳафтаи {exam.WeekIndex}:</p>
//                 <p><strong>Гурӯҳ:</strong> {exam.Group.Name}</p>
//                 <p><strong>Баҳо:</strong> {exam.Value}</p>
//                 <p><strong>Бонусҳо:</strong> {exam.BonusPoints}</p>
//                 <p><strong>Тафсир:</strong> {exam.Comment}</p>
//                 <p>Бо эҳтиром,<br>Маркази таълимӣ</p>
//             ";
//
//             var result = await SendStudentNotificationAsync(studentId, subject, message);
//             
//             // Запись лога уведомления
//             await LogNotification(subject, "Натиҷаи имтиҳон", NotificationType.ExamNotification, studentId, exam.GroupId, null, true, true, 
//                 result.StatusCode == 200, result.StatusCode != 200 ? result.Message : null);
//
//             return result;
//         }
//         catch (Exception ex)
//         {
//             // Запись лога ошибки
//             await LogNotification("Натиҷаи имтиҳон", ex.Message, NotificationType.ExamNotification, studentId, null, null, true, true, false, ex.Message);
//             
//             return new Response<string>
//             {
//                 StatusCode = 500,
//                 Message = $"Хатогӣ ҳангоми фиристодани огоҳиномаи натиҷаи имтиҳон: {ex.Message}",
//                 Data = null
//             };
//         }
//     }
//
//     public Task<Response<List<NotificationDto>>> GetNotificationsAsync()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<Response<List<NotificationDto>>> GetStudentNotificationsAsync(int studentId)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<Response<List<NotificationDto>>> GetNotificationsByTypeAsync(NotificationType type)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<Response<string>> SendBatchNotificationsAsync(List<int> recipientIds, string subject, string message, NotificationType type)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<PaginationResponse<List<NotificationDto>>> GetNotificationsPaginatedAsync(NotificationFilter filter)
//     {
//         throw new NotImplementedException();
//     }
//
//     private async Task LogNotification(string subject, string message, NotificationType type, int? studentId, int? groupId, int? centerId, 
//         bool sentByEmail, bool sentByTelegram, bool isSuccessful, string? errorMessage = null)
//     {
//         try
//         {
//             var log = new NotificationLog
//             {
//                 Subject = subject,
//                 Message = message,
//                 Type = type,
//                 StudentId = studentId,
//                 GroupId = groupId,
//                 CenterId = centerId,
//                 SentByEmail = sentByEmail,
//                 SentByTelegram = sentByTelegram,
//                 IsSuccessful = isSuccessful,
//                 ErrorMessage = errorMessage,
//                 SentDateTime = DateTime.Now
//             };
//
//             _context.NotificationLogs.Add(log);
//             await _context.SaveChangesAsync();
//         }
//         catch
//         {
//             // Игнорируем ошибки логирования
//         }
//     }
// }
