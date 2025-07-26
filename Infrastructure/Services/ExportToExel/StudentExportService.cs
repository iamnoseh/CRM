// using ClosedXML.Excel;
// using Domain.DTOs.Student;
// using Infrastructure.Data;
// using Microsoft.EntityFrameworkCore;
// using System;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Infrastructure.Helpers;
// using Microsoft.AspNetCore.Http;
//
// namespace Infrastructure.Services.ExportToExel;
//
// public class StudentExportService : IStudentExportService
// {
//     private readonly DataContext _context;
//     private readonly IHttpContextAccessor _httpContextAccessor;
//
//     public StudentExportService(DataContext context, IHttpContextAccessor httpContextAccessor)
//     {
//         _context = context;
//         _httpContextAccessor = httpContextAccessor;
//     }
//
//     public async Task<byte[]> ExportAllStudentsToExcelAsync()
//     {
//         var studentsQuery = _context.Students
//             .Include(s => s.User)
//             .Include(s => s.StudentGroups).ThenInclude(sg => sg.Group)
//             .Include(s => s.Center)
//             .Where(s => !s.IsDeleted);
//         studentsQuery = QueryFilterHelper.FilterByCenterIfNotSuperAdmin(
//             studentsQuery, _httpContextAccessor, s => s.CenterId);
//         var students = await studentsQuery.ToListAsync();
//
//         using var workbook = new XLWorkbook();
//         
//         var studentSheet = workbook.Worksheets.Add("Students");
//
//         // Title Row
//         studentSheet.Cell(1, 1).Value = "Student Records";
//         studentSheet.Range(1, 1, 1, 14).Merge().Style
//             .Font.SetBold()
//             .Font.SetFontSize(14)
//             .Font.SetFontColor(XLColor.White)
//             .Fill.SetBackgroundColor(XLColor.FromArgb(0x2F, 0x4F, 0x4F))
//             .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//             .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//         studentSheet.Row(1).Height = 30;
//
//         // Headers
//         string[] headers =
//         {
//             "№", "ID", "Full Name", "Email", "Phone", "Address", "Birthday",
//             "Age", "Gender", "Active Status", "Payment Status", "Groups Count", "Groups", "Center"
//         };
//
//         for (int i = 0; i < headers.Length; i++)
//         {
//             var headerCell = studentSheet.Cell(2, i + 1);
//             headerCell.Value = headers[i];
//             headerCell.Style
//                 .Font.SetBold()
//                 .Font.SetFontName("Calibri")
//                 .Font.SetFontSize(11)
//                 .Fill.SetBackgroundColor(XLColor.FromArgb(0xA9, 0xA9, 0xA9))
//                 .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//                 .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
//             studentSheet.Row(2).Height = 20;
//         }
//
//         // Add Center column
//         studentSheet.Cell(2, 14).Value = "Center";
//         studentSheet.Cell(2, 14).Style
//             .Font.SetBold()
//             .Font.SetFontName("Calibri")
//             .Font.SetFontSize(11)
//             .Fill.SetBackgroundColor(XLColor.FromArgb(0xA9, 0xA9, 0xA9))
//             .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//             .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
//         studentSheet.Column(14).Width = 20;
//
//         // Column Widths
//         studentSheet.Column(1).Width = 5; // №
//         studentSheet.Column(2).Width = 10; // ID
//         studentSheet.Column(3).Width = 25; // Full Name
//         studentSheet.Column(4).Width = 25; // Email
//         studentSheet.Column(5).Width = 15; // Phone
//         studentSheet.Column(6).Width = 30; // Address
//         studentSheet.Column(7).Width = 12; // Birthday
//         studentSheet.Column(8).Width = 8; // Age
//         studentSheet.Column(9).Width = 10; // Gender
//         studentSheet.Column(10).Width = 12; // Active Status
//         studentSheet.Column(11).Width = 12; // Payment Status
//         studentSheet.Column(12).Width = 12; // Groups Count
//         studentSheet.Column(13).Width = 40; // Groups
//         studentSheet.Column(14).Width = 20; // Center
//
//         int row = 3;
//         int index = 1;
//
//         foreach (var s in students)
//         {
//             var dto = new GetStudentDetailedDto
//             {
//                 Id = s.Id,
//                 FullName = s.User?.FullName ?? s.FullName,
//                 Email = s.User?.Email ?? s.Email,
//                 Phone = s.User?.PhoneNumber ?? s.PhoneNumber,
//                 Address = s.Address,
//                 Birthday = s.Birthday,
//                 Age = s.Age,
//                 Gender = s.Gender,
//                 ActiveStatus = s.ActiveStatus,
//                 PaymentStatus = s.PaymentStatus,
//                 GroupsCount = s.StudentGroups.Count(sg => sg.IsActive == true),
//             };
//
//             var groupNames = s.StudentGroups
//                 .Where(g => g.IsActive == true && g.Group != null)
//                 .Select(g => g.Group.Name)
//                 .ToList();
//
//             // Data Cells
//             studentSheet.Cell(row, 1).Value = index++;
//             studentSheet.Cell(row, 2).Value = dto.Id;
//             studentSheet.Cell(row, 3).Value = dto.FullName;
//             studentSheet.Cell(row, 4).Value = dto.Email;
//             studentSheet.Cell(row, 5).Value = dto.Phone;
//             studentSheet.Cell(row, 6).Value = dto.Address;
//             studentSheet.Cell(row, 7).Value = dto.Birthday?.ToString("yyyy-MM-dd") ?? "";
//             studentSheet.Cell(row, 8).Value = dto.Age;
//             studentSheet.Cell(row, 9).Value = dto.Gender.ToString();
//             studentSheet.Cell(row, 10).Value = dto.ActiveStatus.ToString();
//             studentSheet.Cell(row, 11).Value = dto.PaymentStatus.ToString();
//             studentSheet.Cell(row, 12).Value = dto.GroupsCount;
//             studentSheet.Cell(row, 13).Value = string.Join(", ", groupNames);
//             studentSheet.Cell(row, 14).Value = s.Center?.Name ?? "";
//
//             // Cell Styling
//             for (int col = 1; col <= headers.Length; col++)
//             {
//                 var cell = studentSheet.Cell(row, col);
//                 cell.Style
//                     .Font.SetFontName("Calibri")
//                     .Font.SetFontSize(11)
//                     .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
//                     .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//                 if (col == 6 || col == 13) // Address and Groups
//                     cell.Style.Alignment.SetWrapText(true);
//                 if (col == 8) // Age
//                     cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
//             }
//
//             // Alternating Row Colors
//             studentSheet.Range(row, 1, row, headers.Length).Style
//                 .Fill.SetBackgroundColor(row % 2 == 0 ? XLColor.FromArgb(0xF5, 0xF5, 0xF5) : XLColor.White);
//
//             // Active Status Colors
//             var activeCell = studentSheet.Cell(row, 10);
//             switch (dto.ActiveStatus.ToString())
//             {
//                 case "Active":
//                     activeCell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(0x90, 0xEE, 0x90));
//                     activeCell.Style.Font.SetFontColor(XLColor.DarkGreen);
//                     break;
//                 case "Inactive":
//                     activeCell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(0xFF, 0xCC, 0xCB));
//                     activeCell.Style.Font.SetFontColor(XLColor.Red);
//                     break;
//                 case "Completed":
//                     activeCell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(0xFF, 0xFF, 0xE0));
//                     activeCell.Style.Font.SetFontColor(XLColor.DarkOrange);
//                     break;
//             }
//
//             // Payment Status Colors
//             var paymentCell = studentSheet.Cell(row, 11);
//             switch (dto.PaymentStatus.ToString())
//             {
//                 case "Completed":
//                     paymentCell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(0x32, 0xCD, 0x32));
//                     paymentCell.Style.Font.SetFontColor(XLColor.White);
//                     break;
//                 case "Failed":
//                     paymentCell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(0xFF, 0x45, 0x44));
//                     paymentCell.Style.Font.SetFontColor(XLColor.White);
//                     break;
//             }
//
//             // Conditional Formatting for Age
//             studentSheet.Cell(row, 8).AddConditionalFormat()
//                 .WhenGreaterThan(30).Fill.SetBackgroundColor(XLColor.FromArgb(0xFF, 0xE4, 0xC4));
//
//             row++;
//         }
//
//         // Freeze Header and Title Rows
//         studentSheet.SheetView.FreezeRows(2);
//
//         // === STUDENT GROUPS SHEET ===
//         var groupSheet = workbook.Worksheets.Add("Student Groups");
//
//         // Title Row
//         groupSheet.Cell(1, 1).Value = "Student Group Assignments";
//         groupSheet.Range(1, 1, 1, 6).Merge().Style
//             .Font.SetBold()
//             .Font.SetFontSize(14)
//             .Font.SetFontColor(XLColor.White)
//             .Fill.SetBackgroundColor(XLColor.FromArgb(0x2F, 0x4F, 0x4F))
//             .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//             .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//         groupSheet.Row(1).Height = 30;
//
//         // Headers
//         string[] groupHeaders = { "№", "Student ID", "Full Name", "Group ID", "Group Name", "Average Grade" };
//         for (int col = 1; col <= groupHeaders.Length; col++)
//         {
//             var header = groupSheet.Cell(2, col);
//             header.Value = groupHeaders[col - 1];
//             header.Style
//                 .Font.SetBold()
//                 .Font.SetFontName("Calibri")
//                 .Font.SetFontSize(11)
//                 .Fill.SetBackgroundColor(XLColor.FromArgb(0xA9, 0xA9, 0xA9))
//                 .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//                 .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
//             groupSheet.Row(2).Height = 20;
//         }
//
//         // Column Widths
//         groupSheet.Column(1).Width = 5; // №
//         groupSheet.Column(2).Width = 10; // Student ID
//         groupSheet.Column(3).Width = 25; // Full Name
//         groupSheet.Column(4).Width = 10; // Group ID
//         groupSheet.Column(5).Width = 25; // Group Name
//         groupSheet.Column(6).Width = 12; // Average Grade
//
//         int groupRow = 3;
//         int groupIndex = 1;
//
//         foreach (var s in students)
//         {
//             var fullName = s.User?.FullName ?? s.FullName;
//             var groups = s.StudentGroups
//                 .Where(g => g.IsActive == true)
//                 .Select(g => new
//                 {
//                     g.GroupId,
//                     g.Group.Name,
//                     Avg = _context.Grades
//                         .Where(gr => gr.StudentId == s.Id && gr.GroupId == g.GroupId && gr.Value.HasValue)
//                         .Select(gr => (double?)gr.Value)
//                         .Average() ?? 0
//                 }).ToList();
//
//             foreach (var g in groups)
//             {
//                 groupSheet.Cell(groupRow, 1).Value = groupIndex++;
//                 groupSheet.Cell(groupRow, 2).Value = s.Id;
//                 groupSheet.Cell(groupRow, 3).Value = fullName;
//                 groupSheet.Cell(groupRow, 4).Value = g.GroupId;
//                 groupSheet.Cell(groupRow, 5).Value = g.Name;
//                 groupSheet.Cell(groupRow, 6).Value = Math.Round(g.Avg, 2);
//
//                 for (int col = 1; col <= groupHeaders.Length; col++)
//                 {
//                     var cell = groupSheet.Cell(groupRow, col);
//                     cell.Style
//                         .Font.SetFontName("Calibri")
//                         .Font.SetFontSize(11)
//                         .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
//                         .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//                     if (col == 6) // Average Grade
//                         cell.Style.NumberFormat.Format = "0.00";
//                 }
//
//                 // Alternating Row Colors
//                 groupSheet.Range(groupRow, 1, groupRow, groupHeaders.Length).Style
//                     .Fill.SetBackgroundColor(groupRow % 2 == 0 ? XLColor.FromArgb(0xF5, 0xF5, 0xF5) : XLColor.White);
//
//                 groupRow++;
//             }
//         }
//
//         // Freeze Header and Title Rows
//         groupSheet.SheetView.FreezeRows(2);
//
//         // Save to Memory Stream
//         using var stream = new MemoryStream();
//         workbook.SaveAs(stream);
//         return stream.ToArray();
//     }
// }
//
