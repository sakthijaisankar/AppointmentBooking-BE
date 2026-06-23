using AppointmentBooking.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AppointmentBooking.Application.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadRoot;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    public FileStorageService(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:UploadPath"] ?? "Uploads";
        _uploadRoot = Path.Combine(environment.ContentRootPath, configuredPath);
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<(string StoredFileName, string RelativePath)> SavePatientDocumentAsync(
        int patientId,
        string originalFileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type '{extension}' is not allowed.");

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("patients", patientId.ToString(), storedFileName);
        var fullPath = Path.Combine(_uploadRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await fileStream.CopyToAsync(output, cancellationToken);

        return (storedFileName, relativePath.Replace('\\', '/'));
    }

    public Task<(Stream FileStream, string ContentType, string FileName)?> GetPatientDocumentAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_uploadRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult<(Stream, string, string)?>(null);

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<(Stream, string, string)?>((stream, contentType, Path.GetFileName(fullPath)));
    }

    public Task DeletePatientDocumentAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_uploadRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
