using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class ProductReportTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ProductRepository _prodRepo;
    private readonly ProductReportCalculationService _service;

    public ProductReportTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Report_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _prodRepo = new ProductRepository();
        _service = new ProductReportCalculationService();
    }

    public void Dispose()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
        }
    }

    [Fact]
    public void CalculateSummary_ShouldSumQuantitiesAndPrices_ForStandardDish()
    {
        // Arrange
        // Create a product: Price 10 per unit (gram/piece)
        var prodId = _prodRepo.AddProduct("Beef", 1, 1, 10, 1, price: 10);

        // Create a Menu Item (Dish) with 10 portions
        // Dish has 100g of Beef
        var item = new MenuDel_act
        {
            Del_id = 1, // Positive ID = Dish
            Countpor = 10,
            Idmen = 1, // Menu Context
            Lcomp = new List<Components>
            {
                new()
                {
                    Prodid = prodId,
                    Ves = 100, // 100g per portion
                    Name = "Beef",
                    Fass = 0 // No packaging
                }
            }
        };

        // Expected:
        // Total Weight = 100g * 10 portions = 1000g
        // Total Price = 1000g * 10 per unit = 10000

        // Act
        var result = _service.CalculateSummary(new[] { item }, 1);

        // Assert
        Assert.Single(result);
        var reportItem = result.First();
        Assert.Equal(1000, reportItem.Itog);
        Assert.Equal(10000, reportItem.TotalPrice);
    }

    [Fact]
    public void CalculateSummary_ShouldHandle_DirectProductWithPackaging()
    {
        // Arrange
        // Product: "Cola", Fass=0.5 (Pack size), Price=50 (per pack? Wait, logic says Price per unit usually?)
        // Let's verify MenuPriceService logic.
        // MenuPriceService.GetUnitPrice returns Price from Producrs table.

        var prodId =
            _prodRepo.AddProduct("Cola", 1, 1, 1, 1, price: 50); // Price 50 per unit (usually per Fass if packaged)

        // Direct Product Item (Negative Del_id)
        var item = new MenuDel_act
        {
            Del_id = -prodId,
            Countpor = 5, // 5 portions/bottles
            Idmen = 1,
            Lcomp = new List<Components>
            {
                new()
                {
                    Prodid = prodId,
                    Ves = 5, // 5 total units (logic for direct product puts total in Ves)
                    Name = "Cola",
                    Fass = 1 // Pack size 1
                }
            }
        };

        // Act
        var result = _service.CalculateSummary(new[] { item }, 1);

        // Assert
        // Total Weight (Itog) = 5
        // Total Packs (ItogFass) = 5 / 1 = 5
        // Total Price = 5 packs * 50 = 250

        var reportItem = result.First();
        Assert.Equal(5, reportItem.Itog);
        Assert.Equal(5, reportItem.ItogFass);
        Assert.Equal(250, reportItem.TotalPrice);
    }

    [Fact]
    public void CalculateSummary_ShouldHandle_HideInProductReport_Flag()
    {
        var prodId = _prodRepo.AddProduct("Hidden Prod", 1, 1, 1, 1);

        var item1 = new MenuDel_act
        {
            Del_id = 1,
            HideInProductReport = false,
            Lcomp = new List<Components> { new() { Prodid = prodId, Ves = 10 } }
        };

        var item2 = new MenuDel_act
        {
            Del_id = 2,
            HideInProductReport = true, // Should be excluded
            Lcomp = new List<Components> { new() { Prodid = prodId, Ves = 10 } }
        };

        var result = _service.CalculateSummary(new[] { item1, item2 }, 1);

        Assert.Single(result); // Only item1 should be there
        Assert.Equal(1, result.First().Del_id);
    }
}