using AutoMapper;
using Business.Services.Subscriptions.ExternalPayment;
using Domain.Constants;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Presentation.Extensions;
using Presentation.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZaloPayCreateTransactionResponse = Presentation.DTOs.ZaloPayCreateTransactionResponse;
using ZaloPayInitTransactionReturnCode = Presentation.DTOs.ZaloPayInitTransactionReturnCode;

namespace Presentation.Pages.Payment;

/// <summary>
/// Displays payment provider choices and starts the selected payment transaction.
/// </summary>
[Authorize]
public class SelectMethodModel(
    IOrderService orderService,
    IPaymentService paymentService,
    IOptions<PaymentProviderOptions> paymentProviderOptions,
    IMapper mapper) : PageModel
{
    private readonly IOrderService _orderService = orderService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly PaymentProviderOptions _paymentProviderOpts = paymentProviderOptions.Value;
    private readonly IMapper _mapper = mapper;

    private readonly JsonSerializerOptions _zaloPayJsonOpts = new()
    {
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const string DefaultAppUser = "EduChatAI_User";

    /// <summary>
    /// Gets the payment selection view model rendered by the page.
    /// </summary>
    public PaymentSelectMethodVm ViewModel { get; private set; } = new()
    {
        PendingOrder = new OrderCheckoutVm(),
    };

    /// <summary>
    /// Loads the selectable payment methods for a pending order.
    /// </summary>
    /// <param name="id">The pending order identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The payment method page.</returns>
    /// <exception cref="BadRequestException">Thrown when the order ID is empty.</exception>
    /// <exception cref="EntityConflictException">Thrown when the order is not pending payment.</exception>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        if (id == Guid.Empty)
        {
            throw new BadRequestException("Missing order ID.");
        }

        var order = await GetAndValidateOrderAsync(id, cxlTkn);
        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new EntityConflictException("Only pending orders can be paid for.", nameof(Order.Status));
        }

        ViewModel = new PaymentSelectMethodVm
        {
            PaymentMethods = GetPaymentMethods(),
            PendingOrder = _mapper.Map<OrderCheckoutVm>(order),
        };

        return Page();
    }

    /// <summary>
    /// Creates a provider payment transaction for the selected payment method.
    /// </summary>
    /// <param name="id">The pending order identifier.</param>
    /// <param name="viewModel">The posted payment selection form data.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A redirect to the payment provider.</returns>
    /// <exception cref="BadRequestException">Thrown when the order ID is missing or mismatched.</exception>
    /// <exception cref="EntityValidationException">Thrown when the order data is malformed.</exception>
    /// <exception cref="EntityConflictException">Thrown when the order cannot be paid.</exception>
    public async Task<IActionResult> OnPostMakePaymentAsync(
        Guid id,
        [Bind(Prefix = nameof(ViewModel))] PaymentSelectMethodVm viewModel,
        CancellationToken cxlTkn)
    {
        ViewModel = viewModel;

        if (id == Guid.Empty)
        {
            throw new BadRequestException("Missing order ID.");
        }

        if (id != ViewModel.PendingOrder.Id)
        {
            throw new BadRequestException("Mismatched order ID.");
        }

        if (!ModelState.IsValid)
        {
            throw new EntityValidationException("Invalid payment method and/or order. Please try again.", nameof(ViewModel));
        }

        var order = await GetAndValidateOrderAsync(id, cxlTkn);
        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new EntityConflictException("Only pending orders can be paid for.", nameof(Order.Status));
        }

        switch (ViewModel.SelectedMethod)
        {
            case PaymentMethod.ZaloPay:
                var (zpInitTxnRes, extTxnCode) = await CreateZaloPayTransactionAsync(order, cxlTkn);
                await _paymentService.CreatePendingPaymentAsync(order.Id, PaymentMethod.ZaloPay, extTxnCode, cxlTkn);
                return Redirect(zpInitTxnRes.OrderUrl);
        }

        throw new Exception("Unsupported payment method.");
    }

    /// <summary>
    /// Loads the order and confirms that it belongs to the current user.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The validated order.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the order cannot be found.</exception>
    /// <exception cref="UserClaimException">Thrown when the current user cannot access the order.</exception>
    private async Task<Order> GetAndValidateOrderAsync(Guid orderId, CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();

        var order = await _orderService.GetByIdAsync(orderId, cxlTkn)
            ?? throw new EntityNotFoundException("No order matching the provided ID was found.");

        if (order.UserId != userId)
        {
            throw new UserClaimException("You do not have permission to access this order.");
        }

        return order;
    }

    /// <summary>
    /// Creates a ZaloPay payment transaction request for the order.
    /// </summary>
    /// <param name="order">The order being paid.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The provider response and external transaction code.</returns>
    /// <exception cref="Exception">Thrown when the provider rejects or cannot parse the transaction.</exception>
    private async Task<(ZaloPayCreateTransactionResponse Response, string TransactionCode)> CreateZaloPayTransactionAsync(
        Order order,
        CancellationToken cxlTkn)
    {
        var orderId = order.Id;
        var orderTotal = order.ChargedAmount;

        var appId = _paymentProviderOpts.ZaloPay.AppId;
        var appUser = User.FindFirstValue(ClaimTypes.Name) ?? DefaultAppUser;
        var appTransId = GetTransactionCode(orderId);
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var amount = (long)orderTotal;
        var item = "[]";
        var description = $"EduChatAI - Payment for order #{appTransId}";
        var redirectUrl = _paymentProviderOpts.ZaloPay.RedirectUrlBase + "?transaction-id=" + appTransId;
        var embedData = $"{{\"redirecturl\": \"{redirectUrl}\"}}";
        var bankCode = "";
        var mac = ComputeHmacZaloPay(
            $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}",
            _paymentProviderOpts.ZaloPay.Key1);
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

        using var form = new FormUrlEncodedContent(param);
        using var client = new HttpClient();
        var zpInitTxnResMsg = await client.PostAsync(_paymentProviderOpts.ZaloPay.CreateTransactionEndpoint, form, cxlTkn);

        if (!zpInitTxnResMsg.IsSuccessStatusCode)
        {
            throw new Exception("Could not create ZaloPay transaction.");
        }

        var zpCreateTxnRes = JsonSerializer.Deserialize<ZaloPayCreateTransactionResponse>(
            await zpInitTxnResMsg.Content.ReadAsStreamAsync(cxlTkn),
            _zaloPayJsonOpts)
            ?? throw new Exception("Could not read response for ZaloPay transaction creation.");

        if (zpCreateTxnRes.ReturnCode != (int)ZaloPayInitTransactionReturnCode.Success)
        {
            throw new Exception($"Failed to create ZaloPay transaction: {zpCreateTxnRes.ReturnMessage} - {zpCreateTxnRes.SubReturnMessage}");
        }

        return (zpCreateTxnRes, appTransId);
    }

    /// <summary>
    /// Builds the external transaction code expected by ZaloPay.
    /// </summary>
    /// <param name="orderId">The order identifier encoded into the transaction code.</param>
    /// <returns>The provider transaction code.</returns>
    private static string GetTransactionCode(Guid orderId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var curDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).ToString("yyMMdd");
        var code = orderId.ToString("N");
        return curDate + "_" + code;
    }

    /// <summary>
    /// Builds the payment methods displayed by the page.
    /// </summary>
    /// <returns>The supported payment method cards.</returns>
    private static List<PaymentMethodVm> GetPaymentMethods()
    {
        return
        [
            new()
            {
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
