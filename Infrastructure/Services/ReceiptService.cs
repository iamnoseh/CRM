using System.Text;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services;

public class ReceiptService(DataContext dbContext, IConfiguration configuration) : IReceiptService
{
    public async Task<(string receiptNumber, string url)> GenerateOrGetReceiptAsync(int paymentId, string format = "html", CancellationToken ct = default)
    {
        var payment = await dbContext.Payments
            .Include(p => p.Student)
            .Include(p => p.Group)
            .ThenInclude(g => g.Course)
            .Include(p => p.Center)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted, ct);
        if (payment == null)
            throw new InvalidOperationException("Payment not found");

        if (string.IsNullOrWhiteSpace(payment.ReceiptNumber))
            throw new InvalidOperationException("ReceiptNumber is not assigned");

        var webRoot = configuration.GetValue<string>("UploadPath") ?? "wwwroot";
        var receiptsDir = Path.Combine(Directory.GetCurrentDirectory(), webRoot, "receipts");
        if (!Directory.Exists(receiptsDir)) Directory.CreateDirectory(receiptsDir);

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdfName = payment.ReceiptNumber + ".pdf";
            var pdfPath = Path.Combine(receiptsDir, pdfName);
            if (!File.Exists(pdfPath))
            {
                GeneratePdf(payment, pdfPath);
            }
            var url = "/receipts/" + pdfName;
            return (payment.ReceiptNumber!, url);
        }
        else
        {
            var fileName = payment.ReceiptNumber + ".html";
            var filePath = Path.Combine(receiptsDir, fileName);
            if (!File.Exists(filePath))
            {
                var html = BuildHtml(payment);
                await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, ct);
            }
            var url = "/receipts/" + fileName;
            return (payment.ReceiptNumber!, url);
        }
    }

    private static string BuildHtml(Domain.Entities.Payment p)
    {
        var studentName = (p.Student.FullName);
        var groupName = p.Group?.Name;
        var courseName = p.Group?.Course?.CourseName;
        var centerName = p.Center?.Name;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ru\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>Receipt {System.Net.WebUtility.HtmlEncode(p.ReceiptNumber)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body{font-family:Arial,Helvetica,sans-serif;padding:24px;color:#111}");
        sb.AppendLine("    .card{max-width:640px;margin:auto;border:1px solid #eee;border-radius:8px;padding:24px}");
        sb.AppendLine("    .row{display:flex;justify-content:space-between;margin:6px 0}");
        sb.AppendLine("    .muted{color:#666}");
        sb.AppendLine("    h1{font-size:20px;margin:0 0 8px}");
        sb.AppendLine("    h2{font-size:14px;margin:0 0 12px;color:#444}");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine($"    <h1>Квитанция #{System.Net.WebUtility.HtmlEncode(p.ReceiptNumber)}</h1>");
        sb.AppendLine($"    <div class=\"muted\">Центр: {System.Net.WebUtility.HtmlEncode(centerName)}</div>");
        sb.AppendLine("    <hr />");
        sb.AppendLine("    <h2>Плательщик</h2>");
        sb.AppendLine($"    <div class=\"row\"><div>Студент</div><div>{System.Net.WebUtility.HtmlEncode(studentName)}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Группа</div><div>{System.Net.WebUtility.HtmlEncode(groupName)}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Курс</div><div>{System.Net.WebUtility.HtmlEncode(courseName)}</div></div>");
        sb.AppendLine("    <h2>Оплата</h2>");
        sb.AppendLine($"    <div class=\"row\"><div>Сумма</div><div>{p.Amount:F2}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Скидка</div><div>{p.DiscountAmount:F2}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Итого (до скидки)</div><div>{p.OriginalAmount:F2}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Метод</div><div>{System.Net.WebUtility.HtmlEncode(p.PaymentMethod.ToString())}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Дата</div><div>{p.PaymentDate:yyyy-MM-dd HH:mm}</div></div>");
        sb.AppendLine($"    <div class=\"row\"><div>Период</div><div>{p.Month:00}.{p.Year}</div></div>");
        sb.AppendLine("    <hr />");
        sb.AppendLine("    <div class=\"muted\">Сгенерировано CRM Kavsar</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void GeneratePdf(Domain.Entities.Payment p, string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(12));
                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Квитанция #{p.ReceiptNumber}").FontSize(18).SemiBold();
                    col.Item().Text($"Центр: {p.Center?.Name}").FontColor(Colors.Grey.Darken2);
                    col.Item().LineHorizontal(1);
                    col.Item().Text("Плательщик").Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        t.Cell().Element(CellKey).Text("Студент"); t.Cell().Text($"{p.Student?.FullName}");
                        t.Cell().Element(CellKey).Text("Группа"); t.Cell().Text(p.Group?.Name);
                        t.Cell().Element(CellKey).Text("Курс"); t.Cell().Text(p.Group?.Course?.CourseName);
                    });
                    col.Item().Text("Оплата").Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        t.Cell().Element(CellKey).Text("Сумма"); t.Cell().Text(p.Amount.ToString("F2"));
                        t.Cell().Element(CellKey).Text("Скидка"); t.Cell().Text(p.DiscountAmount.ToString("F2"));
                        t.Cell().Element(CellKey).Text("Итого (до скидки)"); t.Cell().Text(p.OriginalAmount.ToString("F2"));
                        t.Cell().Element(CellKey).Text("Метод"); t.Cell().Text(p.PaymentMethod.ToString());
                        t.Cell().Element(CellKey).Text("Дата"); t.Cell().Text(p.PaymentDate.ToString("yyyy-MM-dd HH:mm"));
                        t.Cell().Element(CellKey).Text("Период"); t.Cell().Text($"{p.Month:00}.{p.Year}");
                    });
                    col.Item().LineHorizontal(1);
                    col.Item().Text("Сгенерировано CRM Kavsar").FontColor(Colors.Grey.Darken1);
                });
            });
        })
        .GeneratePdf(outputPath);

        static IContainer CellKey(IContainer container) => container
            .PaddingRight(8)
            .DefaultTextStyle(t => t.FontColor(Colors.Grey.Darken2));
    }
}


