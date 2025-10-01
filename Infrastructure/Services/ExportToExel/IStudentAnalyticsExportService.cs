namespace Infrastructure.Services.ExportToExel;

public interface IStudentAnalyticsExportService
{
    Task<byte[]> ExportStudentAnalyticsToExcelAsync(int? month = null, int? year = null);
}


