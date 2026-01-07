using PaymProdNet9.Data;
using PaymProdNet9.Enums;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class MenuReportTests : IDisposable
{
    private readonly string _dbPath;
    private readonly MenuReportCalculationService _service;
    private readonly MenuRepository _menuRepo;
    private readonly ProductRepository _prodRepo;
    private readonly DelicateRepository _dishRepo;
    private readonly SettingsRepository _settings;

    public MenuReportTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_MenuReport_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _service = new MenuReportCalculationService();
        _menuRepo = new MenuRepository();
        _prodRepo = new ProductRepository();
        _dishRepo = new DelicateRepository();
        _settings = new SettingsRepository();
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
    public void CalculateReport_CostMode_ShouldSumIngredientPrices_NoMarkup_NoService()
    {
        // Arrange
        // Product Cost: 10
        var pId = _prodRepo.AddProduct("Prod", 1, 1, 1, 1, price: 10);

        // Dish Cost: 2 * 10 = 20
        var delicate =
            CreateDishWithComponent(pId, 2, 200, 1); // Markup 200% should be ignored in Cost mode

        // Act
        var result = _service.CalculateReport(new List<DelicatesColl> { delicate }, 0, ReportMode.Cost);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(20, result.Subtotal); // Raw cost
        Assert.Equal(0, result.ServiceAmount); // No service in Cost mode
        Assert.Equal(20, result.GrandTotal);
        Assert.Equal(20, result.Items[0].DishPrice);
    }

    [Fact]
    public void CalculateReport_PriceMode_ShouldApplyMarkup_AndServicePercent()
    {
        // Arrange
        // Product Cost: 10
        var pId = _prodRepo.AddProduct("Prod", 1, 1, 1, 1, price: 10);

        // Dish Raw Cost: 2 * 10 = 20
        // Markup: 200% (x2) -> Dish Price = 40
        var delicate = CreateDishWithComponent(pId, 2, 200, 1);

        // Global Service Percent: Set to 10%
        _settings.SaveSettings(new AppGlobalSettings { ServicePercent = 10 });

        // Act
        var result = _service.CalculateReport(new List<DelicatesColl> { delicate }, 0, ReportMode.Price);

        // Assert
        Assert.Equal(10, result.ServicePercent); // From Settings

        // Dish Price
        Assert.Equal(40, result.Items[0].DishPrice); // 20 * 200% = 40

        // Subtotal
        Assert.Equal(40, result.Subtotal);

        // Service: 10% of 40 = 4
        Assert.Equal(4, result.ServiceAmount);

        // Grand Total: 40 + 4 = 44
        Assert.Equal(44, result.GrandTotal);
    }

    [Fact]
    public void CalculateReport_PriceMode_ShouldUseMenuSpecificServicePercent()
    {
        // Arrange
        var pId = _prodRepo.AddProduct("Prod", 1, 1, 1, 1, price: 10);
        var delicate = CreateDishWithComponent(pId, 1, 100, 1); // Price 10 * 100% = 10

        // Create Menu with specific service 15%
        var menuId = _menuRepo.CreateMenu("Menu 15%", 10, "", "");
        // Update Service Percent manually in DB (since CreateMenu uses default)
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Menus SET ServicePercent = 15 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", menuId);
            cmd.ExecuteNonQuery();
        }

        // Act
        var result = _service.CalculateReport(new List<DelicatesColl> { delicate }, menuId, ReportMode.Price);

        // Assert
        Assert.Equal(15, result.ServicePercent);
        Assert.Equal(10, result.Subtotal);
        Assert.Equal(1.5m, result.ServiceAmount); // 15% of 10
        Assert.Equal(11.5m, result.GrandTotal);
    }

    private DelicatesColl CreateDishWithComponent(int prodId, decimal ves, decimal markup, int count)
    {
        return new DelicatesColl
        {
            Name = "Test Dish",
            DefaultMarkup = markup,
            Count = count,
            Lcomp = new List<Components>
            {
                new()
                {
                    Prodid = prodId,
                    Ves = ves,
                    Fass = 0
                }
            }
        };
    }
}