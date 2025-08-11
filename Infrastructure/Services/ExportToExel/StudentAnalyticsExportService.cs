using ClosedXML.Excel;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Infrastructure.Services.ExportToExel
{
    public class StudentAnalyticsExportService : IStudentAnalyticsExportService
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StudentAnalyticsExportService(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<byte[]> ExportStudentAnalyticsToExcelAsync(int? month = null, int? year = null)
        {
            // ===== подготовка данных =====
            var studentsQuery = _context.Students
                .Include(s => s.StudentGroups).ThenInclude(sg => sg.Group)
                .Include(s => s.Center)
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, _httpContextAccessor, s => s.CenterId);

            var students = await studentsQuery.ToListAsync();

            DateTimeOffset? from = null, to = null;
            if (month.HasValue && year.HasValue)
            {
                var startLocal = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Unspecified);
                from = new DateTimeOffset(startLocal, TimeSpan.Zero);
                to = from.Value.AddMonths(1);
            }

            var titleBg = XLColor.FromArgb(34, 40, 49);        
            var headerBg = XLColor.FromArgb(245, 247, 250);   
            var borderColor = XLColor.FromArgb(220, 224, 230);
            var stripedRow = XLColor.FromArgb(250, 251, 253); 
            var textDark = XLColor.FromArgb(34, 40, 49);      

            using var wb = new XLWorkbook();

            string centerTitle = "Все центры";
            var centerId = UserContextHelper.GetCurrentUserCenterId(_httpContextAccessor);
            if (centerId.HasValue)
            {
                var center = await _context.Centers.FirstOrDefaultAsync(c => c.Id == centerId.Value && !c.IsDeleted);
                centerTitle = center?.Name ?? $"Center #{centerId.Value}";
            }

            string scopeTitle = from.HasValue ? $"Месяц: {year:D4}-{month:D2}" : "За весь период";

            void ApplyTitle(IXLRange titleRange, string text)
            {
                titleRange.Merge();
                titleRange.Value = text;
                titleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                titleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                titleRange.Style.Font.SetBold();
                titleRange.Style.Font.SetFontSize(13);
                titleRange.Style.Font.SetFontColor(XLColor.White);
                titleRange.Style.Fill.SetBackgroundColor(titleBg);
                titleRange.Style.Border.TopBorder = XLBorderStyleValues.None;
                titleRange.Style.Border.BottomBorder = XLBorderStyleValues.None;
            }

            void ApplyHeader(IXLCell cell, string text)
            {
                cell.Value = text;
                cell.Style.Font.SetBold();
                cell.Style.Font.SetFontSize(10);
                cell.Style.Font.SetFontColor(textDark); 
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                cell.Style.Fill.SetBackgroundColor(headerBg);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = borderColor;
                cell.Style.Alignment.WrapText = true;
            }

            void ApplyDataCell(IXLCell cell)
            {
                cell.Style.Font.SetFontSize(10);
                cell.Style.Font.SetFontColor(textDark); 
                cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = borderColor;
            }

            void ApplyNumericCell(IXLCell cell, string format)
            {
                ApplyDataCell(cell);
                cell.Style.NumberFormat.Format = format;
            }

            var studentsSheet = wb.Worksheets.Add("Студенты");
            var studentsHeaders = new[]
            {
                "№", "ФИО", "Email", "Телефон", "Центр", "Пол", "Возраст", "Дата рождения", "Адрес",
                "Статус оплаты", "Статус активности", "Итого оплачено", "Последняя оплата", "Следующий платёж",
                "Кол-во групп", "Группы", "Средняя оценка по группам", "Присутствий", "Отсутствий", "Опозданий",
                "Посещаемость", "Период"
            };

            // title row
            var titleRangeStudents = studentsSheet.Range(1, 1, 1, studentsHeaders.Length);
            ApplyTitle(titleRangeStudents, $"Аналитика студентов — {centerTitle} — {scopeTitle}");
            studentsSheet.Row(1).Height = 26;

            // header row
            for (int i = 0; i < studentsHeaders.Length; i++)
            {
                ApplyHeader(studentsSheet.Cell(2, i + 1), studentsHeaders[i]);
            }
            studentsSheet.Row(2).Height = 24;

            
            int row = 3;
            int index = 1;
            foreach (var s in students)
            {
                var groupIds = s.StudentGroups.Select(sg => sg.GroupId).Distinct().ToList();
                var groups = await _context.Groups.Where(g => groupIds.Contains(g.Id) && !g.IsDeleted).ToListAsync();

                var groupAvgParts = new List<string>();
                foreach (var g in groups)
                {
                    var entriesQuery = _context.JournalEntries
                        .Where(e => e.StudentId == s.Id && e.Journal!.GroupId == g.Id && !e.IsDeleted)
                        .Include(e => e.Journal);

                    if (from.HasValue && to.HasValue)
                        entriesQuery = (IIncludableQueryable<JournalEntry, Journal?>)entriesQuery
                            .Where(e => e.Journal!.WeekStartDate >= from && e.Journal!.WeekStartDate < to);

                    var entries = await entriesQuery.ToListAsync();
                    decimal avg = entries.Where(e => e.Grade.HasValue).Select(e => e.Grade!.Value).DefaultIfEmpty(0).Average();
                    groupAvgParts.Add($"{g.Name}:{Math.Round(avg, 2)}");
                }

                var attQuery = _context.JournalEntries
                    .Where(e => e.StudentId == s.Id && !e.IsDeleted)
                    .Include(e => e.Journal);
                if (from.HasValue && to.HasValue)
                    attQuery = (IIncludableQueryable<JournalEntry, Journal?>)attQuery
                        .Where(e => e.Journal!.WeekStartDate >= from && e.Journal!.WeekStartDate < to);

                var attList = await attQuery.Select(e => e.AttendanceStatus).ToListAsync();
                int present = attList.Count(a => a == AttendanceStatus.Present);
                int absent = attList.Count(a => a == AttendanceStatus.Absent);
                int late = attList.Count(a => a == AttendanceStatus.Late);
                int totalAtt = present + absent + late;
                decimal attendanceRate = totalAtt > 0 ? (decimal)present / totalAtt : 0m; // 0..1
                
                studentsSheet.Cell(row, 1).Value = index++;
                studentsSheet.Cell(row, 2).Value = s.FullName;
                studentsSheet.Cell(row, 3).Value = s.Email;
                studentsSheet.Cell(row, 4).Value = s.PhoneNumber;
                studentsSheet.Cell(row, 5).Value = s.Center?.Name ?? s.CenterId.ToString();
                studentsSheet.Cell(row, 6).Value = TranslateGender(s.Gender);
                studentsSheet.Cell(row, 7).Value = s.Age;
                studentsSheet.Cell(row, 8).Value = s.Birthday;
                studentsSheet.Cell(row, 9).Value = s.Address;
                studentsSheet.Cell(row, 10).Value = TranslatePaymentStatus(s.PaymentStatus);
                studentsSheet.Cell(row, 11).Value = TranslateActiveStatus(s.ActiveStatus);
                studentsSheet.Cell(row, 12).Value = s.TotalPaid;
                studentsSheet.Cell(row, 13).Value = s.LastPaymentDate;
                studentsSheet.Cell(row, 14).Value = s.NextPaymentDueDate;
                studentsSheet.Cell(row, 15).Value = groups.Count;
                studentsSheet.Cell(row, 16).Value = string.Join(", ", groups.Select(g => g.Name));
                studentsSheet.Cell(row, 17).Value = string.Join("; ", groupAvgParts);
                studentsSheet.Cell(row, 18).Value = present;
                studentsSheet.Cell(row, 19).Value = absent;
                studentsSheet.Cell(row, 20).Value = late;
                studentsSheet.Cell(row, 21).Value = attendanceRate;
                studentsSheet.Cell(row, 22).Value = from.HasValue ? $"{year:D4}-{month:D2}" : "Все";

                
                for (int c = 1; c <= studentsHeaders.Length; c++)
                {
                    var cell = studentsSheet.Cell(row, c);
                    ApplyDataCell(cell);

                    if (c == 8 || c == 13 || c == 14) // даты
                    {
                        cell.Style.DateFormat.Format = "dd.MM.yyyy";
                    }
                    if (c == 12) 
                    {
                        ApplyNumericCell(cell, "#,##0.00");
                    }
                    if (c == 21) 
                    {
                        ApplyNumericCell(cell, "0.00%");
                    }
                }

                if ((row % 2) == 1)
                    studentsSheet.Range(row, 1, row, studentsHeaders.Length).Style.Fill.SetBackgroundColor(stripedRow);

                row++;
            }

            var usedRowsStudents = Math.Max(row - 1, 2);
            var tableRangeStudents = studentsSheet.Range(2, 1, usedRowsStudents, studentsHeaders.Length);
            var studentsTable = tableRangeStudents.CreateTable();
            studentsTable.Theme = XLTableTheme.TableStyleMedium2;

            studentsTable.HeadersRow().Style.Fill.SetBackgroundColor(headerBg);
            studentsTable.HeadersRow().Style.Font.SetFontColor(textDark);
            studentsTable.HeadersRow().Style.Font.SetBold();
            studentsSheet.Row(2).Style.Font.SetBold();

            studentsSheet.SheetView.FreezeRows(2);
            studentsSheet.Columns().AdjustToContents();
            studentsSheet.Column(2).Width = 36; 
            studentsSheet.Column(16).Width = 30; 
            var sgSheet = wb.Worksheets.Add("Студент-группа");
            var sgHeaders = new[]
            {
                "№", "Студент", "Группа", "Преподаватель", "Курс",
                "Вступил", "Вышел", "Средняя оценка", "Средн. оценка (экзамен)", "Прис.", "Отс.", "Опозд.", "Посещаемость", "Период"
            };

            var titleRangeSg = sgSheet.Range(1, 1, 1, sgHeaders.Length);
            ApplyTitle(titleRangeSg, $"Аналитика студент-группа — {centerTitle} — {scopeTitle}");
            sgSheet.Row(1).Height = 26;

            for (int i = 0; i < sgHeaders.Length; i++)
                ApplyHeader(sgSheet.Cell(2, i + 1), sgHeaders[i]);
            sgSheet.Row(2).Height = 24;

            int sgRow = 3;
            int sgIndex = 1;
            var allGroupIds = students.SelectMany(st => st.StudentGroups.Select(sg => sg.GroupId)).Distinct().ToList();
            var groupMeta = await _context.Groups
                .Where(g => allGroupIds.Contains(g.Id) && !g.IsDeleted)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    CourseName = g.Course != null ? g.Course.CourseName : null,
                    MentorFullName = g.Mentor != null ? g.Mentor.FullName : null
                })
                .ToListAsync();

            foreach (var s in students)
            {
                foreach (var sg in s.StudentGroups)
                {
                    int gId = sg.GroupId;
                    var meta = groupMeta.FirstOrDefault(m => m.Id == gId);

                    var entriesQuery = _context.JournalEntries
                        .Where(e => e.StudentId == s.Id && e.Journal!.GroupId == gId && !e.IsDeleted)
                        .Include(e => e.Journal);
                    if (from.HasValue && to.HasValue)
                        entriesQuery = (IIncludableQueryable<JournalEntry, Journal?>)entriesQuery
                            .Where(e => e.Journal!.WeekStartDate >= from && e.Journal!.WeekStartDate < to);

                    var entries = await entriesQuery.ToListAsync();

                    decimal avgAll = entries.Where(e => e.Grade.HasValue).Select(e => e.Grade!.Value).DefaultIfEmpty(0).Average();
                    decimal avgExam = entries.Where(e => e.LessonType == LessonType.Exam && e.Grade.HasValue)
                        .Select(e => e.Grade!.Value).DefaultIfEmpty(0).Average();

                    int presentSg = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Present);
                    int absentSg = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Absent);
                    int lateSg = entries.Count(e => e.AttendanceStatus == AttendanceStatus.Late);
                    int totalSg = presentSg + absentSg + lateSg;
                    decimal attendancePct = totalSg > 0 ? (decimal)presentSg / totalSg : 0m;

                    sgSheet.Cell(sgRow, 1).Value = sgIndex++;
                    sgSheet.Cell(sgRow, 2).Value = s.FullName;
                    sgSheet.Cell(sgRow, 3).Value = meta?.Name ?? string.Empty;
                    sgSheet.Cell(sgRow, 4).Value = meta?.MentorFullName ?? string.Empty;
                    sgSheet.Cell(sgRow, 5).Value = meta?.CourseName ?? string.Empty;
                    sgSheet.Cell(sgRow, 6).Value = sg.JoinDate;
                    sgSheet.Cell(sgRow, 7).Value = sg.LeaveDate;
                    sgSheet.Cell(sgRow, 8).Value = Math.Round(avgAll, 2);
                    sgSheet.Cell(sgRow, 9).Value = Math.Round(avgExam, 2);
                    sgSheet.Cell(sgRow, 10).Value = presentSg;
                    sgSheet.Cell(sgRow, 11).Value = absentSg;
                    sgSheet.Cell(sgRow, 12).Value = lateSg;
                    sgSheet.Cell(sgRow, 13).Value = attendancePct;
                    sgSheet.Cell(sgRow, 14).Value = from.HasValue ? $"{year:D4}-{month:D2}" : "Все";

                    for (int c = 1; c <= sgHeaders.Length; c++)
                    {
                        var cell = sgSheet.Cell(sgRow, c);
                        ApplyDataCell(cell);

                        if (c == 6 || c == 7) 
                            cell.Style.DateFormat.Format = "dd.MM.yyyy";
                        if (c == 13) 
                            cell.Style.NumberFormat.Format = "0.00%";
                    }

                    if ((sgRow % 2) == 1)
                        sgSheet.Range(sgRow, 1, sgRow, sgHeaders.Length).Style.Fill.SetBackgroundColor(stripedRow);

                    sgRow++;
                }
            }

            var usedRowsSg = Math.Max(sgRow - 1, 2);
            var tableRangeSg = sgSheet.Range(2, 1, usedRowsSg, sgHeaders.Length);
            var sgTable = tableRangeSg.CreateTable();
            sgTable.Theme = XLTableTheme.TableStyleMedium2;

            sgTable.HeadersRow().Style.Fill.SetBackgroundColor(headerBg);
            sgTable.HeadersRow().Style.Font.SetFontColor(textDark);
            sgTable.HeadersRow().Style.Font.SetBold();
            sgSheet.Row(2).Style.Font.SetBold();

            sgSheet.SheetView.FreezeRows(2);
            sgSheet.Columns().AdjustToContents();

            sgSheet.Column(2).Width = 36;
            sgSheet.Columns(3, 5).Width = 26;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private static string TranslateGender(Gender gender) => gender switch
        {
            Gender.Male => "Мужчина",
            Gender.Female => "Женщина",
            _ => gender.ToString()
        };

        private static string TranslatePaymentStatus(PaymentStatus status) => status switch
        {
            PaymentStatus.Paid or PaymentStatus.Completed => "Оплачено",
            PaymentStatus.Pending => "Ожидается",
            PaymentStatus.Failed => "Неуспешно",
            _ => status.ToString()
        };

        private static string TranslateActiveStatus(ActiveStatus status) => status switch
        {
            ActiveStatus.Active => "Активен",
            ActiveStatus.Inactive => "Неактивен",
            ActiveStatus.Completed => "Завершено",
            _ => status.ToString()
        };
    }
}
