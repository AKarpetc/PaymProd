using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests
{
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
            catch {}
        }

        public static IEnumerable<object[]> GetAllFlagCombinations()
        {
            // Generate all 32 combinations of 5 booleans
            for (int i = 0; i < 32; i++)
            {
                bool prizMen = (i & 1) != 0;
                bool autoAdd = (i & 2) != 0;
                bool mainCount = (i & 4) != 0;
                bool hideInMenu = (i & 8) != 0;
                bool doNotConvert = (i & 16) != 0;

                yield return new object[] { prizMen, autoAdd, mainCount, hideInMenu, doNotConvert };
            }
        }

        [Theory]
        [MemberData(nameof(GetAllFlagCombinations))]
        public void AddProduct_ShouldSaveAllFlagCombinations(
            bool addToDish, bool autoAdd, bool mainCount, bool hideInMenu, bool doNotConvert)
        {
            // Arrange
            string name = $"Prod_{addToDish}_{autoAdd}_{mainCount}_{hideInMenu}_{doNotConvert}";
            int prizMenuInt = addToDish ? 1 : 0;

            // Act
            int id = _repository.AddProduct(
                name: name,
                vesId: 1, typeId: 1, fass: 1, izmerId: 1,
                prizMenu: prizMenuInt,
                count: 10,
                automat: autoAdd,
                countPeople: 0,
                mainCount: mainCount,
                price: 100,
                hideInMenu: hideInMenu,
                doNotConvertToPackInMenu: doNotConvert
            );

            // Assert
            var product = _repository.GetAllProducts().First(p => p.ID == id);

            Assert.Equal(addToDish, product.PrizMen == 1); // Check int mapping
            Assert.Equal(addToDish, product.PrizMen1);     // Check bool property
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
            int id = _repository.AddProduct("ToUpdate", 1, 1, 1, 1, 
                prizMenu: 0, count: 0, automat: false, countPeople: 0, 
                mainCount: false, price: 0, hideInMenu: false, doNotConvertToPackInMenu: false);

            int prizMenuInt = addToDish ? 1 : 0;

            // Act
            _repository.UpdateProduct(
                id: id,
                name: "Updated",
                vesId: 1, typeId: 1, fass: 1, izmerId: 1,
                prizMenu: prizMenuInt,
                count: 10,
                automat: autoAdd,
                countPeople: 0,
                mainCount: mainCount,
                price: 100,
                hideInMenu: hideInMenu,
                doNotConvertToPackInMenu: doNotConvert
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
             int id = _repository.AddProduct("Toggler", 1, 1, 1, 1);
             
             // 1. Toggle DoNotConvertToPackInMenu ON
             _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, false, doNotConvertToPackInMenu: true);
             var p = _repository.GetAllProducts().First(x => x.ID == id);
             Assert.True(p.DoNotConvertToPackInMenu);

             // 2. Toggle DoNotConvertToPackInMenu OFF
             _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, false, doNotConvertToPackInMenu: false);
             p = _repository.GetAllProducts().First(x => x.ID == id);
             Assert.False(p.DoNotConvertToPackInMenu);
             
             // 3. Toggle HideInMenu ON
             _repository.UpdateProduct(id, "Toggler", 1, 1, 1, 1, 0, 0, false, 0, false, 0, hideInMenu: true, false);
             p = _repository.GetAllProducts().First(x => x.ID == id);
             Assert.True(p.HideInMenu);
        }
        
        [Fact]
        public void DeleteProduct_ShouldSetIsDeleted_And_RestoreProduct_ShouldUnset()
        {
             int id = _repository.AddProduct("SoftDelete", 1, 1, 1, 1);
             
             _repository.DeleteProduct(id);
             var p = _repository.GetAllProducts().First(x => x.ID == id);
             Assert.True(p.IsDeleted);
             
             _repository.RestoreProduct(id);
             p = _repository.GetAllProducts().First(x => x.ID == id);
             Assert.False(p.IsDeleted);
        }
    }
}
