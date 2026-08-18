using Discount.Grpc.Data;
using Discount.Grpc.Models;
using Grpc.Core;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Services;

public class DiscountService(DiscountContext dbContext, ILogger<DiscountService> logger)
    : DiscountProtoService.DiscountProtoServiceBase
{
    public override async Task<CouponModel> GetDiscount(
        GetDiscountRequest request,
        ServerCallContext context)
    {
        var coupon = await dbContext.Coupons
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName, context.CancellationToken);

        coupon ??= new Coupon
        {
            ProductName = "No Discount",
            Description = "No Discount",
            Amount = 0
        };

        logger.LogInformation(
            "Discount retrieved for ProductName {ProductName}, Amount {Amount}",
            coupon.ProductName,
            coupon.Amount);

        return coupon.Adapt<CouponModel>();
    }

    public override async Task<CouponModel> CreateDiscount(
        CreateDiscountRequest request,
        ServerCallContext context)
    {
        var coupon = request.Coupon.Adapt<Coupon>();
        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Discount created for ProductName {ProductName}",
            coupon.ProductName);

        return coupon.Adapt<CouponModel>();
    }

    public override async Task<CouponModel> UpdateDiscount(
        UpdateDiscountRequest request,
        ServerCallContext context)
    {
        var coupon = request.Coupon.Adapt<Coupon>();
        dbContext.Coupons.Update(coupon);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Discount updated for ProductName {ProductName}",
            coupon.ProductName);

        return coupon.Adapt<CouponModel>();
    }

    public override async Task<DeleteDiscountResponse> DeleteDiscount(
        DeleteDiscountRequest request,
        ServerCallContext context)
    {
        var coupon = await dbContext.Coupons
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName, context.CancellationToken);

        if (coupon is null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound,
                $"Discount with ProductName={request.ProductName} was not found."));
        }

        dbContext.Coupons.Remove(coupon);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Discount deleted for ProductName {ProductName}",
            request.ProductName);

        return new DeleteDiscountResponse { Success = true };
    }
}
