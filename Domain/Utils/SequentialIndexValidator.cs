namespace Domain.Utils;

public static class SequentialIndexValidator
{
    public static bool IsExact<T>(IEnumerable<T> items, Func<T, int> indexSelector, int startIndex = 1)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(indexSelector);
        if (startIndex < 1) return false;

        var indices = items.Select(indexSelector).Order().ToArray();
        return indices.Select((index, offset) => index == startIndex + offset).All(e => e);
    }

    public static void EnsureExact<T>(IEnumerable<T> items, Func<T, int> indexSelector, string collectionName, int startIndex = 1)
    {
        if (startIndex < 1) throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "The first index must be positive.");
        if (!IsExact(items, indexSelector, startIndex)) throw new InvalidOperationException($"{collectionName} must use an exact contiguous index sequence beginning at {startIndex}.");
    }
}
