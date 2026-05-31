using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

public class SubscriptionsController(ISubscriptionService subService) : Controller
{
    private readonly ISubscriptionService _subService = subService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _subService.GetSubscriptionsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subService.GetSubscriptionAsync(id.Value, cxlTkn);
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
    public async Task<IActionResult> Create(UserSubscription userSubscription, CancellationToken cxlTkn)
    {
        if (!ModelState.IsValid)
        {
            return View(userSubscription);
        }

        await _subService.CreateSubscriptionAsync(userSubscription, cxlTkn);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subService.GetSubscriptionAsync(id.Value, cxlTkn);
        if (userSubscription == null)
        {
            return NotFound();
        }
        return View(userSubscription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserSubscription userSubscription, CancellationToken cxlTkn)
    {
        if (id != userSubscription.Id)
            throw new BadRequestException("IDs of path and data object do not match.");

        if (!ModelState.IsValid)
            return View(userSubscription);

        var userSubToUpdate = await _subService.GetSubscriptionAsync(id, cxlTkn);
        if (userSubToUpdate == null)
            return NotFound();

        await _subService.UpdateSubscriptionAsync(userSubscription, cxlTkn);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id, CancellationToken cxlTkn)
    {
        if (id == null)
            throw new BadRequestException("Subscription plan ID required.");

        var userSubscription = await _subService.GetSubscriptionAsync(id.Value, cxlTkn);
        if (userSubscription == null)
            return NotFound();

        return View(userSubscription);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cxlTkn)
    {
        var userSubscription = await _subService.GetSubscriptionAsync(id, cxlTkn);
        if (userSubscription == null)
            return NotFound();

        await _subService.DeleteSubscriptionAsync(id, cxlTkn);

        return RedirectToAction(nameof(Index));
    }
}
