using Microsoft.Extensions.Options;

namespace Business.Services.Documents.File;

public sealed class LocalFileBuffer(
    IOptions<FileStorageOptions> storageOpts) : ILocalFileBuffer
{
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;

    public Task<ILocalFileLease> AllocateAsync(
        string requestedExtension,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        var directory = Path.Combine(
            Path.GetTempPath(),
            _storageOpts.AppDirectory,
            _storageOpts.FileDirectoryBuffer);

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{Guid.NewGuid()}{NormalizeExtension(requestedExtension)}");
        using (System.IO.File.Create(filePath)) { }
        return Task.FromResult<ILocalFileLease>(new LocalFileLease(filePath));
    }

    public async Task<ILocalFileLease> CopyFromAsync(
        Stream source,
        string canonicalExtension,
        CancellationToken cxlTkn = default)
    {
        var lease = await AllocateAsync(canonicalExtension, cxlTkn);

        try
        {
            await using var destination = new FileStream(
                lease.FilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await source.CopyToAsync(destination, cxlTkn);
            return lease;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }

    private static string NormalizeExtension(string extension) =>
        string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : "." + extension.Trim().TrimStart('.');

    private sealed class LocalFileLease(string filePath) : ILocalFileLease
    {
        private int _disposed;
        private int _ownershipTransferred;

        public string FilePath { get; } = filePath;

        public Stream OpenRead()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return new FileStream(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
        }

        public Stream OpenReadAndTransferOwnership()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (Interlocked.CompareExchange(ref _ownershipTransferred, 1, 0) != 0)
                throw new InvalidOperationException("Lease ownership has already been transferred.");

            return new FileStream(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (Volatile.Read(ref _ownershipTransferred) == 0)
                TryDelete();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void TryDelete()
        {
            try
            {
                System.IO.File.Delete(FilePath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
