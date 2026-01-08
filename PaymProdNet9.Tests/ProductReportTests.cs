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
        var measureKg = _prodRepo.AddMeasure("кг", 1000, "кг"); // Pack unit: 1 kg = 1000 base units
        var typeId = _prodRepo.AddProductType("Spices");

        // Product: Corn Starch.
        // Base Unit (Ves): Gram (ID=measureGram).
        // Pack Unit (Izmer): Kg (ID=measureKg).
        // Fass (Packaging Size): 1000 (from DB, or default).
        // Price: 1000 (per pack/kg).
        
        // AddProduct(name, vesId, typeId, fass (double), izmerId, ..., price (double), ...)
        // Note: AddProduct internally sets 'Ves' to 'izmerId' if 'vesId' is null. We want distinct base and pack units.
        // So we might need to update it after adding, or explicitly pass vesId if allowed (AddProduct logic dependent).
        
        // Let's create with defaults first.
        var pId = _prodRepo.AddProduct("Starch", null, typeId, 1000.0, measureKg, price: 1000.0, hideInMenu: false);
        
        // Now explicit Update to set exact state we want to test:
        // Fass = 0 (to test failure case logic - wait, we want to test that it works even if Fass is 0 in DB because of default fallback).
        // So we set Fass = 0.
        // Izmer (Pack Unit) = measureKg (which has Fass_Def = 1000).
        // Ves (Base Unit) = measureGram (which has Fass_Def = 1).
        
        // UpdateProduct(id, name, ves, type, fass, izmer, ...)
        // Check UpdateProduct signature from previous views:
        // UpdateProduct(int id, string name, string ves, int typeId, decimal fass, string izmer, 
        //               int prizMen, decimal count, bool avtomat, int chel, bool isdiap, double price)
        // Wait, 'ves' and 'izmer' in UpdateProduct are STRINGS (names) or IDs? 
        // Let's verify signature via ViewFile to be 100% sure. 
        // Based on previous errors, it seemed to expect mismatched types.
        // I will assume standard signature based on usage:
        // _prodRepo.UpdateProduct(pId, "Starch", measureGram.ToString(), typeId, 0m, measureKg.ToString(), 0, 0m, false, 0, false, 1000.0);
        // Actually, looking at repo code would be safer.
        // But for now, I will use a known working method or fix the signature in the next step if it fails.
        // Let's try to bypass UpdateProduct if AddProduct is sufficient.
        
        // If I pass Fass=0 to AddProduct?
        // AddProduct(..., fass: 0, ...)
        // But AddProduct might not let us set Ves different from Izmer easily?
        // Let's try to use direct SQL insert if Repository is hard to use for this specific edge case setup? 
        // No, let's use Repository.
        
        _prodRepo.UpdateProduct(
            id: pId, 
            name: "Starch", 
            vesId: measureGram, 
            typeId: typeId, 
            fass: 0m, 
            izmerId: measureKg, 
            prizMenu: 0, 
            count: 0m, 
            automat: false, 
            countPeople: 0, 
            mainCount: false, 
            price: 1000.0);
        
        // Add to Menu: 500g
        _menuRepo.AddDelicateToMenu(menuId, -pId, 500);

        // Act
        var items = _menuRepo.GetMenuDelicates(menuId);
        var report = _service.CalculateSummary(items, menuId);
        var starchItem = report.First(x => x.Name == "Starch");

        // Assert
        // Weight: 500.
        // Price: 1000.
        // Even though product.Fass is 0, the SQL fix should pick up measureKg.Fass_Def (1000).
        // So PackCount should be 500 / 1000 = 0.5.
        // TotalPrice: 1000 * 0.5 = 500.
        
        Assert.Equal(500, starchItem.Itog);
        // Correct Fass should be retrieved (1000)
        Assert.Equal(1000, starchItem.Fass);
        // Correct Cost
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