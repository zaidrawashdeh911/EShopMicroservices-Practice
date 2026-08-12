namespace Catalog.API.Products.CreateProduct;

public record CreateProductCommand(string Name, List<string> Category, string Description, string ImageFile,decimal Price)
    : ICommand<CreateProductResult>;
public record CreateProductResult(Guid Id);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Product category is required.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Product description is required.");
        RuleFor(x => x.ImageFile).NotEmpty().WithMessage("Product image file is required.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Product price must be greater than zero.");
    }
}

internal class CreateProductCommandHandler(IDocumentSession session, IValidator<CreateProductCommand> validator) : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // Implement the logic to create a product here
        // First step: Create a new product entity from command object
        // Second step: Save it to the database
        // Third step: Return the CreateProductResult object

        // Extra step: Validate the command object using the validator
        var result = await validator.ValidateAsync(command, cancellationToken);
        var erros = result.Errors.Select(x => x.ErrorMessage).ToList();
        if (erros.Any())
        {
            throw new ValidationException(erros.FirstOrDefault());
        }

        // First step:
        var product = new Product
        {
            //Id = Guid.NewGuid(),
            Name = command.Name,
            Category = command.Category,
            Description = command.Description,
            ImageFile = command.ImageFile,
            Price = command.Price
        };
        // Second step:
        session.Store(product);
        await session.SaveChangesAsync(cancellationToken);

        // Third step:
        return new CreateProductResult(product.Id);
    }
}
