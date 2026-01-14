using System.Collections.Generic;
using System.Linq;
using Xunit;
using PaymProdNet9.Models;
using PaymProdNet9.Services;

namespace PaymProdNet9.Tests;

public class ProductReportFormatterTests
{
    private readonly List<Measure> _measures;
    private readonly ProductReportFormatter _formatter;

    public ProductReportFormatterTests()
    {
        _measures = new List<Measure>
        {
            new Measure { Name = "г", Id = 1, Fass = 1, FassIzmer = "г", RoundingPrecision = 0 },
            new Measure { Name = "кг", Id = 2, Fass = 1000, FassIzmer = "г", RoundingPrecision = 2 },
            new Measure { Name = "шт", Id = 3, Fass = 1, FassIzmer = "шт", RoundingPrecision = 0 }
        };
        _formatter = new ProductReportFormatter(_measures);
    }

    [Fact]
    public void IntermediateTotals_ShouldMatchSumOfRecalculatedPrices()
    {
        // This test verifies the logic used for "Subtotals" (Intermediate Totals) in the Goods Report.
        // The requirement is that the subtotal row sums up the "Cost" column.
        // The "Cost" column uses Recalculated Price based on the Rounded Amount displayed to the user.
        // This test ensures that if we have a list of products, the sum of their Recalculated Costs is calculated correctly.

        // Arrange
        // Product 1: 1.234 kg -> displayed as 1.24 kg
        // Price per kg: 1000
        // Exact Cost: 1234
        // Displayed Cost should be: 1.24 * 1000 = 1240
        var p1 = new GroupedProduct
        {
            Name = "Prod1",
            Mera = "кг",
            TotalWeight = 1.234m, // Already in KG
            TotalPrice = 1234, 
            Fass = 0
        };

        // Product 2: 0.555 kg -> displayed as 0.56 kg
        // Price per kg: 2000
        // Exact Cost: 1110
        // Displayed Cost should be: 0.56 * 2000 = 1120
        var p2 = new GroupedProduct
        {
            Name = "Prod2",
            Mera = "кг",
            TotalWeight = 0.555m, // Already in KG
            TotalPrice = 1110, 
            Fass = 0
        };

        var products = new List<GroupedProduct> { p1, p2 };

        // Ensure "кг" measure exists and doesn't trigger scaling (treat as base or top)
        // Updating _measures in constructor or setup if possible, but here we used shared _measures.
        // The shared _measures has KG with Fass=1000.
        // If TotalWeight=1.234 and Fass=1000. 1.234 < 1000. Loop won't run. 
        // So it stays as "kg" and value 1.234.
        // This is what we want.

        // Act
        decimal groupTotal = 0;
        foreach (var product in products)
        {
            // Logic mirrored from ProductsReportPage.xaml.cs using the Formatter
            var (_, _, roundedAmount, _) = _formatter.FormatAmountWithRoundedValue(product);
            var recalculatedPrice = _formatter.RecalculatePrice(product, roundedAmount);
            groupTotal += recalculatedPrice;
        }

        // Assert
        // P1: Rounded 1.24. Recalc: (1234 / 1.234) * 1.24 = 1000 * 1.24 = 1240.
        // P2: Rounded 0.56. Recalc: (1110 / 0.555) * 0.56 = 2000 * 0.56 = 1120.
        // Total: 1240 + 1120 = 2360.
        
        Assert.Equal(2360, groupTotal);
    }

    [Fact]
    public void RecalculatePrice_ShouldUseRoundedAmount()
    {
        // Arrange
        var p = new GroupedProduct
        {
            Name = "Test",
            Mera = "кг",
            TotalWeight = 1.234m, // 1.234 kg
            TotalPrice = 1234, 
            Fass = 0
        };

        // Act
        var (_, _, roundedAmount, _) = _formatter.FormatAmountWithRoundedValue(p);
        var recalcPrice = _formatter.RecalculatePrice(p, roundedAmount);

        // Assert
        Assert.Equal(1.24, roundedAmount);
        Assert.Equal(1240, recalcPrice);
    }
}
