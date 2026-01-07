using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class ReferenceDataTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ProductRepository _repository;

    public ReferenceDataTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Ref_{Guid.NewGuid()}.db");
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

    [Fact]
    public void ProductTypes_CRUD_ShouldWork()
    {
        // Create
        var typeName = "Test Type";
        var id = _repository.AddProductType(typeName, 10, true);

        var types = _repository.GetProductTypes();
        var type = types.FirstOrDefault(t => t.Id == id);

        Assert.NotNull(type);
        Assert.Equal(typeName, type.Name);
        Assert.Equal(10, type.SortOrder);
        Assert.True(type.HideInMenu);

        // Update
        _repository.UpdateProductType(id, "Updated Type", 20, false);
        type = _repository.GetProductTypes().First(t => t.Id == id);

        Assert.Equal("Updated Type", type.Name);
        Assert.Equal(20, type.SortOrder);
        Assert.False(type.HideInMenu);

        // Delete
        var deleted = _repository.DeleteProductType(id);
        Assert.True(deleted);

        // Verify soft delete (GetProductTypes filters out IsDeleted=1)
        // Implementation of GetProductTypes: "WHERE COALESCE(IsDeleted, 0) = 0"
        types = _repository.GetProductTypes();
        Assert.DoesNotContain(types, t => t.Id == id);
    }

    [Fact]
    public void Measures_CRUD_ShouldWork()
    {
        // Create
        var name = "Test Measure";
        var id = _repository.AddMeasure(name, 100, "g", 3, 4);

        var measures = _repository.GetMeasures();
        var measure = measures.FirstOrDefault(m => m.Id == id);
        Assert.NotNull(measure);
        Assert.Equal(name, measure.Name);
        Assert.Equal(100, measure.Fass);
        Assert.Equal("g", measure.FassIzmer);
        Assert.Equal(3, measure.RoundingPrecision);
        Assert.Equal(4, measure.MenuRoundingPrecision);

        // Update
        _repository.UpdateMeasure(id, "Updated Measure", 200, "kg", 1, 2);
        measure = _repository.GetMeasures().First(m => m.Id == id);

        Assert.Equal("Updated Measure", measure.Name);
        Assert.Equal(200, measure.Fass);
        Assert.Equal("kg", measure.FassIzmer);
        Assert.Equal(1, measure.RoundingPrecision);
        Assert.Equal(2, measure.MenuRoundingPrecision);

        // Delete
        _repository.DeleteMeasure(id);
        // Verify soft delete
        // Verify soft delete (filtered out by GetMeasures)
        measures = _repository.GetMeasures();
        Assert.DoesNotContain(measures, m => m.Id == id);
    }
}