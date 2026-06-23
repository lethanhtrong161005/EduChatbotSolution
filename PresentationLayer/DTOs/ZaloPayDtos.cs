namespace Presentation.DTOs;

/// <summary>
/// Represents the ZaloPay create transaction response used by payment initialization.
/// </summary>
public class ZaloPayCreateTransactionResponse
{
    /// <summary>Gets or sets the provider return code.</summary>
    public int ReturnCode { get; set; }

    /// <summary>Gets or sets the provider return message.</summary>
    public string ReturnMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider sub-return code.</summary>
    public int SubReturnCode { get; set; }

    /// <summary>Gets or sets the provider sub-return message.</summary>
    public string SubReturnMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider-hosted payment URL.</summary>
    public string OrderUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider transaction token.</summary>
    public string ZpTransToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider order token.</summary>
    public string OrderToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the QR code payload returned by the provider.</summary>
    public string QrCode { get; set; } = string.Empty;
}

/// <summary>
/// Represents a ZaloPay callback request.
/// </summary>
public class ZaloPayCallbackRequest
{
    /// <summary>Gets or sets the signed callback data.</summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback HMAC value.</summary>
    public string Mac { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback type.</summary>
    public int Type { get; set; }
}

/// <summary>
/// Represents the decoded data inside a ZaloPay callback.
/// </summary>
public class ZaloPayCallbackData
{
    /// <summary>Gets or sets the ZaloPay application ID.</summary>
    public int AppId { get; set; }

    /// <summary>Gets or sets the merchant transaction code.</summary>
    public string AppTransId { get; set; } = string.Empty;

    /// <summary>Gets or sets the merchant transaction timestamp.</summary>
    public long AppTime { get; set; }

    /// <summary>Gets or sets the merchant user identifier.</summary>
    public string AppUser { get; set; } = string.Empty;

    /// <summary>Gets or sets the paid amount.</summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets provider embed data.</summary>
    public string EmbedData { get; set; } = string.Empty;

    /// <summary>Gets or sets provider item data.</summary>
    public string Item { get; set; } = string.Empty;

    /// <summary>Gets or sets the ZaloPay transaction identifier.</summary>
    public long ZpTransId { get; set; }

    /// <summary>Gets or sets the provider server timestamp.</summary>
    public long ServerTime { get; set; }

    /// <summary>Gets or sets the provider payment channel.</summary>
    public int Channel { get; set; }

    /// <summary>Gets or sets the provider merchant user identifier.</summary>
    public string MerchantUserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user fee amount.</summary>
    public long UserFeeAmount { get; set; }

    /// <summary>Gets or sets the discount amount.</summary>
    public long DiscountAmount { get; set; }
}

/// <summary>
/// Defines ZaloPay create transaction return codes.
/// </summary>
public enum ZaloPayInitTransactionReturnCode
{
    /// <summary>Transaction initialization succeeded.</summary>
    Success = 1,

    /// <summary>Transaction initialization failed.</summary>
    Failure = 2,
}

/// <summary>
/// Defines ZaloPay callback acknowledgement return codes.
/// </summary>
public enum ZaloPayCallbackReturnCode
{
    /// <summary>The callback failed and should not be retried.</summary>
    FailureDoNotRetry = -1,

    /// <summary>The callback failed and can be retried later.</summary>
    FailureRetryLater = 0,

    /// <summary>The callback was processed successfully.</summary>
    Success = 1,

    /// <summary>The callback references a conflicting transaction.</summary>
    IdConflict = 2,
}
