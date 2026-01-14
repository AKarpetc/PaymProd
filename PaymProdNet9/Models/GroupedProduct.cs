
namespace PaymProdNet9.Models;

public record GroupedProduct
{
    public string Name { get; init; } = string.Empty;
    public decimal TotalWeight { get; init; }
    public decimal TotalPackages { get; init; }
    public string? FassIz { get; init; }
    public string? Mera { get; init; }
    public decimal Fass { get; init; }
    public decimal Price { get; init; }
    public decimal TotalPrice { get; init; }
}
