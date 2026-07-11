namespace Business.Services.Documents.File;

public interface ILocalFileBuffer
{
    Task<ILocalFileLease> AllocateAsync(
        string canonicalExtension,
        CancellationToken cxlTkn = default);

    Task<ILocalFileLease> CopyFromAsync(
        Stream source,
        string canonicalExtension,
        CancellationToken cxlTkn = default);
}

public interface ILocalFileLease : IDisposable, IAsyncDisposable
{
    string FilePath { get; }

    Stream OpenRead();

    Stream OpenReadAndTransferOwnership();
}
