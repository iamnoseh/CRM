using System.Threading.Tasks;

namespace Infrastructure.Services.ExportToExel;

public interface IMentorExportService
{
    Task<byte[]> ExportAllMentorsToExcelAsync();
}