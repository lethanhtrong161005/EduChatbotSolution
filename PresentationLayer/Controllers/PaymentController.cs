using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
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

namespace Presentation.Controllers;

[Authorize]
public class PaymentController(
    ISubscriptionService subscriptionService,
    IOrderService orderService,
    IPaymentService paymentService,
    IOptions<PaymentProviderOptions> paymentProviderOptions,
    IMapper mapper) : Controller
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly IOrderService _orderService = orderService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly PaymentProviderOptions _paymentProviderOpts = paymentProviderOptions.Value;
    private readonly IMapper _mapper = mapper;

    private readonly JsonSerializerOptions _zaloPayJsonOpts = new()
    {
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const string DefaultAppUser = "EduChatbotAI_User";

    [HttpGet]
    public async Task<IActionResult> SelectMethod(Guid id, CancellationToken cxlTkn)
    {
        if (id == Guid.Empty)
            throw new BadRequestException("Missing order ID.");

        var order = await GetAndValidateOrderAsync(id, cxlTkn);
        if (order.Status != OrderStatus.PendingPayment)
            throw new EntityConstraintException("Only pending orders can be paid for.");

        var selectPaymentMethodVm = new PaymentSelectMethodVm
        {
            PaymentMethods = GetPaymentMethods(),
            PendingOrder = _mapper.Map<OrderCheckoutVm>(order),
        };
        return View(selectPaymentMethodVm);
    }

    [HttpPost]
    public async Task<IActionResult> MakePayment(Guid id, PaymentSelectMethodVm vm, CancellationToken cxlTkn)
    {
        if (id == Guid.Empty)
            throw new BadRequestException("Missing order ID.");
        if (id != vm.PendingOrder.Id)
            throw new BadRequestException("Mismatched order ID.");
        if (!ModelState.IsValid)
            throw new EntityConstraintException("Invalid payment method and/or order. Please try again.");

        var order = await GetAndValidateOrderAsync(id, cxlTkn);
        if (order.Status != OrderStatus.PendingPayment)
            throw new EntityConstraintException("Only pending orders can be paid for.");

        switch (vm.SelectedMethod)
        {
            case PaymentMethod.ZaloPay:
                var (zpInitTxnRes, extTxnCode) = await CreateZaloPayTransaction();
                await _paymentService.CreatePendingPaymentAsync(order.Id, PaymentMethod.ZaloPay, extTxnCode, cxlTkn);
                return Redirect(zpInitTxnRes.OrderUrl);
        }

        throw new Exception("Eh!? I am confusion.");

        async Task<(ZaloPayCreateTransactionResponse response, string transactionCode)> CreateZaloPayTransaction()
        {
            var orderId = order.Id;
            var orderTotal = order.ChargedAmount;

            var appId = _paymentProviderOpts.ZaloPay.AppId;
            var appUser = User.FindFirstValue(ClaimTypes.Name) ?? DefaultAppUser;
            var appTransId = GetTransactionCode(orderId);
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var amount = (long)orderTotal;
            var item = "[]";
            var description = $"EduChatbotAI - Thanh toán cho đơn hàng #{appTransId}";
            var redirectUrl = _paymentProviderOpts.ZaloPay.RedirectUrlBase + "?transaction-id=" + appTransId;
            var embedData = $"{{\"redirecturl\": \"{redirectUrl}\"}}";
            var bankCode = "";
            var mac = ComputeHmacZaloPay($"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}", _paymentProviderOpts.ZaloPay.Key1);
            var callbackUrl = _paymentProviderOpts.ZaloPay.CallbackUrl;

            var param = new Dictionary<string, string>
            {
                { "app_id", appId.ToString() },
                { "app_user", appUser },
                { "app_trans_id", appTransId },
                { "app_time", appTime.ToString() },
                { "amount", amount.ToString() },
                { "item", item },
                { "description", description },
                { "embed_data", embedData },
                { "bank_code", bankCode },
                { "mac", mac },
                { "callback_url", callbackUrl },
            };
            var form = new FormUrlEncodedContent(param);

            using var client = new HttpClient();
            var zpInitTxnResMsg = await client.PostAsync(_paymentProviderOpts.ZaloPay.CreateTransactionEndpoint, form, cxlTkn);

            if (!zpInitTxnResMsg.IsSuccessStatusCode)
            {
                throw new Exception("Could not create ZaloPay transaction.");
            }

            var zpCreateTxnRes = JsonSerializer.Deserialize<ZaloPayCreateTransactionResponse>(await zpInitTxnResMsg.Content.ReadAsStreamAsync(cxlTkn), _zaloPayJsonOpts)
                               ?? throw new Exception("Could not read response for ZaloPay transaction creation.");

            if (zpCreateTxnRes.ReturnCode != (int)ZaloPayInitTransactionReturnCode.Success)
            {
                throw new Exception($"Failed to create ZaloPay transaction: {zpCreateTxnRes.ReturnMessage} - {zpCreateTxnRes.SubReturnMessage}");
            }

            return (zpCreateTxnRes, appTransId);

            static string GetTransactionCode(Guid orderId)
            {
                // RULES: Format: yyMMdd_<CODE>; Max length: 40; Timezone: Vietnam (UTC+7)

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var curDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).ToString("yyMMdd");

                var code = orderId.ToString("N");

                //var bytes = orderId.ToByteArray();
                //var ints = new uint[4];
                //for (int i = 0; i < 4; i++)
                //{
                //    ints[i] = BitConverter.ToUInt32(bytes, i * 4);
                //}
                //var code = ints.Select(i => string.Format("{0:d10}", i)).Aggregate((a, b) => a + b);

                return curDate + "_" + code;
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> ProcessingPayment([FromQuery] string transactionId, CancellationToken cxlTkn)
    {
        if (string.IsNullOrEmpty(transactionId))
            throw new BadRequestException("Missing payment transaction code.");

        var payment = await GetAndValidatePaymentAsync(transactionId, cxlTkn);

        var paymentVm = _mapper.Map<PaymentProcessingVm>(payment);
        return View(paymentVm);
    }

    [HttpGet]
    public async Task<IActionResult> Status(Guid id, CancellationToken cxlTkn)
    {
        if (id == Guid.Empty)
            throw new BadRequestException("Missing transaction ID.");

        var payment = await GetAndValidatePaymentAsync(id, cxlTkn);
        return Json(new
        {
            Status = payment.Status.ToString(),
            RedirectUrl = "/plans",
        });
    }

    [HttpGet]
    public async Task<IActionResult> MySubscription(CancellationToken cxlTkn)
    {
        return View();
    }

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

                var orderId = GetOrderId(callbackData.AppTransId);
                await _paymentService.CompletePaymentAsync(externalTransactionCode: callbackData.AppTransId, cancellationToken: CancellationToken.None);

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

        static Guid GetOrderId(string appTransId)
        {
            var code = appTransId[(appTransId.IndexOf('_') + 1)..];
            return Guid.Parse(code);

            //if (!Regex.IsMatch(code, @"^[\d]{40}$"))
            //{
            //    throw new Exception("Invalid AppTransId received from ZaloPay callback.");
            //}

            //var bytes = new byte[16];
            //for (int i = 0; i < 4; i++)
            //{
            //    var segment = code[(i * 10)..((i + 1) * 10)];
            //    var num = uint.Parse(segment);
            //    Array.Copy(BitConverter.GetBytes(num), 0, bytes, i * 4, 4);
            //}
            //return new Guid(bytes);
        }
    }

    private async Task<Order> GetAndValidateOrderAsync(Guid orderId, CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");

        var order = await _orderService.GetByIdAsync(orderId, cxlTkn)
            ?? throw new EntityNotFoundException("No order matching the provided ID was found.");

        if (order.UserId != userId)
            throw new UserClaimException("You do not have permission to access this order.");

        return order;
    }

    private async Task<Payment> GetAndValidatePaymentAsync(Guid paymentId, CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");

        var payment = await _paymentService.GetByIdAsync(paymentId, cxlTkn)
            ?? throw new EntityNotFoundException("No transaction matching the provided ID was found.");

        if (payment.Order.UserId != userId)
            throw new UserClaimException("You do not have permission to access this transaction.");

        return payment;
    }

    private async Task<Payment> GetAndValidatePaymentAsync(string txnCode, CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");

        var payment = (await _paymentService.GetAsync(filter: e => e.ExternalTransactionCode == txnCode,
                                                      includeProperties: [nameof(Payment.Order)
                                                                          + "."
                                                                          + nameof(Payment.Order.Subscription)
                                                                          + "."
                                                                          + nameof(Payment.Order.Subscription.PlanOption)
                                                                          + "."
                                                                          + nameof(Payment.Order.Subscription.PlanOption.Plan)],
                                                      cancellationToken: cxlTkn))
                                            .FirstOrDefault()
                      ?? throw new EntityNotFoundException("No transaction matching the provided transaction code was found.");

        if (payment.Order.UserId != userId)
            throw new UserClaimException("You do not have permission to access this transaction.");

        return payment;
    }

    private static List<PaymentMethodVm> GetPaymentMethods()
    {
        return
        [
            new() {
                Name = PaymentMethod.BankTransfer.ToString(),
                IconClass = "fas fa-university",
                DisplayName = "Direct Bank Transfer",
                Description = "Receive bank account details and transfer manually.",
            },
            new()
            {
                Name = PaymentMethod.Visa_Mastercard.ToString(),
                IconClass = "fas fa-credit-card",
                DisplayName = "Visa / Mastercard",
                Description = "International credit and debit cards.",
            },
            new()
            {
                Name = PaymentMethod.VnPay.ToString(),
                ImageSource = "/img/payment/logo-vnpay.png",
                ImageAlt = "VNPay",
                DisplayName = "VNPay",
                Description = "ATM cards, Internet Banking, QR Pay.",
            },
            new()
            {
                Name = PaymentMethod.MoMo.ToString(),
                ImageSource = "/img/payment/logo-momo.png",
                ImageAlt = "MoMo",
                DisplayName = "MoMo",
                Description = "Pay using your MoMo wallet.",
            },
            new()
            {
                Name = PaymentMethod.ZaloPay.ToString(),
                ImageSource = "/img/payment/logo-zalopay.webp",
                ImageAlt = "ZaloPay",
                DisplayName = "ZaloPay",
                Description = "Wallet, ATM cards, and linked banks.",
            },
        ];
    }

    private async Task<Dictionary<string, List<(string Bankcode, string Name)>>> GetZaloPayBankList(CancellationToken cxlTkn)
    {
        var appId = _paymentProviderOpts.ZaloPay.AppId.ToString();
        var reqTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var mac = ComputeHmacZaloPay($"{appId}|{reqTime}", _paymentProviderOpts.ZaloPay.Key1);

        var param = new Dictionary<string, string>
        {
            { "appid", appId },
            { "reqtime", reqTime },
            { "mac", mac }
        };
        var form = new FormUrlEncodedContent(param);

        using var client = new HttpClient();
        var zpBankListResMsg = await client.PostAsync(_paymentProviderOpts.ZaloPay.BankListEndpoint, form, cxlTkn);

        var zpBankListRes = JsonSerializer.Deserialize<ZaloPayBankListResponse>(await zpBankListResMsg.Content.ReadAsStreamAsync(cxlTkn), _zaloPayJsonOpts)
                            ?? throw new Exception("Could not read response for ZaloPay bank list.");

        var atmBanks = zpBankListRes.Banks
                        .GetValueOrDefault((int)ZaloPayBankListCategory.ATM)?
                        .Select(bank => (bank.Bankcode, bank.Name))
                        .ToList();

        var bankList = new Dictionary<string, List<(string Bankcode, string Name)>>();
        if (atmBanks != null && atmBanks.Count != 0)
        {
            bankList.Add("ZaloPay", atmBanks);
        }
        return bankList;
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

public class ZaloPayBankListResponse
{
    public int Returncode { get; set; }
    public string Returnmessage { get; set; } = string.Empty;
    public Dictionary<int, List<ZaloPayBankDto>> Banks { get; set; } = [];
}

public class ZaloPayBankDto
{
    public string Bankcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Displayorder { get; set; }
    public int Pmcid { get; set; }
    public long Minamount { get; set; }
    public long Maxamount { get; set; }
}

public enum ZaloPayBankListCategory
{
    Visa_Master_JCB = 36,
    BankAccount = 37,
    ZaloPay = 38,
    ATM = 39,
    Visa_Master_Debit = 41,
}

public class ZaloPayCreateTransactionResponse
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
