using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class MenuPrinterFullMenuTests : IDisposable
{
    private readonly string _dbPath;
    private readonly MenuPrinter _printer;
    private readonly DelicateRepository _dishRepo;
    private readonly ProductRepository _prodRepo;

    public MenuPrinterFullMenuTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_MenuPrinter_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _printer = new MenuPrinter();
        _dishRepo = new DelicateRepository();
        _prodRepo = new ProductRepository();
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
    public void PrintCustomFullMenu_ShouldCreateDocument()
    {
        // Arrange
        var pId = _prodRepo.AddProduct("Test Prod", 1, 1, 1, 1, price: 50);
        var typeId = _dishRepo.AddDelicateType("Hot Dishes");
        var dId = _dishRepo.AddDelicate(typeId, "Test Dish", 100, 1, false, false, false);
        _dishRepo.UpdateDelicateDefaultMarkup(dId, 200); // 200% markup
        _dishRepo.AddComponent(dId, pId, 100);

        var allDelicates = _dishRepo.GetAvailableDelicatesForMenu(null);

        // Act & Assert
        // We call it with openFile=false so it doesn't try to pop up MS Word.
        var exception = Record.Exception(() => _printer.PrintCustomFullMenu(allDelicates, showCost: true, showPrice: true, openFile: false));
        
        Assert.Null(exception);
    }
}
