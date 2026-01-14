using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PaymProdNet9.Tests;

[Collection("Database Tests")]
public class MassUpdateTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DelicateRepository _delicateRepo;
    private readonly MenuRepository _menuRepo;

    public MassUpdateTests()
    {
        // Создаем уникальную базу данных для теста
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_MassUpdate_{Guid.NewGuid()}.db");
        
        // Инициализируем базу данных (это настроит DatabaseHelper на использование этого пути)
        DatabaseHelper.InitializeDatabase(_dbPath);
        
        _delicateRepo = new DelicateRepository();
        _menuRepo = new MenuRepository();
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
            // Игнорируем ошибки при удалении
        }
    }

    [Fact]
    public void ApplyMarkupToAll_ShouldUpdateCatalogAndMenu()
    {
        // ---------------------------------------------------------
        // Arrange (Подготовка)
        // ---------------------------------------------------------
        decimal targetMarkup = 450.0m;

        // 1. Создаем тип блюда
        var typeId = _delicateRepo.AddDelicateType("Test Type");

        // 2. Добавляем блюда с РАЗНЫМИ наценками по умолчанию
        var dish1Id = _delicateRepo.AddDelicate(typeId, "Dish 100%", 100, 1, false);
        _delicateRepo.UpdateDelicateDefaultMarkup(dish1Id, 100);

        var dish2Id = _delicateRepo.AddDelicate(typeId, "Dish 200%", 100, 1, false);
        _delicateRepo.UpdateDelicateDefaultMarkup(dish2Id, 200);

        // 3. Создаем меню и добавляем блюда
        var menuId = _menuRepo.CreateMenu("Test Menu", 10, "Details", "");
        
        _menuRepo.AddDelicateToMenu(menuId, dish1Id, 5);
        _menuRepo.AddDelicateToMenu(menuId, dish2Id, 5);

        // Проверяем исходное состояние (Sanity Check)
        var initialDish1 = _delicateRepo.GetDelicateById(dish1Id);
        var initialDish2 = _delicateRepo.GetDelicateById(dish2Id);
        Assert.Equal(100, initialDish1.DefaultMarkup);
        Assert.Equal(200, initialDish2.DefaultMarkup);

        var initialMenuItems = _menuRepo.GetMenuDelicates(menuId);
        var item1 = initialMenuItems.First(x => x.Del_id == dish1Id);
        var item2 = initialMenuItems.First(x => x.Del_id == dish2Id);
        Assert.Equal(100, item1.Markup);
        Assert.Equal(200, item2.Markup);

        // ---------------------------------------------------------
        // Act (Действие - эмуляция логики из ParametersPage.xaml.cs)
        // ---------------------------------------------------------

        // 1. Обновляем справочник
        var updatedCount = _delicateRepo.UpdateAllDefaultMarkups(targetMarkup);

        // 2. Обновляем текущее меню
        var updatedMenuCount = _menuRepo.UpdateMarkupForMenu(menuId, targetMarkup);

        // ---------------------------------------------------------
        // Assert (Проверка)
        // ---------------------------------------------------------

        // 1. Проверяем, что обновление затронуло нужные записи (возвращаемые значения)
        // У нас 2 блюда в справочнике, значит updatedCount >= 2? 
        // Нет, UpdateAllDefaultMarkups обновляет таблицу Delicates.
        // Если база пустая (кроме наших 2 блюд), то должно быть 2.
        // Но InitializeDatabase может создавать дефолтные? Нет, InitializeDefaultData не создает блюда, только типы.
        Assert.True(updatedCount >= 2); 

        // 2. Проверяем справочник
        var finalDish1 = _delicateRepo.GetDelicateById(dish1Id);
        var finalDish2 = _delicateRepo.GetDelicateById(dish2Id);
        Assert.Equal(targetMarkup, finalDish1.DefaultMarkup);
        Assert.Equal(targetMarkup, finalDish2.DefaultMarkup);

        // 3. Проверяем меню
        var finalMenuItems = _menuRepo.GetMenuDelicates(menuId);
        
        foreach(var item in finalMenuItems)
        {
            if (item.Del_id > 0) // Только для блюд (продукты могут иметь свою логику или быть обновлены тоже, если они в Menu_Delicates)
            {
                 // Логика UpdateMarkupForMenu обновляет ALL Menu_Delicates for that menu
                 // Продукты с Priz_menu=1 тоже находятся в Menu_Delicates (с отрицательными ID)
                 // Но UpdateMarkupForMenu просто делает UPDATE Menu_Delicates SET Markup = ...
                 // Значит и продукты обновятся.
                 
                 Assert.Equal(targetMarkup, item.Markup);
            }
        }
    }
}
