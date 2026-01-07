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
    public class DelicateRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DelicateRepository _repository;
        private readonly ProductRepository _productRepository;

        public DelicateRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Delic_{Guid.NewGuid()}.db");
            DatabaseHelper.InitializeDatabase(_dbPath);
            _repository = new DelicateRepository();
            _productRepository = new ProductRepository();
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

        public static IEnumerable<object[]> GetFlagCombinations()
        {
            // 3 flags: AutoAdd, HideInMenu, HideInProductReport
            // 2^3 = 8 combinations
            for (int i = 0; i < 8; i++)
            {
                bool autoAdd = (i & 1) != 0;
                bool hideInMenu = (i & 2) != 0;
                bool hideInReport = (i & 4) != 0;
                yield return new object[] { autoAdd, hideInMenu, hideInReport };
            }
        }

        [Theory]
        [MemberData(nameof(GetFlagCombinations))]
        public void AddDelicate_ShouldSaveFlagsCorrectly(bool autoAdd, bool hideInMenu, bool hideInReport)
        {
            // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            string name = $"Delicate_{autoAdd}_{hideInMenu}_{hideInReport}";

            // Act
            int id = _repository.AddDelicate(typeId, name, 100, 1, autoAdd, hideInMenu, hideInReport);

            // Assert
            var delicate = _repository.GetDelicateById(id);
            Assert.NotNull(delicate);
            Assert.Equal(name, delicate.Name);
            Assert.Equal(autoAdd, delicate.AutoAdd);
            Assert.Equal(hideInMenu, delicate.HideInMenu);
            Assert.Equal(hideInReport, delicate.HideInProductReport);
        }

        [Theory]
        [MemberData(nameof(GetFlagCombinations))]
        public void UpdateDelicate_ShouldUpdateFlagsCorrectly(bool autoAdd, bool hideInMenu, bool hideInReport)
        {
            // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            int id = _repository.AddDelicate(typeId, "Original Name", 100, 1, false, false, false);

            // Act
            _repository.UpdateDelicate(id, typeId, "Updated Name", 200, 2, autoAdd, hideInMenu, hideInReport);

            // Assert
            var delicate = _repository.GetDelicateById(id);
            Assert.NotNull(delicate);
            Assert.Equal("Updated Name", delicate.Name);
            Assert.Equal(200, delicate.Ves);
            Assert.Equal(2, delicate.Count);
            Assert.Equal(autoAdd, delicate.AutoAdd);
            Assert.Equal(hideInMenu, delicate.HideInMenu);
            Assert.Equal(hideInReport, delicate.HideInProductReport);
        }

        [Fact]
        public void Components_CRUD_ShouldWork()
        {
            // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            int delId = _repository.AddDelicate(typeId, "Test Dish", 100, 1, false);
            
            // Create a product to use as component
            int prodId = _productRepository.AddProduct("Component Prod", 1, 1, 1, 1);

            // Act: Add Component
            _repository.AddComponent(delId, prodId, 50, "Detail");

            // Assert: Verify Component Added
            var delicate = _repository.GetDelicateById(delId);
            Assert.Single(delicate.Lcomp);
            var comp = delicate.Lcomp.First();
            Assert.Equal(prodId, comp.Prodid);
            Assert.Equal(50, comp.Ves);
            // Note: Detail handling in GetDelicateComponents appends to name "Name(Detail)" in NameT property
            Assert.Contains("Detail", comp.NameT); 

            // Act: Update Component Weight
            _repository.UpdateComponentWeight(delId, prodId, 75);
            
            // Assert: Verify Update
            delicate = _repository.GetDelicateById(delId);
            comp = delicate.Lcomp.First();
            Assert.Equal(75, comp.Ves);

            // Act: Delete Component
            _repository.DeleteComponent(comp.Id);

            // Assert: Verify Deletion
            delicate = _repository.GetDelicateById(delId);
            Assert.Empty(delicate.Lcomp);
        }

        [Fact]
        public void DeleteDelicate_ShouldSoftDelete()
        {
            // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            int id = _repository.AddDelicate(typeId, "To Delete", 100, 1, false);

            // Act
            _repository.DeleteDelicate(id);

            // Assert
            var delicate = _repository.GetDelicateById(id);
            Assert.NotNull(delicate);
            Assert.True(delicate.IsDeleted);

            // Check if filtered out from GetAllDelicates list (if implementation filters it)
            // DelicateRepository.GetAllDelicates sends explicit IsDeleted column, does NOT filter in SQL.
            // "SELECT ... FROM Delicates d ... ORDER BY ..." (no WHERE IsDeleted=0)
            // So it should still be in the list, but marked IsDeleted.
             var all = _repository.GetAllDelicates();
             var d = all.FirstOrDefault(x => x.Id == id);
             Assert.NotNull(d);
             Assert.True(d.IsDeleted);
             
             // Check GetAvailableDelicatesForMenu - THIS ONE FILTERS DELETED
             // "WHERE ... COALESCE(d.IsDeleted, 0) = 0"
             var available = _repository.GetAvailableDelicatesForMenu();
             Assert.DoesNotContain(available, x => x.Id == id);
        }

        [Fact]
        public void RestoreDelicate_ShouldUnsetIsDeleted()
        {
             // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            int id = _repository.AddDelicate(typeId, "To Restore", 100, 1, false);
            _repository.DeleteDelicate(id);

            // Act
            _repository.RestoreDelicate(id);

            // Assert
            var delicate = _repository.GetDelicateById(id);
            Assert.False(delicate.IsDeleted);
            
            var available = _repository.GetAvailableDelicatesForMenu();
            Assert.Contains(available, x => x.Id == id);
        }

        [Fact]
        public void UpdateDefaultMarkup_ShouldWork()
        {
            // Arrange
            int typeId = _repository.AddDelicateType("Test Type");
            int id = _repository.AddDelicate(typeId, "Markup Test", 100, 1, false);
            
            // Act
            _repository.UpdateDelicateDefaultMarkup(id, 300);

            // Assert
            var delicate = _repository.GetDelicateById(id);
            Assert.Equal(300, delicate.DefaultMarkup);
        }
    }
}
