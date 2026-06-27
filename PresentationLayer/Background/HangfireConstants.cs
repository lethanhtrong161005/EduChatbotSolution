namespace Presentation.Background;

public static class HangfireConstants
{
    public const string HighPriorityQueue = "0_high";
    public const string MediumPriorityQueue = "1_medium";
    public const string LowPriorityQueue = "2_low";

    public static readonly IReadOnlyList<string> Queues =
    [
        HighPriorityQueue,
        MediumPriorityQueue,
        LowPriorityQueue,
    ];
}
