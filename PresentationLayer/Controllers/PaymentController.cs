using Domain.Common;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Models;
using Presentation.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Presentation.Controllers;

[Authorize]
public class PaymentController(
    ISubscriptionService subscriptionService,
    IPaymentService paymentService,
    IOptions<PaymentServiceOptions> paymentServiceOptions) : Controller
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly PaymentServiceOptions _paymentServiceOpts = paymentServiceOptions.Value;

    private readonly JsonSerializerOptions _zaloPayJsonOpts = new()
    {
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const string DefaultAppUser = "EduChatbotAI_User";

    public async Task<IActionResult> SelectMethod(int id) // SubscriptionOption ID
    {
        // Checkout logic

        return View();
    }

    public async Task<IActionResult> MakePayment(PaymentTransactionVm paymentTransaction, CancellationToken cxlTkn)
    {
        switch (paymentTransaction.PaymentMethod)
        {
            case PaymentMethod.ZaloPay:
                var zpInitTxnRes = await InitZaloPayTransaction();
                return Redirect(zpInitTxnRes.OrderUrl);
        }

        return View();

        async Task<ZaloPayInitTransactionResponse> InitZaloPayTransaction()
        {
            // TEMP: Pretend these are from the newly-generated order.
            Guid subId = Guid.NewGuid();
            decimal orderTotal = 100_000;

            var appId = _paymentServiceOpts.ZaloPay.AppId;
            var appUser = User.FindFirstValue(ClaimTypes.Email) ?? DefaultAppUser;
            var appTransId = GetTransactionCode(subId);
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var amount = (long)orderTotal;
            var item = "[]";    // TODO
            var description = $"EduChatbot - Thanh toán cho đơn hàng #{appTransId}";
            var embedData = JsonSerializer.Serialize(new
            {
                preferred_payment_method = new string[] { "domestic_card", "account" },
            }, _zaloPayJsonOpts);
            var bankCode = "";  // TODO
            var mac = ComputeHmacZaloPay($"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}", _paymentServiceOpts.ZaloPay.Key1);
            var callbackUrl = _paymentServiceOpts.ZaloPay.CallbackUrl;

            var client = new HttpClient();
            var zpInitTxnResMsg = await client.PostAsJsonAsync(_paymentServiceOpts.ZaloPay.Endpoint, new
            {
                app_id = _paymentServiceOpts.ZaloPay.AppId,
                app_user = appUser,
                app_trans_id = appTransId,
                app_time = appTime,
                amount = amount,
                item = item,
                description = description,
                embed_data = embedData,
                bank_code = bankCode,
                mac = mac,
                callback_url = callbackUrl,
            }, _zaloPayJsonOpts, cxlTkn);

            if (!zpInitTxnResMsg.IsSuccessStatusCode)
            {
                throw new Exception("Could not initiate ZaloPay transaction.");
            }

            var zpInitTxnRes = JsonSerializer.Deserialize<ZaloPayInitTransactionResponse>(zpInitTxnResMsg.Content.ReadAsStream(cxlTkn), _zaloPayJsonOpts)
                               ?? throw new Exception("Could not read response for ZaloPay transaction initiation.");

            if (zpInitTxnRes.ReturnCode != (int)ZaloPayInitTransactionReturnCode.Success)
            {
                throw new Exception($"ZaloPay transaction initiation failed: {zpInitTxnRes.ReturnMessage} - {zpInitTxnRes.SubReturnMessage}");
            }

            return zpInitTxnRes;

            static string GetTransactionCode(Guid orderId)
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var curDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var bytes = orderId.ToByteArray();
                var ints = new uint[4];
                for (int i = 0; i < 4; i++)
                {
                    ints[i] = BitConverter.ToUInt32(bytes, i * 4);
                }
                var code = ints.Select(i => string.Format("{0:d10}", i)).Aggregate((a, b) => a + b);

                return curDate + "_" + code;
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackRequest callbackReq, CancellationToken cxlTkn)
    {
        var result = new Dictionary<string, object>();

        try
        {
            var mac = ComputeHmacZaloPay(callbackReq.Data, _paymentServiceOpts.ZaloPay.Key2);

            if (mac == callbackReq.Mac)
            {
                var callbackData = JsonSerializer.Deserialize<ZaloPayCallbackData>(callbackReq.Data, _zaloPayJsonOpts)
                                   ?? throw new Exception("Could not read ZaloPay callback data.");

                var subId = GetSubscriptionId(callbackData.AppTransId);



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
            result["return_code"] = 0;  // Have ZaloPay retry the callback later.
            result["return_message"] = "Error processing callback data";
        }

        return Ok(result);

        static Guid GetSubscriptionId(string appTransId)
        {
            var code = appTransId[appTransId.IndexOf('_')..];
            if (!Regex.IsMatch(code, @"^[\d]{40}$"))
            {
                throw new Exception("Invalid AppTransId received from ZaloPay callback.");
            }

            var bytes = new byte[16];
            for (int i = 0; i < 4; i++)
            {
                var segment = code[(i * 10)..((i + 1) * 10)];
                var num = uint.Parse(segment);
                Array.Copy(BitConverter.GetBytes(num), 0, bytes, i * 4, 4);
            }
            return new Guid(bytes);
        }
    }

    private static string ComputeHmacZaloPay(string input, string key)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var outputBytes = new HMACSHA256(keyBytes).ComputeHash(inputBytes);
        var output = Convert.ToHexStringLower(outputBytes);
        return output;
    }
}

public class ZaloPayInitTransactionResponse
{
    public int ReturnCode { get; set; }
    public string ReturnMessage { get; set; } = string.Empty;
    public int SubReturnCode { get; set; }
    public string SubReturnMessage { get; set; } = string.Empty;
    public string OrderUrl { get; set; } = string.Empty;
    public string ZpTransToken { get; set; } = string.Empty;
    public string OrderToken { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}

public class ZaloPayCallbackRequest
{
    public string Data { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
    public int Type { get; set; }
}

public class ZaloPayCallbackData
{
    public int AppId { get; set; }
    public string AppTransId { get; set; } = string.Empty;
    public long AppTime { get; set; }
    public string AppUser { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string EmbedData { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public long ZpTransId { get; set; }
    public long ServerTime { get; set; }
    public int Channel { get; set; }
    public string MerchantUserId { get; set; } = string.Empty;
    public long UserFeeAmount { get; set; }
    public long DiscountAmount { get; set; }
}

public enum ZaloPayInitTransactionReturnCode
{
    Success = 1,
    Failure = 2,
}

public enum ZaloPayCallbackReturnCode
{
    FailureDoNotRetry = -1,
    FailureRetryLater = 0,
    Success = 1,
    IdConflict = 2,
}
