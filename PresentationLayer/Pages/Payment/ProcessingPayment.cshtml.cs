using AutoMapper;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Payment;

/// <summary>
/// Displays the payment verification page after returning from a provider.
/// </summary>
[Authorize]
public class ProcessingPaymentModel(
    IPaymentService paymentService,
    IMapper mapper) : PageModel
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the payment processing view model rendered by the page.
    /// </summary>
    public PaymentProcessingVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads the payment being verified by the background callback.
    /// </summary>
    /// <param name="transactionId">The provider transaction code.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The processing payment page.</returns>
    /// <exception cref="BadRequestException">Thrown when the transaction code is missing.</exception>
    public async Task<IActionResult> OnGetAsync([FromQuery] string transactionId, CancellationToken cxlTkn)
    {
        if (string.IsNullOrEmpty(transactionId))
        {
            throw new BadRequestException("Missing payment transaction code.");
        }

        var payment = await GetAndValidatePaymentAsync(transactionId, cxlTkn);
        ViewModel = _mapper.Map<PaymentProcessingVm>(payment);
        return Page();
    }

    /// <summary>
    /// Loads the payment and confirms that it belongs to the current user.
    /// </summary>
    /// <param name="txnCode">The external provider transaction code.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The validated payment.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the payment cannot be found.</exception>
    /// <exception cref="UserClaimException">Thrown when the current user cannot access the payment.</exception>
    private async Task<Domain.Entities.Payment> GetAndValidatePaymentAsync(string txnCode, CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();

        var payment = (await _paymentService.GetAsync(
                filter: e => e.ExternalTransactionCode == txnCode,
                includeProperties:
                [
                    nameof(Domain.Entities.Payment.Order)
                    + "."
                    + nameof(Order.Subscription)
                    + "."
                    + nameof(Subscription.PlanOption)
                    + "."
                    + nameof(PlanOption.Plan)
                ],
                cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No transaction matching the provided transaction code was found.");

        if (payment.Order.UserId != userId)
        {
            throw new UserClaimException("You do not have permission to access this transaction.");
        }

        return payment;
    }
}
