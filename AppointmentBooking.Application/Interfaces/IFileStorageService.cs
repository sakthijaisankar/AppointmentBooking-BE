namespace AppointmentBooking.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string StoredFileName, string RelativePath)> SavePatientDocumentAsync(
        int patientId,
        string originalFileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<(Stream FileStream, string ContentType, string FileName)?> GetPatientDocumentAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeletePatientDocumentAsync(string relativePath, CancellationToken cancellationToken = default);
}
