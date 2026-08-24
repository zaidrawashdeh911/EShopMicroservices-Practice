namespace BuildingBlocks.Pagination;

public class PaginatedResult<TEntity>
    (int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
    where TEntity : class
{
    public int PageIndex { get; } = pageIndex; //Current page index, starting from zero
    public int PageSize { get; } = pageSize; //Number of items per page
    public long Count { get; } = count; //Total number of items in the data set
    public IEnumerable<TEntity> Data { get; } = data; //The actual data for the current page
}