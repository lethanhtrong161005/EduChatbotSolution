namespace DataAccessLayer.Common;

public class PaginatedList<T> : List<T>
{
    public int PageSize { get; private set; }
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public PaginatedList() { }

    public PaginatedList(List<T> items, int count, int pageSize, int pageIndex)
    {
        AddRange(items);

        if (pageSize > 0 && pageIndex > 0)
        {
            PageSize = pageSize;
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }
    }
}
