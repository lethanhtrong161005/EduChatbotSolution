namespace Business.Services.Storage;

public sealed class SupabaseOptions
{
    public string ApiUrl { get; set; } = string.Empty;

    public string ApiPublishableKey { get; set; } = string.Empty;

    public string ApiSecretKey { get; set; } = string.Empty;

    public string DocumentBucket { get; set; } = string.Empty;

    public string UserImportBucket { get; set; } = string.Empty;
}
