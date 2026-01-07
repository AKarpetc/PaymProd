using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests
{
    // Ensure no parallel execution conflicts on the static DatabaseHelper
    [Collection("Database Tests")]
    public class ProductRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly ProductRepository _repository;

        public ProductRepositoryTests()
        {
            // Create a unique temporary database file
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db");
            
            // Initialize the database (this sets the static ConnectionString in DatabaseHelper)
            DatabaseHelper.InitializeDatabase(_dbPath);
            
            _repository = new ProductRepository();
        }

        public void Dispose()
        {
            // Clear connection string to avoid reuse (optional but good practice)
            // DatabaseHelper doesn't have a Clear method, but re-init overwrites.
            
            // Try to delete the DB file
            try
            {
                // Force GC to release any SQLite handles held by un-disposed connections (if any)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch
            {
                // Ignored: Sometimes file lock persists due to connection pooling
            }
        }

        [Fact]
        public void AddProduct_ShouldSaveAllFlagsCorrectly()
        {
            // Arrange
            string name = "Test Product";
            bool hideInMenu = true;
            bool doNotConvert = true;
            bool avtomat = true;
            double price = 150.50;

            // Act
            int id = _repository.AddProduct(
                name: name,
                vesId: 1, // Assumes 'г' exists (ID 1 created by InitializeDatabase)
                typeId: 1, // Assumes 'Овощи' exists (ID 1 created by InitializeDatabase)
                fass: 1,
                izmerId: 1,
                prizMenu: 0,
                count: 100,
                automat: avtomat,
                countPeople: 0,
                mainCount: false,
                price: price,
                hideInMenu: hideInMenu,
                doNotConvertToPackInMenu: doNotConvert
            );

            // Assert
            var products = _repository.GetAllProducts();
            var product = products.FirstOrDefault(p => p.ID == id);

            Assert.NotNull(product);
            Assert.Equal(name, product.Name);
            Assert.Equal((decimal)price, product.Price);
            Assert.True(product.HideInMenu, "HideInMenu flag should be true");
            Assert.True(product.DoNotConvertToPackInMenu, "DoNotConvertToPackInMenu flag should be true");
            Assert.True(product.AutoAdd, "Avtomat (AutoAdd) flag should be true");
        }

        [Fact]
        public void UpdateProduct_ShouldUpdateFlagsAndValues()
        {
            // Arrange
            int id = _repository.AddProduct("Original Name", 1, 1, 1, 1, 0, 10, false, 0, false, 100, false, false);
            
            // Act
            _repository.UpdateProduct(
                id: id,
                name: "Updated Name",
                vesId: 1,
                typeId: 1,
                fass: 2,
                izmerId: 1,
                prizMenu: 0,
                count: 200,
                automat: true,
                countPeople: 10,
                mainCount: true,
                price: 250,
                hideInMenu: true,
                doNotConvertToPackInMenu: true
            );

            // Assert
            var result = _repository.GetAllProducts().First(p => p.ID == id);
            
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal(2, result.Fass);
            Assert.Equal(200, result.Count);
            Assert.Equal(250, result.Price);
            Assert.True(result.AutoAdd, "AutoAdd should be updated to true");
            Assert.True(result.HideInMenu, "HideInMenu should be updated to true");
            Assert.True(result.DoNotConvertToPackInMenu, "DoNotConvertToPackInMenu should be updated to true");
            Assert.True(result.MainCount, "MainCount (IsDiap) should be updated to true");
        }

        [Fact]
        public void DeleteProduct_ShouldSoftDelete()
        {
            // Arrange
            int id = _repository.AddProduct("To Delete", 1, 1, 1, 1);
            
            // Act
            _repository.DeleteProduct(id);
            var products = _repository.GetAllProducts(); // This usually filters out deleted items?
            
            // Let's verify via direct SQL or check if GetAllProducts excludes it.
            // Looking at ProductRepository.GetAllProducts SQL: 
            // "SELECT ... COALESCE(p.IsDeleted, 0) ... FROM Producrs p ..."
            // It does NOT have a WHERE IsDeleted = 0 clause! It returns everything.
            // Wait, looking at GetAllProducts implementation I saw earlier:
            /* 
               command.CommandText = @"
                SELECT ...
                FROM Producrs p ... 
                INNER JOIN Produkt_Type pt ...
                LEFT JOIN Mera ...
               ";
               It does NOT filter by IsDeleted. It returns the IsDeleted flag.
            */

             var deletedProduct = products.FirstOrDefault(p => p.ID == id);
             
             // Assert
             Assert.NotNull(deletedProduct);
             Assert.True(deletedProduct.IsDeleted, "Product should be marked as IsDeleted");
        }

        [Fact]
        public void AddProduct_DefaultsShouldBeFalse()
        {
            // Act
            int id = _repository.AddProduct("Default Flags", 1, 1, 1, 1);
            var product = _repository.GetAllProducts().First(p => p.ID == id);

            // Assert
            Assert.False(product.HideInMenu);
            Assert.False(product.DoNotConvertToPackInMenu);
            Assert.False(product.AutoAdd);
            Assert.False(product.IsDeleted);
        }

        [Fact]
        public void RestoreProduct_ShouldUnmarkDelete()
        {
             // Arrange
            int id = _repository.AddProduct("To Restore", 1, 1, 1, 1);
            _repository.DeleteProduct(id);
            
            // Act
            _repository.RestoreProduct(id);
            var product = _repository.GetAllProducts().First(p => p.ID == id);

            // Assert
            Assert.False(product.IsDeleted, "Product should be restored (IsDeleted = false)");
        }
    }
}
