using Microsoft.AspNetCore.Http;
using BaseWebApi.Shared.Constants;
using ApplicationException = BaseWebApi.Shared.Exceptions.ApplicationException;

namespace BaseWebApi.Shared.Helpers;

/// <summary>
/// Helpers for multipart/form-data file uploads.
/// </summary>
public static class FileHelpers
{
    public static void ValidateUpload(
        IFormFile? file,
        long? maxBytes = null,
        IReadOnlyCollection<string>? allowedContentTypes = null)
    {
        if (file is null || file.Length <= 0)
        {
            throw new ApplicationException("Uploaded file is required.", "file.required");
        }

        var limit = maxBytes ?? AppConstants.Storage.MaxUploadBytes;
        if (file.Length > limit)
        {
            throw new ApplicationException(
                $"File exceeds maximum size of {limit} bytes.",
                "file.too_large");
        }

        var allowed = allowedContentTypes ?? AppConstants.Storage.AllowedContentTypes;
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ApplicationException(
                $"Content type '{file.ContentType}' is not allowed.",
                "file.content_type");
        }
    }

    public static async Task<(string FileName, string ContentType, byte[] Content)> ReadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateUpload(file);

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        return (file.FileName, file.ContentType, memory.ToArray());
    }

    public static string BuildObjectKey(string folder, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return $"{folder.Trim('/')}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{safeName}";
    }
}
