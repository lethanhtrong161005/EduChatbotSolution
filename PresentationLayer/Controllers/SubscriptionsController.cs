using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
[Route("/plans/[action]")]
public class SubscriptionsController(ISubscriptionService subService, IMapper mapper) : Controller
{
    private readonly ISubscriptionService _subService = subService;
    private readonly IMapper _mapper = mapper;

    [HttpGet("~/plans")]
    public async Task<IActionResult> Index()
    {
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Id = 1,
                Name = "Basic",
                Tier = 1,
                Description = "Perfect for casual learners.",
                DailyMessageQuota = 100,
                ChatSessionLimit = 10,
                DailyFileUploadQuota = 5,
                FileLibraryLimit = 20,
                AllowAdvancedModels = false,

                SubscriptionPlanOptions =
                [
                    new()
                    {
                        Id = 101,
                        OptionName = "Monthly",
                        DurationDays = 30,
                        Price = 10_000m
                    }
                ]
            },

            new()
            {
                Id = 2,
                Name = "Advanced",
                Tier = 2,
                Description = "More conversations and file storage.",
                DailyMessageQuota = 500,
                ChatSessionLimit = 50,
                DailyFileUploadQuota = 20,
                FileLibraryLimit = 100,
                AllowAdvancedModels = false,

                SubscriptionPlanOptions =
                [
                    new()
                    {
                        Id = 201,
                        OptionName = "Monthly",
                        DurationDays = 30,
                        Price = 20_000m
                    },

                    new()
                    {
                        Id = 202,
                        OptionName = "Quarterly",
                        DurationDays = 90,
                        Price = 55_000m
                    }
                ]
            },

            new()
            {
                Id = 3,
                Name = "Premium",
                Tier = 3,
                Description = "Most popular plan for serious students.",
                DailyMessageQuota = 2_000,
                ChatSessionLimit = 200,
                DailyFileUploadQuota = 100,
                FileLibraryLimit = 500,
                AllowAdvancedModels = true,

                SubscriptionPlanOptions =
                [
                    new()
                    {
                        Id = 301,
                        OptionName = "Monthly",
                        DurationDays = 30,
                        Price = 30_000m
                    },

                    new()
                    {
                        Id = 302,
                        OptionName = "Semi-Annual",
                        DurationDays = 180,
                        Price = 160_000m
                    },

                    new()
                    {
                        Id = 303,
                        OptionName = "Annual",
                        DurationDays = 365,
                        Price = 300_000m
                    }
                ]
            },

            new()
            {
                Id = 4,
                Name = "Deluxe",
                Tier = 4,
                Description = "For power users who need higher limits.",
                DailyMessageQuota = 10_000,
                ChatSessionLimit = 1_000,
                DailyFileUploadQuota = 500,
                FileLibraryLimit = 2_000,
                AllowAdvancedModels = true,

                SubscriptionPlanOptions =
                [
                    new()
                    {
                        Id = 401,
                        OptionName = "Quarterly",
                        DurationDays = 90,
                        Price = 150_000m
                    },

                    new()
                    {
                        Id = 402,
                        OptionName = "Annual",
                        DurationDays = 365,
                        Price = 600_000m
                    }
                ]
            },

            new()
            {
                Id = 5,
                Name = "Ultra",
                Tier = 5,
                Description = "Everything included. No practical limits.",
                DailyMessageQuota = AppConstants.UnlimitedQuota,
                ChatSessionLimit = AppConstants.UnlimitedQuota,
                DailyFileUploadQuota = AppConstants.UnlimitedQuota,
                FileLibraryLimit = AppConstants.UnlimitedQuota,
                AllowAdvancedModels = true,

                SubscriptionPlanOptions =
                [
                    new()
                    {
                        Id = 501,
                        OptionName = "Annual",
                        DurationDays = 365,
                        Price = 1_000_000m
                    }
                ]
            }
        };

        var planVms = _mapper.Map<IEnumerable<SubscriptionPlanCardVm>>(plans);
        var planSelectVm = new SelectSubscriptionPlanVm
        {
            Plans = planVms,
        };

        return View(planSelectVm);
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
