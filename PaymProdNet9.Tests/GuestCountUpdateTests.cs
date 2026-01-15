using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace PaymProdNet9.Tests
{
    [Collection("Database Tests")]
    public class GuestCountUpdateTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _dbPath;
        private readonly MenuRepository _menuRepo;
        private readonly ProductRepository _prodRepo;
        private readonly DelicateRepository _delRepo; // Need to create helper or use existing if publicly available

        public GuestCountUpdateTests(ITestOutputHelper output)
        {
            _output = output;
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_GuestCount_{Guid.NewGuid()}.db");
            DatabaseHelper.InitializeDatabase(_dbPath);

            _menuRepo = new MenuRepository(_dbPath);
            _prodRepo = new ProductRepository(_dbPath);
            _delRepo = new DelicateRepository(_dbPath);
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { }
            }
        }

        [Fact]
        public void Should_Update_PortionCount_Only_If_Matches_OldGuestCount_And_Not_TotalQuantity()
        {
            // 1. Setup Data
            // Create Measures
            int kgId = _prodRepo.AddMeasure("кг", 1000, "кг");
            int pcsId = _prodRepo.AddMeasure("шт", 1, "шт");
            int typeId = _prodRepo.AddProductType("General", 1);

            // Create Products
            // Product 1: Normal (Updates with guests)
            int prodNormalId = _prodRepo.AddProduct("Normal Product", kgId, typeId, 1, kgId);
            
            // Product 2: Total Quantity (Isdiap = 1) - Should NOT update
            // Note: AddProduct doesn't expose Isdiap, need to update it manually or use a method if available.
            // Let's use SQL to set Isdiap for now, or check if Repository has a method.
            // Assuming we can update it via DB execution.
            SetProductIsDiap(prodNormalId, false); // Explicitly ensure false

            int prodFixedId = _prodRepo.AddProduct("Fixed Product", pcsId, typeId, 1, pcsId);
            SetProductIsDiap(prodFixedId, true);

            // Create Dish
            // Create Dish
            int typeDelId = _delRepo.AddDelicateType("Type");
            int dishId = _delRepo.AddDelicate(typeDelId, "Standard Dish", 1, 1, false);
            // Add component to dish? Not strictly necessary for this test if we just check Menu_Delicates count.
            
            // 2. Create Menu (10 guests)
            int initialGuests = 10;
            int menuId = _menuRepo.CreateMenu("Test Banquet", initialGuests, "Details", "2025-01-01");

            // 3. Add Items to Menu
            // Item A: Matches Guest Count (10) -> Should Update to 15
            _menuRepo.AddDelicateToMenu(menuId, dishId, initialGuests);

            // Item B: Manual Count (5) -> Should Stay 5
            int itemB_Id = _menuRepo.AddDelicateToMenu(menuId, dishId, 5); // Using same dish, different count

            // Item C: Product Normal (10) -> Should Update to 15
            // Products are added with negative ID
            _menuRepo.AddDelicateToMenu(menuId, -prodNormalId, initialGuests);

            // Item D: Product Fixed (10) -> Should Stay 10 (Isdiap = 1)
            _menuRepo.AddDelicateToMenu(menuId, -prodFixedId, initialGuests);

            // 4. Update Menu to 15 guests
            // 4. Update Menu to 15 guests
            int newGuests = 15;
            _menuRepo.UpdateMenu(menuId, "Test Banquet Updated", newGuests, "Details", "2025-01-01", recalculatePortions: true);

            // 5. Verify
            var items = _menuRepo.GetMenuDelicates(menuId);

            foreach (var item in items)
            {
                if (item.Del_id == dishId && item.Countpor == newGuests)
                {
                    // Item A: Standard Dish updated
                    Assert.Equal(newGuests, item.Countpor); 
                }
                else if (item.Del_id == dishId && item.Countpor == 5)
                {
                    // Item B: Manual count preserved
                     Assert.Equal(5, item.Countpor);
                }
                else if (item.Del_id == -prodNormalId)
                {
                    // Item C: Normal product updated
                    Assert.Equal(newGuests, item.Countpor);
                }
                else if (item.Del_id == -prodFixedId)
                {
                    // Item D: Fixed product preserved
                    Assert.Equal(initialGuests, item.Countpor);
                }
            }
            
            // Assert counts explicitly
            Assert.Contains(items, i => i.Del_id == dishId && i.Countpor == newGuests);
            Assert.Contains(items, i => i.Del_id == dishId && i.Countpor == 5);
            Assert.Contains(items, i => i.Del_id == -prodNormalId && i.Countpor == newGuests);
            Assert.Contains(items, i => i.Del_id == -prodFixedId && i.Countpor == initialGuests);
        }

        private void SetProductIsDiap(int prodId, bool isDiap)
        {
             using var connection = DatabaseHelper.GetConnection(_dbPath);
             connection.Open();
             var command = connection.CreateCommand();
             command.CommandText = "UPDATE Producrs SET Isdiap = @isDiap WHERE Prod_ID = @id";
             command.Parameters.AddWithValue("@isDiap", isDiap ? 1 : 0);
             command.Parameters.AddWithValue("@id", prodId);
             command.ExecuteNonQuery();
        }
    }
}
