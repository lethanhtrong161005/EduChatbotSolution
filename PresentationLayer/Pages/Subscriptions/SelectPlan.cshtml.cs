using AutoMapper;
using Domain.Contracts;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;
using System.Security.Claims;

namespace Presentation.Pages.Subscriptions;

/// <summary>
/// Displays available subscription plans and handles plan purchases.
/// </summary>
[Authorize]
public class SelectPlanModel(
    ISubscriptionService subscriptionService,
    IMapper mapper) : PageModel
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the subscription selection view model rendered by the page.
    /// </summary>
    public SubscriptionSelectPlanVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads available subscription plans and the current user subscription.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The subscription selection page.</returns>
    /// <exception cref="UserClaimException">Thrown when the current user has no valid identifier.</exception>
    public async Task<IActionResult> OnGetAsync(CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");
        }

        var plans = await _subscriptionService.GetPlansAsync(cxlTkn);
        var currentSub = await _subscriptionService.GetSubscriptionOfUserAsync(userId, cxlTkn);

        ViewModel = new SubscriptionSelectPlanVm
        {
            Plans = _mapper.Map<IEnumerable<PlanCardVm>>(plans),
            CurrentSubscription = currentSub == null ? null : _mapper.Map<CurrentSubscriptionVm>(currentSub),
        };

        return Page();
    }

    /// <summary>
    /// Subscribes the current user to the selected plan option and redirects to payment.
    /// </summary>
    /// <param name="id">The selected plan option identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A redirect to the payment method page.</returns>
    /// <exception cref="UserClaimException">Thrown when the current user has no valid identifier.</exception>
    public async Task<IActionResult> OnPostSubscribeAsync(int id, CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");
        }

        var subscription = await _subscriptionService.SubscribeUserToPlanAsync(userId, id, createOrder: true, cxlTkn);
        return RedirectToPage("/Payment/SelectMethod", new { id = subscription.Order!.Id });
    }
}
