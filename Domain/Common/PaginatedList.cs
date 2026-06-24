namespace Domain.Common;

/// <summary>
/// A generic list that supports pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PaginatedList<T> : List<T>
{
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    public PaginatedList() { }

    public PaginatedList(List<T> items, int count, int pageSize, int pageIndex)
    {
        AddRange(items);

        if (pageSize > 0 && pageIndex > 0)
        {
            PageSize = pageSize;
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
        }
    }

    public static implicit operator PaginatedList<T>(PaginatedEnumerable<T> enumerable)
    {
        return new PaginatedList<T>(
            [.. enumerable.Items],
            enumerable.TotalCount,
            enumerable.PageSize,
            enumerable.PageIndex);
    }
}
