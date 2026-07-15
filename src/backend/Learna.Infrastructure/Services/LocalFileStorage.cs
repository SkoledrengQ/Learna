using Learna.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Learna.Infrastructure.Services;

/// <summary>Local-disk file storage. Production deployments must add virus scanning before files become available.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly long _maxBytes;
    private readonly HashSet<string> _allowedExtensions;

    public LocalFileStorage(IConfiguration configuration)
    {
        _root = Path.GetFullPath(configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "storage"));
        _maxBytes = (long.TryParse(configuration["Storage:MaxFileSizeMb"], out var maxMb) ? maxMb : 25) * 1024L * 1024L;
        var configured = configuration.GetSection("Storage:AllowedExtensions").GetChildren().Select(x => x.Value).OfType<string>().ToArray();
        _allowedExtensions = (configured.Length > 0 ? configured : new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".zip", ".txt" })
            .Select(NormalizeExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0) throw new FileStorageValidationException("FILE_EMPTY");
        if (sizeBytes > _maxBytes) throw new FileStorageValidationException("FILE_TOO_LARGE");
        var extension = NormalizeExtension(Path.GetExtension(originalFileName));
        if (!_allowedExtensions.Contains(extension)) throw new FileStorageValidationException("FILE_TYPE_NOT_ALLOWED");

        var storedPath = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var directory = SafePath(storedPath);
        Directory.CreateDirectory(directory);
        var fullPath = SafePath(storedPath, storedName);
        try
        {
            await using var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await source.CopyToAsync(destination, cancellationToken);
        }
        catch
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
            throw;
        }
        return new StoredFile(storedPath.Replace('\\', '/'), storedName, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default)
    {
        var path = SafePath(storedPath, storedName);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default)
    {
        var path = SafePath(storedPath, storedName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string SafePath(params string[] parts)
    {
        var path = Path.GetFullPath(parts.Aggregate(_root, Path.Combine));
        var prefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Stored file path escaped the storage root.");
        return path;
    }

    private static string NormalizeExtension(string extension) => extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}
