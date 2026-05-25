using Microsoft.AspNetCore.Http;

namespace Kvitoria.Services.Images;

public class PlantImageService(IWebHostEnvironment environment) : IPlantImageService
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const string UploadsFolder = "uploads";
    private const string PlantImagesFolder = "plants";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    public async Task<string?> SaveImageAsync(
        IFormFile? photo,
        string? currentImageUrl,
        CancellationToken cancellationToken = default)
    {
        if (photo is null || photo.Length == 0)
        {
            return string.IsNullOrWhiteSpace(currentImageUrl) ? null : currentImageUrl;
        }

        ValidateImage(photo);

        var uploadsDirectory = GetUploadsDirectory();
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await photo.CopyToAsync(stream, cancellationToken);

        return $"/{UploadsFolder}/{PlantImagesFolder}/{fileName}";
    }

    private static void ValidateImage(IFormFile photo)
    {
        if (!photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Завантажити можна лише файл зображення.");
        }

        if (photo.Length > MaxFileSize)
        {
            throw new ArgumentException("Фото має бути не більше 5 МБ.");
        }

        var extension = Path.GetExtension(photo.FileName);

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Підтримуються фото JPG, PNG, WEBP або GIF.");
        }
    }

    private string GetUploadsDirectory()
    {
        var webRootPath = environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        return Path.Combine(webRootPath, UploadsFolder, PlantImagesFolder);
    }
}
