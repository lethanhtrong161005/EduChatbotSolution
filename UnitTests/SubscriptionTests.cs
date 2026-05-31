using Business.Utils;
using Domain.Entities;

namespace UnitTests;

public class SubscriptionTests
{
    private readonly List<SubscriptionPlan> _plans = [];
    private readonly List<SubscriptionPlanOption> _opts = [];
    private readonly Guid _userId = Guid.NewGuid();

    private static void AssertSpansCoverTarget(
        UserSubscription target,
        IReadOnlyList<SubscriptionHelper.TimeSegment> segments)
    {
        Assert.That(
            segments.Select(e => e.TimeSpan).Aggregate((a, b) => a + b),
            Is.EqualTo(target.EndDate - target.StartDate));
    }

    [SetUp]
    public void Setup()
    {
        _plans.AddRange(
        [
            new SubscriptionPlan
            {
                Id = 1,
                Name = "Basic",
                Tier = 1,
            },
            new SubscriptionPlan
            {
                Id = 2,
                Name = "Advanced",
                Tier = 2,
            },
            new SubscriptionPlan
            {
                Id = 3,
                Name = "Premium",
                Tier = 3,
            },
            new SubscriptionPlan
            {
                Id = 4,
                Name = "Deluxe",
                Tier = 4,
            },
            new SubscriptionPlan
            {
                Id = 5,
                Name = "Ultra",
                Tier = 5,
            }
        ]);

        _opts.AddRange(
        [
            new SubscriptionPlanOption
            {
                Id = 1,
                SubscriptionPlan = _plans[0],
                Price = 9.99m,
            },
            new SubscriptionPlanOption
            {
                Id = 2,
                SubscriptionPlan = _plans[1],
                Price = 19.99m,
            },
            new SubscriptionPlanOption
            {
                Id = 3,
                SubscriptionPlan = _plans[2],
                Price = 29.99m,
            },
            new SubscriptionPlanOption
            {
                Id = 4,
                SubscriptionPlan = _plans[3],
                Price = 39.99m,
            },
            new SubscriptionPlanOption
            {
                Id = 5,
                SubscriptionPlan = _plans[4],
                Price = 49.99m,
            }
        ]);
    }

    [Test]
    public void GetOverlapTimeSegments_NoOverlap_ShouldReturnOneSegmentWithNoTier()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 2, 28),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
        };
        var existingSub2 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[3].Price,
                SubscriptionPlanOption = _opts[3],
            },
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 31),
        };

        var existingSubs = new List<UserSubscription> { existingSub1, existingSub2 };
        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, existingSubs);

        Assert.That(segments, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(targetSub.EndDate - targetSub.StartDate));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_FullOverlap_ShouldReturnMultipleZeroLengthSegments_And_OneWholeLengthSegmentWithTheHighestTier()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 7, 1),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 7, 1),
        };
        var existingSub2 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[3].Price,
                SubscriptionPlanOption = _opts[3],
            },
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 7, 1),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1, existingSub2]);

        Assert.That(segments, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(4));
            Assert.That(segments[1].Tier, Is.EqualTo(4));
            Assert.That(segments[2].Tier, Is.EqualTo(2));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(TimeSpan.Zero));
            Assert.That(segments[1].TimeSpan, Is.EqualTo(targetSub.EndDate - targetSub.StartDate));
            Assert.That(segments[2].TimeSpan, Is.EqualTo(TimeSpan.Zero));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_PartialOverlapAtStart_ShouldReturn2SegmentsWithCorrectTiers()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 2, 28),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 1, 15),
            EndDate = new DateTime(2026, 2, 15),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1]);

        Assert.That(segments, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(2));
            Assert.That(segments[1].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(existingSub1.EndDate - targetSub.StartDate));
            Assert.That(segments[1].TimeSpan, Is.EqualTo(targetSub.EndDate - existingSub1.EndDate));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_PartialOverlapAtEnd_ShouldReturn2SegmentsWithCorrectTiers()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 2, 28),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 2, 15),
            EndDate = new DateTime(2026, 3, 15),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1]);

        Assert.That(segments, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[1].Tier, Is.EqualTo(2));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(existingSub1.StartDate - targetSub.StartDate));
            Assert.That(segments[1].TimeSpan, Is.EqualTo(targetSub.EndDate - existingSub1.StartDate));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_PartialOverlapInMiddle_ShouldReturn3SegmentsWithCorrectTiers()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 4, 1),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 2, 15),
            EndDate = new DateTime(2026, 3, 15),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1]);

        Assert.That(segments, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[1].Tier, Is.EqualTo(2));
            Assert.That(segments[2].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(existingSub1.StartDate - targetSub.StartDate));
            Assert.That(segments[1].TimeSpan, Is.EqualTo(existingSub1.EndDate - existingSub1.StartDate));
            Assert.That(segments[2].TimeSpan, Is.EqualTo(targetSub.EndDate - existingSub1.EndDate));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_ComplexOverlaps_ShouldReturnCorrectSegments()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[4].Price,
                SubscriptionPlanOption = _opts[4],
            },
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 12, 1),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[3].Price,
                SubscriptionPlanOption = _opts[3],
            },
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 15),
        };
        var existingSub2 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[0].Price,
                SubscriptionPlanOption = _opts[0],
            },
            StartDate = new DateTime(2026, 1, 15),
            EndDate = new DateTime(2026, 3, 1),
        };
        var existingSub3 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 5, 1),
        };
        var existingSub4 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 7, 1),
        };
        var existingSub5 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 9, 1),
        };
        var existingSub6 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[0].Price,
                SubscriptionPlanOption = _opts[0],
            },
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 12, 1),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1, existingSub2, existingSub3, existingSub4, existingSub5, existingSub6]);

        Assert.That(segments, Has.Length.EqualTo(10));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(4));
            Assert.That(segments[1].Tier, Is.EqualTo(4));
            Assert.That(segments[0].TimeSpan, Is.EqualTo(existingSub2.EndDate - targetSub.StartDate));
            Assert.That(segments[1].TimeSpan, Is.EqualTo(existingSub3.StartDate - existingSub2.EndDate));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }

    [Test]
    public void GetOverlapTimeSegments_SameTierOverlap_ShouldReturnMultipleSegmentsWithSameTiers()
    {
        var targetSub = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[2].Price,
                SubscriptionPlanOption = _opts[2],
            },
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 4, 1),
        };

        var existingSub1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 1, 15),
            EndDate = new DateTime(2026, 3, 1),
        };
        var existingSub2 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            SubscriptionPurchase = new SubscriptionPurchase
            {
                Id = Guid.NewGuid(),
                ChargedAmount = _opts[1].Price,
                SubscriptionPlanOption = _opts[1],
            },
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 3, 15),
        };

        var segments = SubscriptionHelper.GetOverlapTimeSegments(targetSub, [existingSub1, existingSub2]);

        Assert.That(segments, Has.Length.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(segments[0].Tier, Is.EqualTo(int.MinValue));
            Assert.That(segments[1].Tier, Is.EqualTo(2));
            Assert.That(segments[2].Tier, Is.EqualTo(2));
            Assert.That(segments[3].Tier, Is.EqualTo(2));
            Assert.That(segments[4].Tier, Is.EqualTo(int.MinValue));
        });
        AssertSpansCoverTarget(targetSub, segments);
    }
}
