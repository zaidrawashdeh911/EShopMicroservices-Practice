using BuildingBlocks.Pagination;

namespace Ordering.Application.Orders.Queries.GetOrders;
public class GetOrdersHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetOrdersQuery, GetOrdersResult>
{
    public async Task<GetOrdersResult> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        // get orders with pagination
        // return result

        var pageIndex = query.PaginationRequest.PageIndex;
        var pageSize = query.PaginationRequest.PageSize;

        var totalCount = await dbContext.Orders.LongCountAsync(cancellationToken);

        var orders = await dbContext.Orders
                       .Include(o => o.OrderItems)
                       .OrderBy(o => o.OrderName.Value)
                       .Skip(pageSize * pageIndex) //Example1: size=10, index=0, skip 0 and take 10
                       //Example2: size=10, index=1, skip first 10 and take the next 10
                       //Example3: size=10, index = 10, skip first 100 and take the next 10
                       .Take(pageSize)
                       .ToListAsync(cancellationToken);

        return new GetOrdersResult(
            new PaginatedResult<OrderDto>(
                pageIndex,
                pageSize,
                totalCount,
                orders.ToOrderDtoList()));        
    }
}