using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Presentation.Pages.Payment;

/// <summary>
/// Receives and processes ZaloPay server callbacks.
/// </summary>
[AllowAnonymous]
public class ZpCallbackModel(IPaymentService paymentService) : PageModel
{
    private readonly IPaymentService _paymentService = paymentService;

    private readonly JsonSerializerOptions _zaloPayJsonOpts = new()
    {
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// Receives ZaloPay server callbacks and completes matching payments.
    /// </summary>
    /// <param name="callbackReq">The callback payload sent by ZaloPay.</param>
    /// <param name="key2">The ZaloPay provider secret key.</param>
    /// <returns>A provider-specific callback acknowledgement.</returns>
    public async Task<IActionResult> OnPostAsync([FromBody] ZaloPayCallbackRequest callbackReq, [FromServices] IConfiguration config)
    {
        var result = new Dictionary<string, object>();

        try
        {
            var key2 = config["PaymentProviders:ZaloPay:Key2"] ?? "";
            var mac = ComputeHmacZaloPay(callbackReq.Data, key2);

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

        return new JsonResult(result);
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
