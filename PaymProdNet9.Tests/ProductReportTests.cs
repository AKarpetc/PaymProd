using System;
using System.IO;
using System.Linq;
using Xunit;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;

namespace PaymProdNet9.Tests;

public class ProductReportTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ProductReportCalculationService _service;
    private readonly MenuRepository _menuRepo;
    private readonly ProductRepository _prodRepo;

    public ProductReportTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_ProdReport_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        
        _service = new ProductReportCalculationService();
        _menuRepo = new MenuRepository();
        _prodRepo = new ProductRepository();
    }

    public void Dispose()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch { }
    }

    [Fact]
    public void PackagingCost_ShouldBeCalculatedCorretly()
    {
        // Arrange
        var menuId = _menuRepo.CreateMenu("Test Menu", 10, "Desc", DateTime.Now.ToString());
        var measureGram = _prodRepo.AddMeasure("грамм", 1, "грамм");
        var measureKg = _prodRepo.AddMeasure("кг", 1, "кг"); // Pack unit
        var typeId = _prodRepo.AddProductType("Spices");

        // Product: Starch. Base: gram. Pack: kg. Fass: 1000. Price: 1000 (per pack).
        // AddProduct(name, vesId, typeId, fass (double), izmerId, ..., price (double), ...)
        var pId = _prodRepo.AddProduct("Starch", null, typeId, 1000.0, measureKg, price: 1000.0, hideInMenu: false);
        
        // UpdateProduct(id, name, vesId, typeId, fass (decimal), izmerId, ..., price (double))
        _prodRepo.UpdateProduct(pId, "Starch", measureGram, typeId, 1000m, measureKg, 0, 0m, false, 0, false, 1000.0);

        // Add to Menu: 500g
        _menuRepo.AddDelicateToMenu(menuId, -pId, 500);

        // Act
        var items = _menuRepo.GetMenuDelicates(menuId);
        var report = _service.CalculateSummary(items, menuId);
        var starchItem = report.First(x => x.Name == "Starch");

        // Assert
        // Weight: 500.
        // Price: 1000.
        // Fass: 1000.
        // PackCount: 500 / 1000 = 0.5.
        // TotalPrice: 1000 * 0.5 = 500.
        
        Assert.Equal(500, starchItem.Itog);
        Assert.Equal(1000, starchItem.Fass);
        Assert.Equal(500, starchItem.TotalPrice);
    }

    [Fact]
    public void ProductReport_WhenFiltered_ShouldMatchVisibleMenuCost()
    {
        // Arrange
        var menuId = _menuRepo.CreateMenu("Test Menu", 10, "Desc", DateTime.Now.ToString());

        // Create references (Measure, Type)
        var measureId = _prodRepo.AddMeasure("kg", 1, "kg");
        var typeId = _prodRepo.AddProductType("Type1");

        // 1. Add a visible product (Price 100)
        var p1 = _prodRepo.AddProduct("Visible Prod", null, typeId, 1, measureId, price: 100, hideInMenu: false);
        _menuRepo.AddDelicateToMenu(menuId, -p1, 1);

        // 2. Add a hidden product (Price 50)
        var p2 = _prodRepo.AddProduct("Hidden Prod", null, typeId, 1, measureId, price: 50, hideInMenu: true);
        _menuRepo.AddDelicateToMenu(menuId, -p2, 1);

        // Get items (MenuRepository returns all, sorted)
        var items = _menuRepo.GetMenuDelicates(menuId);
        
        // Assert we have 2 items
        Assert.Equal(2, items.Count);

        // Logic check mimicking UI
        
        // Case 1: Filter Enabled (Only Visible)
        // Note: MenuRepository.GetMenuDelicates sets HideInMenu property on items logic
        // We need to ensure we filter by d.HideInMenu
        
        var visibleItems = items.Where(d => !d.HideInMenu);
        var reportVisible = _service.CalculateSummary(visibleItems, menuId);
        var sumVisible = reportVisible.Sum(x => x.TotalPrice);

        // Assert: Visible Sum = 100 * 1 = 100
        Assert.Equal(100, sumVisible);

        // Case 2: Filter Disabled (All Items)
        var allItems = items; // No filter
        var reportAll = _service.CalculateSummary(allItems, menuId);
        var sumAll = reportAll.Sum(x => x.TotalPrice);

        // Assert: Total Sum = 100 + 50 = 150
        Assert.Equal(150, sumAll);

        // Verify Difference
        Assert.NotEqual(sumVisible, sumAll);
    }
    [Fact]
    public void Debug_PrintDbValues()
    {
        // This test connects to the ACTUAL user database if possible, or we assume logic.
        // Wait, unit tests use a temp DB. I can't check user's DB.
        // I need to create a script to run against the ACTUAL DB path.
        // But I don't know the actual DB path. It's in the connection string.
        // Data/DatabaseHelper.cs has the connection string.
        
        // I will write a small Console App "DebugApp.cs" in the main project folder
        // and run it via "dotnet run".
    }
}