using Microsoft.AspNetCore.Http;

namespace Kvitoria.Services.Images;

public interface IPlantImageService
{
    Task<string?> SaveImageAsync(
        IFormFile? photo,
        string? currentImageUrl,
        CancellationToken cancellationToken = default);
}
