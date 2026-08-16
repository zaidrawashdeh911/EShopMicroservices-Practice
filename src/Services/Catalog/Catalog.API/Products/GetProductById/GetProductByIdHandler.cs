namespace Catalog.API.Products.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<GetProdutByIdResult>;
public record GetProdutByIdResult(Product Product);
internal class GetProductByIdQueryHandler
    (IDocumentSession session) 
    : IQueryHandler<GetProductByIdQuery, GetProdutByIdResult>
{
    public async Task<GetProdutByIdResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<Product>(query.Id, cancellationToken);

        if (product is null)
        {
            throw new ProductNotFoundException(query.Id);
        }

        return new GetProdutByIdResult(product);
    }
}
