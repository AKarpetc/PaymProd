using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class ProductRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ProductRepository _repository;

    // Flags: 
    // 1. PrizMen (AddToDish)
    // 2. AutoAdd (Automat)
    // 3. MainCount (IsDiap)
    // 4. HideInMenu
    // 5. DoNotConvertToPackInMenu

    public ProductRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Prod_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _repository = new ProductRepository();
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

    public static IEnumerable<object[]> GetAllFlagCombinations()
    {
        // Generate all 32 combinations of 5 booleans
        for (var i = 0; i < 32; i++)
        {
            var prizMen = (i & 1) != 0;
            var autoAdd = (i & 2) != 0;
            var mainCount = (i & 4) != 0;
            var hideInMenu = (i & 8) != 0;
            var doNotConvert = (i & 16) != 0;

            yield return new object[] { prizMen, autoAdd, mainCount, hideInMenu, doNotConvert };
        }
    }

    [Theory]
    [MemberData(nameof(GetAllFlagCombinations))]
    public void AddProduct_ShouldSaveAllFlagCombinations(
        bool addToDish, bool autoAdd, bool mainCount, bool hideInMenu, bool doNotConvert)
    {
        // Arrange
        var name = $"Prod_{addToDish}_{autoAdd}_{mainCount}_{hideInMenu}_{doNotConvert}";
        var prizMenuInt = addToDish ? 1 : 0;

        // Act
        var id = _repository.AddProduct(
            name,
            1, 1, 1, 1,
            prizMenuInt,
            10,
            autoAdd,
            0,
            mainCount,
            100,
            hideInMenu,
            doNotConvert
        );

        // Assert
        var product = _repository.GetAllProducts().First(p => p.ID == id);

        Assert.Equal(addToDish, product.PrizMen == 1); // Check int mapping
        Assert.Equal(addToDish, product.PrizMen1); // Check bool property
        Assert.Equal(autoAdd, product.AutoAdd);
        Assert.Equal(mainCount, product.MainCount);
        Assert.Equal(hideInMenu, product.HideInMenu);
        Assert.Equal(doNotConvert, product.DoNotConvertToPackInMenu);

        // Bonus: Verify side effect of "Add To Dish" (PrizMen)
        if (addToDish)
        {
            // Verify a linked delicate was created
            // We need to access Delicates table or use a delicate repository.
            // Assuming DatabaseHelper initialized tables correctly.
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Delicates WHERE LinkedProductId = @pid";
            cmd.Parameters.AddWithValue("@pid", id);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(1, count);
        }
    }

    [Theory]
    [MemberData(nameof(GetAllFlagCombinations))]
    public void UpdateProduct_ShouldUpdateAllFlagCombinations(
        bool addToDish, bool autoAdd, bool mainCount, bool hideInMenu, bool doNotConvert)
    {
        // Arrange - start with everything FALSE
        var id = _repository.AddProduct("ToUpdate", 1, 1, 1, 1,
            0, 0, false, 0,
            false, 0, false, false);

        var prizMenuInt = addToDish ? 1 : 0;

        // Act
        _repository.UpdateProduct(
            id,
            "Updated",
            1, 1, 1, 1,
            prizMenuInt,
            10,
            autoAdd,
            0,
            mainCount,
            100,
            hideInMenu,
            doNotConvert
        );

        // Assert
        var product = _repository.GetAllProducts().First(p => p.ID == id);

        Assert.Equal(addToDish, product.PrizMen == 1);
        Assert.Equal(addToDish, product.PrizMen1);
        Assert.Equal(autoAdd, product.AutoAdd);
        Assert.Equal(mainCount, product.MainCount);
        Assert.Equal(hideInMenu, product.HideInMenu);
        Assert.Equal(doNotConvert, product.DoNotConvertToPackInMenu);

        // Verify side effect of "Add To Dish" (PrizMen) on update
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Delicates WHERE LinkedProductId = @pid";
        cmd.Parameters.AddWithValue("@pid", id);
        var count = Convert.ToInt32(cmd.ExecuteScalar());

        if (addToDish)
            Assert.Equal(1, count);
        else
            Assert.Equal(0, count); // Should be removed if flag is unset
    }

    [Fact]
    public void UpdateProduct_ToggleFlags_ShouldWorkAndPersist()
    {
        // Specialized test for toggling specific flags back and forth
        // Arrange
        var id = _repository.AddProduct("Toggler", 1, 1, 1, 1);

        // 1. Toggle DoNotConvertToPackInMenu ON
        _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, false, true);
        var p = _repository.GetAllProducts().First(x => x.ID == id);
        Assert.True(p.DoNotConvertToPackInMenu);

        // 2. Toggle DoNotConvertToPackInMenu OFF
        _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, false, false);
        p = _repository.GetAllProducts().First(x => x.ID == id);
        Assert.False(p.DoNotConvertToPackInMenu);

        // 3. Toggle HideInMenu ON
        _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, true, false);
        p = _repository.GetAllProducts().First(x => x.ID == id);
        Assert.True(p.HideInMenu);
    }

    [Fact]
    public void DeleteProduct_ShouldSetIsDeleted_And_RestoreProduct_ShouldUnset()
    {
        var id = _repository.AddProduct("SoftDelete", 1, 1, 1, 1);

        _repository.DeleteProduct(id);
        var p = _repository.GetAllProducts().First(x => x.ID == id);
        Assert.True(p.IsDeleted);

        _repository.RestoreProduct(id);
        p = _repository.GetAllProducts().First(x => x.ID == id);
        _repository.RestoreProduct(id);
        p = _repository.GetAllProducts().First(x => x.ID == id);
        Assert.False(p.IsDeleted);
    }

    [Fact]
    public void UpdateProduct_ShouldRemoveFromOpenMenus_WhenAutoAddIsDisabled()
    {
        // Arrange
        var menuRepo = new MenuRepository();
        var prodId = _repository.AddProduct("Auto Prod", 1, 1, 1.0, 1, automat: true);

        // Create Open Menu
        var menuId = menuRepo.CreateMenu("Open Menu", 10, "", "");

        // Add product to menu (simulate auto-add or manual add being present)
        menuRepo.AddDelicateToMenu(menuId, -prodId, 5);

        // Also manually add to Components1 to strictly verify cleanup of "zombie" data
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            // Disable FKs to allow inserting negative Delic_id (Product)
            var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            pragma.ExecuteNonQuery();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Components1 (Idmen, Delic_id, ProductID, Ves) VALUES (@mid, @did, @pid, 1)";
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@did", -prodId);
            cmd.Parameters.AddWithValue("@pid", prodId);
            cmd.ExecuteNonQuery();
        }

        // Verify existence
        var items = menuRepo.GetMenuDelicates(menuId);
        Assert.Contains(items, x => x.Del_id == -prodId);

        // Act: Update product to disable AutoAdd
        _repository.UpdateProduct(
            prodId,
            "Auto Prod",
            1,
            1,
            1m,
            1,
            0,
            0m,
            false,
            0,
            false);

        // Assert
        items = menuRepo.GetMenuDelicates(menuId);
        Assert.DoesNotContain(items, x => x.Del_id == -prodId);

        // Verify Components1 cleanup
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Components1 WHERE Idmen = @mid AND Delic_id = @did";
            cmd.Parameters.AddWithValue("@mid", menuId);
            cmd.Parameters.AddWithValue("@did", -prodId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(0, count);
        }
    }
}