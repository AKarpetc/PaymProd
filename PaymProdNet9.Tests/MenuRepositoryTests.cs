using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class MenuRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly MenuRepository _repository;
    private readonly ProductRepository _prodRepo;
    private readonly DelicateRepository _dishRepo;

    public MenuRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Menu_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _repository = new MenuRepository();
        _prodRepo = new ProductRepository();
        _dishRepo = new DelicateRepository();
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
    public void CreateMenu_ShouldSaveFieldsAndAutoAdd_ProductsAndDishes()
    {
        // Arrange
        // 1. Create a product with AutoAdd
        var prodId = _prodRepo.AddProduct("Auto Prod", 1, 1, 1, 1, automat: true, count: 5);
        // 2. Create a dish with AutoAdd
        var typeId = _dishRepo.AddDelicateType("Type");
        var dishId = _dishRepo.AddDelicate(typeId, "Auto Dish", 100, 1, true);

        // Act
        var menuId = _repository.CreateMenu("Test Menu", 10, "Details", "01.01.2025");

        // Assert
        var menu = _repository.GetMenuById(menuId);
        Assert.NotNull(menu);
        Assert.Equal("Test Menu", menu.Name);
        Assert.Equal(10, menu.CountP);

        // Verify Auto Added items
        var menuItems = _repository.GetMenuDelicates(menuId);

        // Check for Auto Prod (Products have negative ID in MenuDelicates usually if added directly? 
        // Wait, AddDelicateToMenu logic for products: "If delicateId negative...". 
        // MenuRepository.CreateMenu logic: "SELECT Prod_ID... FROM Producrs WHERE Avtomat=1" -> AddAutoProductToMenu -> AddDelicateToMenu(menuId, linkedDelicateId, ...) OR AddProductDirectlyToMenu
        // Since "Auto Prod" is not linked to any dish, it should be added as a product directly (Id_delic = -prodId)

        var prodItem = menuItems.FirstOrDefault(x => x.Del_id == -prodId);
        Assert.NotNull(prodItem);
        // Count logic for auto product: 
        // IsDiap=0 (default) -> totalCount = countPeople (10)
        // But wait, my AddProduct call had `count: 5`. 
        // `AutoAddProductToMenu`: `totalCount = baseCount` IF `isdiap` else `countPeople`.
        // Let's verify IsDiap in AddProduct default is false. 
        // My default `mainCount` in AddProduct is false.
        // So expected count is 10 (Guests)
        Assert.Equal(10, prodItem.Countpor);

        // Check for Auto Dish
        var dishItem = menuItems.FirstOrDefault(x => x.Del_id == dishId);
        Assert.NotNull(dishItem);
        Assert.Equal(10, dishItem.Countpor); // Dishes added with count = guests
    }

    [Fact]
    public void AddDelicateToMenu_ShouldAdd_And_RemoveDelicateFromMenu_ShouldRemove()
    {
        // Arrange
        var menuId = _repository.CreateMenu("Menu", 10, "", "");
        var typeId = _dishRepo.AddDelicateType("Type");
        var dishId = _dishRepo.AddDelicate(typeId, "Dish", 100, 1, false);

        // Act: Add
        _repository.AddDelicateToMenu(menuId, dishId, 5);

        // Assert: Add
        var items = _repository.GetMenuDelicates(menuId);
        var item = items.FirstOrDefault(x => x.Del_id == dishId);
        Assert.NotNull(item);
        Assert.Equal(5, item.Countpor);

        // Act: Remove
        // We need the ID of the Menu_Delicates record, not the Dish ID
        _repository.RemoveDelicateFromMenu(item.Id);

        // Assert: Remove
        items = _repository.GetMenuDelicates(menuId);
        Assert.DoesNotContain(items, x => x.Del_id == dishId);
    }

    [Fact]
    public void UpdateMenu_ShouldUpdateGuestCount_ButNotRecalculateOldItems()
    {
        // Arrange
        var guests = 10;
        var menuId = _repository.CreateMenu("Menu", guests, "", "");
        var typeId = _dishRepo.AddDelicateType("Type");
        var dishId = _dishRepo.AddDelicate(typeId, "Dish", 100, 1, false);

        // Add dish with count = 10
        _repository.AddDelicateToMenu(menuId, dishId, 10);

        // Act: Change guests to 20
        _repository.UpdateMenu(menuId, "Updated Menu", 20, "New Details", "02.01.2025");

        // Assert
        var menu = _repository.GetMenuById(menuId);
        Assert.NotNull(menu);
        Assert.Equal(20, menu.CountP);
        Assert.Equal("Updated Menu", menu.Name);

        // Verify Dish Count did NOT change (automatic recalculation removed)
        var items = _repository.GetMenuDelicates(menuId);
        var item = items.First(x => x.Del_id == dishId);
        Assert.Equal(10, item.Countpor); // Should remain 10, not 20
    }

    [Fact]
    public void AddProductDirectlyToMenu_ShouldWork()
    {
        // Test adding a raw product to menu (simulating search & add product)
        var menuId = _repository.CreateMenu("Menu", 10, "", "");
        var prodId = _prodRepo.AddProduct("Raw Product", 1, 1, 1, 1);

        // Add using negative ID convention
        _repository.AddDelicateToMenu(menuId, -prodId, 5);

        var items = _repository.GetMenuDelicates(menuId);
        var item = items.FirstOrDefault(x => x.Del_id == -prodId);

        Assert.NotNull(item);
        Assert.Equal(5, item.Countpor);
        Assert.Equal("Raw Product", item.Del); // Name comes from product
    }

    [Fact]
    public void RemoveDelicateFromMenu_ShouldCleanUpComponents1()
    {
        // Arrange
        var menuId = _repository.CreateMenu("Test Menu", 10, "Details", "2024-01-01");
        // Add product with Isdiap=1 (mainCount=true) effectively simulating AutoAdd behavior or manual add of such product
        var prodId = _prodRepo.AddProduct("Auto Prod", 1, 1, 1, 1, 1, 0, true, mainCount: true);

        // Trigger auto-add logic explicitly
        _repository.EnsureAutoAddProductsInMenu(menuId, 10);

        // Verify it exists in Menu_Delicates and Components1
        var menuDelicates = _repository.GetMenuDelicates(menuId);
        Assert.Single(menuDelicates);
        var delId = menuDelicates[0].Del_id;
        Assert.Equal(-prodId, delId);

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Components1 WHERE Idmen = @mid AND Delic_id = @did";
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@did", delId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.True(count > 0, "Should have Components1 entry for Isdiap product");
        }

        // Act
        _repository.RemoveDelicateFromMenu(menuDelicates[0].Id);

        // Assert
        menuDelicates = _repository.GetMenuDelicates(menuId);
        Assert.Empty(menuDelicates);

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Components1 WHERE Idmen = @mid AND Delic_id = @did";
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@did", delId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(0, count); // Should be 0
        }
    }

    [Fact]
    public void ManualRemoval_ShouldPreventAutoReAdd()
    {
        // Arrange
        var menuId = _repository.CreateMenu("ReAdd Test", 10, "", "");
        // Create product that auto-adds
        var prodId = _prodRepo.AddProduct("AutoAdd Product", 1, 1, 1.0, 1, automat: true, count: 0m);

        // 1. Initial Auto Add
        _repository.EnsureAutoAddProductsInMenu(menuId, 10);
        var items = _repository.GetMenuDelicates(menuId);
        var item = items.FirstOrDefault(x => x.Del_id == -prodId);
        Assert.NotNull(item); // Should be present

        // 2. Manual Removal (simulating UI deletion)
        // UI calls RegisterAutoProductManualRemoval THEN RemoveDelicateFromMenu
        _repository.RegisterAutoProductManualRemoval(menuId, item.Del_id);
        _repository.RemoveDelicateFromMenu(item.Id);

        // Verify it's gone
        items = _repository.GetMenuDelicates(menuId);
        Assert.DoesNotContain(items, x => x.Del_id == -prodId);

        // 3. Trigger Auto Add again (e.g. refreshing menu or re-opening)
        _repository.EnsureAutoAddProductsInMenu(menuId, 10);

        // Assert: Should NOT reappear
        items = _repository.GetMenuDelicates(menuId);
        Assert.DoesNotContain(items, x => x.Del_id == -prodId);

        // Verify ignore record exists
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Menu_AutoProduct_Ignore WHERE Id_men = @mid AND ProductID = @pid";
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@pid", prodId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public void EnsureAutoAdd_ShouldExcludeDeletedProducts()
    {
        // Arrange
        var menuId = _repository.CreateMenu("Deleted AutoAdd Test", 10, "", "");

        // 1. Create product with AutoAdd = TRUE
        var prodId = _prodRepo.AddProduct("Deleted Auto Product", 1, 1, 1.0, 1, automat: true);

        // 2. Soft DELETE the product (IsDeleted = 1), but keep Avtomat = 1
        _prodRepo.DeleteProduct(prodId);

        // Verify it is deleted
        var p = _prodRepo.GetAllProducts().FirstOrDefault(x => x.ID == prodId);
        Assert.True(p.IsDeleted);
        Assert.True(p.AutoAdd); // Flag might still be true in DB

        // 3. Run EnsureAutoAdd
        _repository.EnsureAutoAddProductsInMenu(menuId, 10);

        // Assert: Should NOT be in menu
        var items = _repository.GetMenuDelicates(menuId);
        Assert.DoesNotContain(items, x => x.Del_id == -prodId);
    }
}