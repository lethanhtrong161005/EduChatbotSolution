using System.Collections;

namespace Domain.Common;

public class PaginatedEnumerable<T> : IEnumerable<T>
{
    public IEnumerable<T> Items { get; private set; } = [];
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public PaginatedEnumerable() { }

    public PaginatedEnumerable(IEnumerable<T> items, int count, int pageSize, int pageIndex)
    {
        Items = items;

        if (pageSize > 0 && pageIndex > 0)
        {
            PageSize = pageSize;
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
