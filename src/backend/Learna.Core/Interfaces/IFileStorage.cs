namespace Learna.Core.Interfaces;

public record StoredFile(string StoredPath, string StoredName, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default);
}

public sealed class FileStorageValidationException(string errorCode) : Exception(errorCode)
{
    public string ErrorCode { get; } = errorCode;
}
