namespace Business.Services.Documents.File;

public sealed class FileStorageOptions
{
    public string AppDirectory { get; set; } = string.Empty;

    public string FileDirectoryBuffer { get; set; } = string.Empty;

    public string FileDirectoryStaging { get; set; } = string.Empty;

    public string FileDirectoryReceived { get; set; } = string.Empty;

    public string FileDirectoryProcessing { get; set; } = string.Empty;

    public string FileDirectoryIndexed { get; set; } = string.Empty;

    public string FileDirectoryFailed { get; set; } = string.Empty;
}
