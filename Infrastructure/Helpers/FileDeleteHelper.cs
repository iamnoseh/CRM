namespace Infrastructure.Helpers;

public static class FileDeleteHelper
{
    public static void DeleteFile(string? relativePath, string uploadPath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        var fullPath = Path.Combine(uploadPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch
            {
                // Log error or ignore
            }
        }
    }
} 