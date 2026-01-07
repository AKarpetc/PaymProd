using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.IO;
using Xunit;

namespace PaymProdNet9.Tests.Services;

[Collection("Database Tests")]
public class MenuPriceServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly MenuPriceService _service;
    private readonly ProductRepository _prodRepo;
    private readonly MenuRepository _menuRepo;

    public MenuPriceServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Price_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _service = new MenuPriceService();
        _prodRepo = new ProductRepository();
        _menuRepo = new MenuRepository();
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
    public void GetUnitPrice_ShouldReturnBasePrice_WhenNoMenuPrice()
    {
        // Arrange
        var prodId = _prodRepo.AddProduct("Base Prod", 1, 1, 1, 1, price: 100);

        // Act
        var price = _service.GetUnitPrice(0, prodId); // menuId 0 or non-existent

        // Assert
        Assert.Equal(100, price);
    }

    [Fact]
    public void GetUnitPrice_ShouldReturnMenuPrice_WhenExists()
    {
        // Arrange
        var prodId = _prodRepo.AddProduct("Menu Prod", 1, 1, 1, 1, price: 100);
        var menuId = _menuRepo.CreateMenu("Menu", 10, "", "");

        // Set Menu Price (need to use SQL directly as Repository might not expose Price update easily for products? 
        // Actually ProductPricesPage uses ProductRepository.UpdateProductPriceInMenu(int menuId, int productId, double price))
        // Let's see if ProductRepository has that. 
        // Checking: ProductRepository.cs
        // I'll assume it exists or use SQL. Safest is SQL for test setup if I didn't verify repo method.
        // But let's look for UpdateProductPriceInMenu... 
        // Wait, I can just use SqliteCommand to insert into Price_izmn_m

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Menu_Product_Prices (ProductID, Id_men, Price) VALUES (@pid, @mid, @price)";
            cmd.Parameters.AddWithValue("@pid", prodId);
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@price", 150); // Override 100 -> 150
            cmd.ExecuteNonQuery();
        }

        // Act
        var price = _service.GetUnitPrice(menuId, prodId);

        // Assert
        Assert.Equal(150, price);
    }

    [Fact]
    public void GetComponentPriceInfo_ShouldCalculateCorrectly()
    {
        // Arrange
        var prodId = _prodRepo.AddProduct("Comp Prod", 1, 1, 1, 1, price: 10);
        var component = new Components
        {
            Prodid = prodId,
            Ves = 2, // 2 units/grams per portion
            Fass = 0 // No packaging divisor
        };

        // Act: 10 portions
        var info = _service.GetComponentPriceInfo(0, component, 10);

        // Assert
        // Units: 2 * 10 = 20
        // UnitPrice: 10
        // Total: 20 * 10 = 200
        Assert.Equal(20, info.Units);
        Assert.Equal(10, info.UnitPrice);
        Assert.Equal(200, info.TotalPrice);
    }

    [Fact]
    public void GetComponentPriceInfo_ShouldHandlePackaging()
    {
        // Arrange
        var prodId = _prodRepo.AddProduct("Pack Prod", 1, 1, 1, 1, price: 100); // 100 per pack
        var component = new Components
        {
            Prodid = prodId,
            Ves = 500, // 500g total needed
            Fass = 1000 // 1000g per pack
        };

        // Act: 1 portion (or total logic)
        var info = _service.GetComponentPriceInfo(0, component, 1);

        // Assert
        // Required Units: 500 / 1000 = 0.5 packs
        // Unit Price: 100 per pack
        // Total: 0.5 * 100 = 50
        Assert.Equal(0.5m, info.Units);
        Assert.Equal(100, info.UnitPrice);
        Assert.Equal(50, info.TotalPrice);
    }
}