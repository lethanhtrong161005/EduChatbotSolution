using AutoMapper;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
[Route("plans")]
public class SubscriptionsController(ISubscriptionService subService, IMapper mapper) : Controller
{
    private readonly ISubscriptionService _subscriptionService = subService;
    private readonly IMapper _mapper = mapper;

    [HttpGet("select")]
    public async Task<IActionResult> SelectPlan(CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");

        var plans = await _subscriptionService.GetPlansAsync(cxlTkn);
        var currentSub = await _subscriptionService.GetSubscriptionOfUserAsync(userId, cxlTkn);

        var planSelectVm = new SubscriptionSelectPlanVm
        {
            Plans = _mapper.Map<IEnumerable<PlanCardVm>>(plans),
            CurrentSubscription = currentSub == null ? null : _mapper.Map<CurrentSubscriptionVm>(currentSub),
        };

        return View(planSelectVm);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(int id, CancellationToken cxlTkn)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new UserClaimException("Current user does not have a valid ID. Try signing in again.");
        }
        var subscription = await _subscriptionService.SubscribeUserToPlanAsync(userId, id, createOrder: true, cxlTkn);

        return RedirectToAction(
            nameof(PaymentController.SelectMethod),
            "Payment",
            new { subscription.Order!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subscriptionService.GetSubscriptionAsync(id.Value, cxlTkn);
        if (userSubscription == null)
        {
            return NotFound();
        }

        return View(userSubscription);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subscription userSubscription, CancellationToken cxlTkn)
    {
        if (!ModelState.IsValid)
        {
            return View(userSubscription);
        }

        await _subscriptionService.CreateSubscriptionAsync(userSubscription, cxlTkn);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subscriptionService.GetSubscriptionAsync(id.Value, cxlTkn);
        if (userSubscription == null)
        {
            return NotFound();
        }
        return View(userSubscription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Subscription userSubscription, CancellationToken cxlTkn)
    {
        if (id != userSubscription.Id)
            throw new BadRequestException("IDs of path and data object do not match.");

        if (!ModelState.IsValid)
            return View(userSubscription);

        var userSubToUpdate = await _subscriptionService.GetSubscriptionAsync(id, cxlTkn);
        if (userSubToUpdate == null)
            return NotFound();

        await _subscriptionService.UpdateSubscriptionAsync(userSubscription, cxlTkn);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subscriptionService.GetSubscriptionAsync(id.Value, cxlTkn);
        if (userSubscription == null)
            return NotFound();

        return View(userSubscription);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cxlTkn)
    {
        var userSubscription = await _subscriptionService.GetSubscriptionAsync(id, cxlTkn);
        if (userSubscription == null)
            return NotFound();

        await _subscriptionService.DeleteSubscriptionAsync(id, cxlTkn);

        return RedirectToAction(nameof(Index));
    }
}
