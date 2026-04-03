using PaymProdNet9.Data;
using PaymProdNet9.Enums;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using PaymProdNet9.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace PaymProdNet9.Tests
{
    [Collection("Database Tests")]
    public class MenuReportCalculationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _dbPath;
        private readonly MenuRepository _menuRepo;
        private readonly ProductRepository _prodRepo;

        public MenuReportCalculationTests(ITestOutputHelper output)
        {
            _output = output;
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid()}.db");
            DatabaseHelper.InitializeDatabase(_dbPath);

            _menuRepo = new MenuRepository(_dbPath);
            _prodRepo = new ProductRepository(_dbPath);
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch
                {
                }
            }
        }

        [Fact]
        public void ShouldCalculatePortionCostAndPriceCorrectly()
        {
                // 1. Создаем меры
                int gramId = _prodRepo.AddMeasure("г", 1, "г"); 
                int kgId = _prodRepo.AddMeasure("кг", 1000, "кг");
                
                // 2. Создаем тип продукта
                int typeId = _prodRepo.AddProductType("Тестовый Тип", 1);
                
                // 3. Создаем продукт: Цена 1000 за кг
                // vesId=gramId, typeId=typeId, fass=1, izmerId=kgId
                int productId = _prodRepo.AddProduct(name: "Test Ingredient", vesId: gramId, typeId: typeId, fass: 1.0, izmerId: kgId); 
                _prodRepo.UpdateProductPrice(productId, 1000);

                // 4. Создаем меню
                int menuId = _menuRepo.CreateMenu("Test Menu", 10, "Details", "2025-01-01");
                
                // 5. Сохраняем цену продукта в меню (важно для MenuPriceService)
                _prodRepo.SaveMenuProductPrice(menuId, productId, 1000);

                // 6. Подготавливаем данные для расчета
                var delicate = new DelicatesColl
                {
                    Name = "Test Dish",
                    Count = 5, // 5 порций
                    DefaultMarkup = 200, // Наценка 200% (множитель 2.0)
                    Lcomp = new List<Components>()
                };

                // Добавляем компонент: 100г продукта
                // Себестоимость 100г = (1000 руб / 1000г) * 100г = 100 руб.
                // Общая себестоимость на 5 порций: 100 * 5 = 500 руб.
                delicate.Lcomp.Add(new Components
                {
                    Prodid = productId,
                    Ves = 100,
                    Mera = "г",
                    Name = "Test Ingredient",
                    Fass = 1000 // Важно: коэффициент пересчета (1000г в 1кг)
                });

                var delicatesList = new List<DelicatesColl> { delicate };

                // 7. Инициализируем сервис с тестовым путем БД
                var service = new MenuReportCalculationService(_dbPath);

                // 8. Выполняем расчет в режиме Price
                var result = service.CalculateReport(delicatesList, menuId, ReportMode.Price);

                // 9. Проверяем результаты
                var item = result.Items[0];
                
                // Raw Dish Price (Total for 5 portions): 500
                // Portion Cost = 500 / 5 = 100
                Assert.Equal(100m, item.PortionCost);

                // Dish Price with Markup: 500 * (200 / 100) = 1000
                // Portion Price = 1000 / 5 = 200
                Assert.Equal(200m, item.PortionPrice);
                
                Assert.Equal(1000m, item.DishPrice); // Total price

                // Verify Totals
                Assert.Equal(100m, result.TotalPortionCost); // Sum of portion costs (1 item: 100)
                Assert.Equal(200m, result.TotalPortionPrice); // Sum of portion prices (1 item: 200)
        }
    }
}