namespace Basket.API.Basket.GetBasket;

public record GetBasketQuery(string Username) : IQuery<GetBasketResult>;
public record GetBasketResult(ShoppingCart Cart);
public class GetBasketQueryHandler : IQueryHandler<GetBasketQuery, GetBasketResult>
{
    public async Task<GetBasketResult> Handle(GetBasketQuery query, CancellationToken cancellationToken)
    {
        //TODO: get basket from database or cache
        //var basket = await _repository.GetBasketAsync(query.Username);

        return new GetBasketResult(new ShoppingCart("swn"));
    }
}
