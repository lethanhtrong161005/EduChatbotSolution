using Business.Services.ExternalPayment;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Extensions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Presentation.Controllers;

/// <summary>
/// Exposes payment status and provider callback endpoints.
/// Razor Pages handle payment method selection and processing screens.
/// </summary>
[Authorize]
[Route("payment")]
public class PaymentController(
    IPaymentService paymentService,
    IOptions<PaymentProviderOptions> paymentProviderOptions) : Controller
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly PaymentProviderOptions _paymentProviderOpts = paymentProviderOptions.Value;

    private readonly JsonSerializerOptions _zaloPayJsonOpts = new()
    {
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// Returns the current payment status for polling clients.
    /// </summary>
    /// <param name="id">The payment transaction identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A JSON result containing status and redirect URL.</returns>
    /// <exception cref="BadRequestException">Thrown when the payment ID is empty.</exception>
    [HttpGet("status/{id:guid}")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cxlTkn)
    {
        if (id == Guid.Empty)
        {
            throw new BadRequestException("Missing transaction ID.");
        }

        var payment = await GetAndValidatePaymentAsync(id, cxlTkn);
        return Json(new
        {
            Status = payment.Status.ToString(),
            RedirectUrl = "/plans",
        });
    }

    /// <summary>
    /// Receives ZaloPay server callbacks and completes matching payments.
    /// </summary>
    /// <param name="callbackReq">The callback payload sent by ZaloPay.</param>
    /// <returns>A provider-specific callback acknowledgement.</returns>
    [AllowAnonymous]
    [HttpPost("/zp-callback")]
    public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackRequest callbackReq)
    {
        var result = new Dictionary<string, object>();

        try
        {
            var mac = ComputeHmacZaloPay(callbackReq.Data, _paymentProviderOpts.ZaloPay.Key2);

            if (mac == callbackReq.Mac)
            {
                var callbackData = JsonSerializer.Deserialize<ZaloPayCallbackData>(callbackReq.Data, _zaloPayJsonOpts)
                                   ?? throw new Exception("Could not read ZaloPay callback data.");

                await _paymentService.CompletePaymentAsync(
                    externalTransactionCode: callbackData.AppTransId,
                    cancellationToken: CancellationToken.None);

                result["return_code"] = (int)ZaloPayCallbackReturnCode.Success;
                result["return_message"] = "Success";
            }
            else
            {
                result["return_code"] = (int)ZaloPayCallbackReturnCode.FailureDoNotRetry;
                result["return_message"] = "MAC mismatch";
            }
        }
        catch
        {
            result["return_code"] = (int)ZaloPayCallbackReturnCode.FailureRetryLater;
            result["return_message"] = "Error processing callback data";
        }

        return Ok(result);
    }

    /// <summary>
    /// Loads the payment and confirms that it belongs to the current user.
    /// </summary>
    /// <param name="paymentId">The payment identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The validated payment.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the payment cannot be found.</exception>
    /// <exception cref="UserClaimException">Thrown when the current user cannot access the payment.</exception>
    private async Task<Payment> GetAndValidatePaymentAsync(Guid paymentId, CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();

        var payment = await _paymentService.GetByIdAsync(paymentId, cxlTkn)
            ?? throw new EntityNotFoundException("No transaction matching the provided ID was found.");

        if (payment.Order.UserId != userId)
        {
            throw new UserClaimException("You do not have permission to access this transaction.");
        }

        return payment;
    }

    /// <summary>
    /// Computes a ZaloPay HMAC signature from the input payload.
    /// </summary>
    /// <param name="input">The raw payload to sign.</param>
    /// <param name="key">The provider secret key.</param>
    /// <returns>The hexadecimal HMAC value.</returns>
    private static string ComputeHmacZaloPay(string input, string key)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var outputBytes = new HMACSHA256(keyBytes).ComputeHash(inputBytes);
        return Convert.ToHexStringLower(outputBytes);
    }
}

/// <summary>
/// Represents the ZaloPay create transaction response used by payment initialization.
/// </summary>
public class ZaloPayCreateTransactionResponse
{
    /// <summary>
    /// Gets or sets the provider return code.
    /// </summary>
    public int ReturnCode { get; set; }

    /// <summary>
    /// Gets or sets the provider return message.
    /// </summary>
    public string ReturnMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider sub-return code.
    /// </summary>
    public int SubReturnCode { get; set; }

    /// <summary>
    /// Gets or sets the provider sub-return message.
    /// </summary>
    public string SubReturnMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider-hosted payment URL.
    /// </summary>
    public string OrderUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider transaction token.
    /// </summary>
    public string ZpTransToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider order token.
    /// </summary>
    public string OrderToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the QR code payload returned by the provider.
    /// </summary>
    public string QrCode { get; set; } = string.Empty;
}

/// <summary>
/// Represents a ZaloPay callback request.
/// </summary>
public class ZaloPayCallbackRequest
{
    /// <summary>
    /// Gets or sets the signed callback data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback HMAC value.
    /// </summary>
    public string Mac { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback type.
    /// </summary>
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
