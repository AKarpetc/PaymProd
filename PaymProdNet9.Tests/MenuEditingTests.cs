using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests
{
    [Collection("Database Tests")]
    public class MenuEditingTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly MenuRepository _menuRepo;
        private readonly DelicateRepository _dishRepo;
        private readonly ProductRepository _prodRepo;

        public MenuEditingTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Edit_{Guid.NewGuid()}.db");
            DatabaseHelper.InitializeDatabase(_dbPath);
            _menuRepo = new MenuRepository();
            _dishRepo = new DelicateRepository();
            _prodRepo = new ProductRepository();
        }

        public void Dispose()
        {
            try { 
                GC.Collect(); GC.WaitForPendingFinalizers();
                if (File.Exists(_dbPath)) File.Delete(_dbPath); 
            } catch { }
        }

        [Fact]
        public void EditDishQuantity_ShouldUpdateMenuRecord_ButNotDirectory()
        {
            // Arrange
            int menuId = _menuRepo.CreateMenu("Test Menu", 10, "", "");
            int typeId = _dishRepo.AddDelicateType("Type");
            int dishId = _dishRepo.AddDelicate(typeId, "Original Dish", 100, 1, false);
            
            // Add to menu with 10 portions
            _menuRepo.AddDelicateToMenu(menuId, dishId, 10);

            // Fetch the item to edit
            var menuItems = _menuRepo.GetMenuDelicates(menuId);
            var itemToEdit = menuItems.First(x => x.Del_id == dishId);
            
            // Act: Change quantity to 20
            itemToEdit.Countpor = 20; // Assuming this is int in Model, but Repo supports decimal logic internally? 
            // Wait, MenuDel_act.Countpor is int in the model I saw in MenuRepository.GetMenuDelicates (reader.GetInt32(3)).
            // Let's verify Model. 
            // In MenuRepository.cs line 437: Countpor = reader.GetInt32(3)
            // But verify if SaveMenuChanges uses it.
            // SaveMenuChanges: command.Parameters.AddWithValue("@count", menuDel.Countpor);
            
            _menuRepo.SaveMenuChanges(menuId, menuItems);

            // Assert
            // 1. Verify Menu has 20
            var updatedItems = _menuRepo.GetMenuDelicates(menuId);
            var updatedItem = updatedItems.First(x => x.Del_id == dishId);
            Assert.Equal(20, updatedItem.Countpor);

            // 2. Verify Directory still has original default count (1)
            var directoryDish = _dishRepo.GetDelicateById(dishId);
            Assert.NotNull(directoryDish);
            Assert.Equal(1, directoryDish.Count);
        }

        [Fact]
        public void EditDishComposition_ShouldSaveToComponents1_AndNotAllocDirectory()
        {
            // Arrange
            int menuId = _menuRepo.CreateMenu("Comp Test", 10, "", "");
            int typeId = _dishRepo.AddDelicateType("Type");
            int dishId = _dishRepo.AddDelicate(typeId, "Dish With Comps", 100, 1, false);

            int p1 = _prodRepo.AddProduct("Prod1", 1, 1, 1, 1);
            int p2 = _prodRepo.AddProduct("Prod2", 1, 1, 1, 1);
            
            _dishRepo.AddComponent(dishId, p1, 100, null); // 100g of Prod1
            // Dish has Prod1 (100g)

            _menuRepo.AddDelicateToMenu(menuId, dishId, 10);

            // Fetch
            var menuItems = _menuRepo.GetMenuDelicates(menuId);
            var item = menuItems.First(x => x.Del_id == dishId); // Has Lcomp with Prod1

            // Act: Modify Composition locally
            // 1. Update Prod1 weight to 150
            item.Lcomp.First(c => c.Prodid == p1).Ves = 150;
            // 2. Add Prod2 (50g)
            item.Lcomp.Add(new Components { Prodid = p2, Ves = 50, NameT = "Prod2" });

            _menuRepo.SaveMenuChanges(menuId, menuItems);

            // Assert
            // 1. Verify Menu Item has new composition
            var updatedItems = _menuRepo.GetMenuDelicates(menuId);
            var updatedItem = updatedItems.First(x => x.Del_id == dishId);
            
            Assert.True(updatedItem.IsModified);
            Assert.Equal(2, updatedItem.Lcomp.Count);
            
            var c1 = updatedItem.Lcomp.First(c => c.Prodid == p1);
            Assert.Equal(150, c1.Ves);
            
            var c2 = updatedItem.Lcomp.First(c => c.Prodid == p2);
            Assert.Equal(50, c2.Ves);

            // 2. Verify Directory Dish is UNCHANGED
            var dirDish = _dishRepo.GetDelicateById(dishId); // Uses GetDelicateComponents -> Components table
            Assert.NotNull(dirDish);
            Assert.Single(dirDish.Lcomp);
            Assert.Equal(100, dirDish.Lcomp.First().Ves); // Original weight
        }
    }
}
