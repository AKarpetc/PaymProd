using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests
{
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
            try { 
                GC.Collect(); GC.WaitForPendingFinalizers();
                if (File.Exists(_dbPath)) File.Delete(_dbPath); 
            } catch { }
        }

        [Fact]
        public void CreateMenu_ShouldSaveFieldsAndAutoAdd_ProductsAndDishes()
        {
            // Arrange
            // 1. Create a product with AutoAdd
            int prodId = _prodRepo.AddProduct("Auto Prod", 1, 1, 1, 1, automat: true, count: 5);
            // 2. Create a dish with AutoAdd
            int typeId = _dishRepo.AddDelicateType("Type");
            int dishId = _dishRepo.AddDelicate(typeId, "Auto Dish", 100, 1, autoAdd: true);

            // Act
            int menuId = _repository.CreateMenu("Test Menu", 10, "Details", "01.01.2025");

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
            int menuId = _repository.CreateMenu("Menu", 10, "", "");
            int typeId = _dishRepo.AddDelicateType("Type");
            int dishId = _dishRepo.AddDelicate(typeId, "Dish", 100, 1, false);

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
            int guests = 10;
            int menuId = _repository.CreateMenu("Menu", guests, "", "");
            int typeId = _dishRepo.AddDelicateType("Type");
            int dishId = _dishRepo.AddDelicate(typeId, "Dish", 100, 1, false);
            
            // Add dish with count = 10
            _repository.AddDelicateToMenu(menuId, dishId, 10);

            // Act: Change guests to 20
            _repository.UpdateMenu(menuId, "Updated Menu", 20, "New Details", "02.01.2025");

            // Assert
            var menu = _repository.GetMenuById(menuId);
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
             int menuId = _repository.CreateMenu("Menu", 10, "", "");
             int prodId = _prodRepo.AddProduct("Raw Product", 1, 1, 1, 1);

             // Add using negative ID convention
             _repository.AddDelicateToMenu(menuId, -prodId, 5);

             var items = _repository.GetMenuDelicates(menuId);
             var item = items.FirstOrDefault(x => x.Del_id == -prodId);
             
             Assert.NotNull(item);
             Assert.Equal(5, item.Countpor);
             Assert.Equal("Raw Product", item.Del); // Name comes from product
        }
    }
}
