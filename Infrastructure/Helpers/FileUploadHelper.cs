using System.Net;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Helpers;

public static class FileUploadHelper
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
    private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
    private const long MaxImageSize = 50 * 1024 * 1024; // 50 MB
    private const long MaxDocumentSize = 20 * 1024 * 1024; // 20 MB

    public static async Task<Response<string>> UploadFileAsync(
        IFormFile file,
        string uploadPath,
        string entityType,
        string fileType, // "profile" или "document"
        bool deleteOldFile = false,
        string? oldFilePath = null)
    {
        if (file == null || file.Length == 0)
            return new Response<string>(HttpStatusCode.BadRequest, $"No {fileType} file provided");

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = fileType == "profile" ? AllowedImageExtensions : AllowedDocumentExtensions;
        var maxSize = fileType == "profile" ? MaxImageSize : MaxDocumentSize;

        if (!allowedExtensions.Contains(fileExtension))
            return new Response<string>(HttpStatusCode.BadRequest,
                $"Invalid {fileType} format. Allowed formats: {string.Join(", ", allowedExtensions)}");

        if (file.Length > maxSize)
            return new Response<string>(HttpStatusCode.BadRequest, $"{fileType} size must be less than {maxSize / (1024 * 1024)}MB");

        // Удаление старого файла, если указано
        if (deleteOldFile && !string.IsNullOrEmpty(oldFilePath))
        {
            var fullOldPath = Path.Combine(uploadPath, oldFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullOldPath))
            {
                try
                {
                    File.Delete(fullOldPath);
                }
                catch
                {
                    // Игнорируем ошибки удаления
                }
            }
        }

        // Создание папки
        var folder = Path.Combine(uploadPath, "uploads", fileType == "profile" ? entityType : $"documents/{entityType}");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        // Сохранение файла
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folder, uniqueFileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return new Response<string>($"/Uploads/{(fileType == "profile" ? entityType : $"documents/{entityType}")}/{uniqueFileName}");
    }

    public static async Task<Response<byte[]>> GetFileAsync(string filePath, string uploadPath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new Response<byte[]>(HttpStatusCode.NotFound, "File path is empty");

        var fullPath = Path.Combine(uploadPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return new Response<byte[]>(HttpStatusCode.NotFound, "File not found on server");

        var fileBytes = await File.ReadAllBytesAsync(fullPath);
        return new Response<byte[]>(fileBytes);
    }
}