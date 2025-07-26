// using ClosedXML.Excel;
// using Domain.DTOs.Mentor;
// using Infrastructure.Data;
// using Microsoft.EntityFrameworkCore;
// using Infrastructure.Helpers;
// using Microsoft.AspNetCore.Http;
//
// namespace Infrastructure.Services.ExportToExel;
//
// public class MentorExportService : IMentorExportService
// {
//     private readonly DataContext _context;
//     private readonly IHttpContextAccessor _httpContextAccessor;
//
//     public MentorExportService(DataContext context, IHttpContextAccessor httpContextAccessor)
//     {
//         _context = context;
//         _httpContextAccessor = httpContextAccessor;
//     }
//
//     public async Task<byte[]> ExportAllMentorsToExcelAsync()
//     {
//         var mentorsQuery = _context.Mentors
//             .Include(m => m.Groups)
//             .Include(m => m.Center)
//             .Include(m => m.User)
//             .Where(m => !m.IsDeleted);
//         mentorsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
//             mentorsQuery, _httpContextAccessor, m => m.CenterId);
//         var mentors = await mentorsQuery.ToListAsync();
//
//         using var workbook = new XLWorkbook();
//         var sheet = workbook.Worksheets.Add("Mentors");
//
//         string[] headers =
//         {
//             "№", "ID", "Full Name", "Email", "Phone", "Address", "Birthday", "Age", "Gender", "Experience", "Salary",
//             "Active Status", "Payment Status", "Center", "Groups"
//         };
//
//         for (int i = 0; i < headers.Length; i++)
//         {
//             sheet.Cell(1, i + 1).Value = headers[i];
//             sheet.Cell(1, i + 1).Style.Font.Bold = true;
//             sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
//             if (headers[i] is "Full Name" or "Email" or "Phone" or "Address" or "Groups")
//                 sheet.Column(i + 1).Width = 25;
//             else
//                 sheet.Column(i + 1).AdjustToContents();
//         }
//
//         int row = 2;
//         int index = 1;
//         foreach (var m in mentors)
//         {
//             var dto = new GetMentorDto
//             {
//                 Id = m.Id,
//                 FullName = m.User?.FullName ?? m.FullName,
//                 Email = m.User?.Email ?? m.Email,
//                 Phone = m.User?.PhoneNumber ?? m.PhoneNumber,
//                 Address = m.Address,
//                 Birthday = m.Birthday,
//                 Age = m.Age,
//                 Gender = m.Gender,
//                 Experience = m.Experience,
//                 PaymentStatus = m.PaymentStatus,
//                 ActiveStatus = m.ActiveStatus,
//                 Salary = m.Salary,
//                 CenterId = m.CenterId,
//                 Role = null,
//                 UserId = m.UserId
//             };
//             sheet.Cell(row, 1).Value = index++;
//             sheet.Cell(row, 2).Value = dto.Id;
//             sheet.Cell(row, 3).Value = dto.FullName;
//             sheet.Cell(row, 4).Value = dto.Email;
//             sheet.Cell(row, 5).Value = dto.Phone;
//             sheet.Cell(row, 6).Value = dto.Address;
//             sheet.Cell(row, 7).Value = dto.Birthday.ToString("yyyy-MM-dd");
//             sheet.Cell(row, 8).Value = dto.Age;
//             sheet.Cell(row, 9).Value = dto.Gender.ToString();
//             sheet.Cell(row, 10).Value = dto.Experience;
//             sheet.Cell(row, 11).Value = dto.Salary;
//             // Active Status with color
//             var activeCell = sheet.Cell(row, 12);
//             activeCell.Value = dto.ActiveStatus.ToString();
//             if (dto.ActiveStatus == Domain.Enums.ActiveStatus.Active)
//                 activeCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
//             else if (dto.ActiveStatus == Domain.Enums.ActiveStatus.Completed)
//                 activeCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
//             else
//                 activeCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
//             // Payment Status with color
//             var paymentCell = sheet.Cell(row, 13);
//             paymentCell.Value = dto.PaymentStatus.ToString();
//             if (dto.PaymentStatus == Domain.Enums.PaymentStatus.Completed)
//                 paymentCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
//             else if (dto.PaymentStatus == Domain.Enums.PaymentStatus.Failed)
//                 paymentCell.Style.Fill.BackgroundColor = XLColor.LightPink;
//             else
//                 paymentCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
//             sheet.Cell(row, 14).Value = m.Center?.Name ?? "";
//             // Groups as comma separated
//             var groupNames = m.Groups?.Select(g => g.Name).ToList() ?? new();
//             sheet.Cell(row, 15).Value = string.Join(", ", groupNames);
//             // Borders
//             for (int col = 1; col <= headers.Length; col++)
//             {
//                 sheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                 sheet.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//             }
//
//             row++;
//         }
//
//         // Mentor Groups sheet (только те группы, где ментор действительно преподаёт)
//         var groupsWithMentors = await _context.Groups
//             .Include(g => g.Mentor).ThenInclude(m => m.User)
//             .Include(g => g.Course)
//             .Where(g => g.MentorId != 0 && g.Mentor != null && g.Course != null && !g.Mentor.IsDeleted)
//             .ToListAsync();
//         var mentorGroupSheet = workbook.Worksheets.Add("Mentor Groups");
//         string[] mgHeaders =
//         {
//             "№", "Mentor ID", "Full Name", "Group ID", "Group Name", "Is Active", "Course Name", "Course Duration",
//             "Course Price"
//         };
//         for (int i = 0; i < mgHeaders.Length; i++)
//         {
//             mentorGroupSheet.Cell(1, i + 1).Value = mgHeaders[i];
//             mentorGroupSheet.Cell(1, i + 1).Style.Font.Bold = true;
//             mentorGroupSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
//             mentorGroupSheet.Column(i + 1).AdjustToContents();
//         }
//
//         int mgRow = 2;
//         int mgIndex = 1;
//         foreach (var g in groupsWithMentors)
//         {
//             var mentor = g.Mentor;
//             var fullName = mentor?.User?.FullName ?? mentor?.FullName ?? "";
//             mentorGroupSheet.Cell(mgRow, 1).Value = mgIndex++;
//             mentorGroupSheet.Cell(mgRow, 2).Value = mentor?.Id ?? 0;
//             mentorGroupSheet.Cell(mgRow, 3).Value = fullName;
//             mentorGroupSheet.Cell(mgRow, 4).Value = g.Id;
//             mentorGroupSheet.Cell(mgRow, 5).Value = g.Name;
//             mentorGroupSheet.Cell(mgRow, 6).Value =
//                 g.Status == Domain.Enums.ActiveStatus.Active ? "Active" : g.Status.ToString();
//             mentorGroupSheet.Cell(mgRow, 7).Value = g.Course?.CourseName ?? "";
//             mentorGroupSheet.Cell(mgRow, 8).Value = g.Course?.DurationInMonth ?? 0;
//             mentorGroupSheet.Cell(mgRow, 9).Value = g.Course?.Price ?? 0;
//             for (int col = 1; col <= mgHeaders.Length; col++)
//             {
//                 mentorGroupSheet.Cell(mgRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                 mentorGroupSheet.Cell(mgRow, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//             }
//
//             mgRow++;
//         }
//
//         using var stream = new MemoryStream();
//         workbook.SaveAs(stream);
//         return stream.ToArray();
//     }
// }