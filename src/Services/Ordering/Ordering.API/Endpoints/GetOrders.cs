using BuildingBlocks.Pagination;
using Ordering.Application.Orders.Queries.GetOrders;

namespace Ordering.API.Endpoints;

//- Accepts pagination parameters.
//- Constructs a GetOrdersQuery with these parameters.
//- Retrieves the data and returns it in a paginated format.

//public record GetOrdersRequest(PaginationRequest PaginationRequest);
public record GetOrdersResponse(PaginatedResult<OrderDto> Orders);

public class GetOrders : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        /*
            - [AsParameters] tells ASP.NET Core to treat the properties inside PaginationRequest as separate endpoint parameters.
            - For example: 
                public record PaginationRequest(int PageIndex, int PageSize);
            - A request like: 
                GET /orders?pageIndex=0&pageSize=10
              will produce approximately:
                request.PageIndex == 0
                request.PageSize == 10
            
            - Without [AsParameters], ASP.NET Core may treat PaginationRequest as one complex parameter rather than expanding and binding its individual properties.
         */
        app.MapGet("/orders", async ([AsParameters] PaginationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new GetOrdersQuery(request));

            var response = result.Adapt<GetOrdersResponse>();

            return Results.Ok(response);
        })
        .WithName("GetOrders")
        .Produces<GetOrdersResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get Orders")
        .WithDescription("Get Orders");
    }
}
