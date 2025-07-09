namespace Infrastructure.Services.ExportToExel;

public interface IStudentExportService
{
    Task<byte[]> ExportAllStudentsToExcelAsync();
}