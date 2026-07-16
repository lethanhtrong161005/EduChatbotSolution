namespace Business.Services.Storage;

public interface ILocalFileBuffer
{
    Task<ILocalFileLease> AllocateAsync(
        string requestedExtension,
        CancellationToken cxlTkn = default);

    Task<ILocalFileLease> CopyFromAsync(
        Stream source,
        string requestedExtension,
        CancellationToken cxlTkn = default);
}

public interface ILocalFileLease : IDisposable, IAsyncDisposable
{
    string FilePath { get; }

    Stream OpenRead();

    Stream OpenReadAndTransferOwnership();
}
