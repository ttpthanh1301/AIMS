namespace AIMS.BackendServer.Helpers;

public static class FileHelper
{
    private static readonly string[] AllowedExtensions
        = { ".pdf", ".doc", ".docx" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public static bool IsValidCVFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        return AllowedExtensions.Contains(ext)
            && file.Length > 0
            && file.Length <= MaxFileSizeBytes;
    }

    public static async Task<string> SaveCVFileAsync(
        IFormFile file,
        string uploadFolder)
    {
        Directory.CreateDirectory(uploadFolder);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName; // Trả về tên file để lưu vào DB
    }
}