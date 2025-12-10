using System.Collections.Generic;
using System.Linq;
using PaymProdNet9.Data;
using PaymProdNet9.Models;

namespace PaymProdNet9.Services;

public record ComponentPriceInfo(decimal Units, decimal UnitPrice, decimal TotalPrice);

/// <summary>
///     Сервис расчета цен продуктов в меню
/// </summary>
public class MenuPriceService
{
    private readonly ProductRepository _productRepository = new();
    private Dictionary<int, decimal>? _basePricesCache;
    private readonly Dictionary<int, Dictionary<int, decimal>> _menuPricesCache = new();

    public ComponentPriceInfo GetComponentPriceInfo(int menuId, Components component, decimal dishCount)
    {
        var unitPrice = GetUnitPrice(menuId, component.Prodid);
        var requiredUnits = CalculateRequiredUnits(component, dishCount);
        var total = unitPrice * requiredUnits;

        return new ComponentPriceInfo(requiredUnits, unitPrice, decimal.Round(total, 2));
    }

    public decimal GetUnitPrice(int menuId, int productId)
    {
        if (menuId > 0)
        {
            if (!_menuPricesCache.TryGetValue(menuId, out var menuPrices))
            {
                var menuPriceList = _productRepository.GetMenuProductPrices(menuId);
                menuPrices = menuPriceList.ToDictionary(p => p.ProductID, p => (decimal)p.Price);
                _menuPricesCache[menuId] = menuPrices;
            }

            if (menuPrices.TryGetValue(productId, out var price))
                return price;
        }

        _basePricesCache ??= _productRepository.GetAllProducts().ToDictionary(p => p.ID, p => p.Price);

        return _basePricesCache.TryGetValue(productId, out var basePrice) ? basePrice : 0;
    }

    private static decimal CalculateRequiredUnits(Components component, decimal dishCount)
    {
        var safeCount = dishCount <= 0 ? 1 : dishCount;
        
        // Если dishCount = 1, это означает, что component.Ves уже содержит итоговое количество
        // (например, для продуктов с AutoAdd, добавленных напрямую)
        // В этом случае используем component.Ves как есть
        decimal totalWeight;
        if (safeCount == 1 && component.Ves > 0)
        {
            // Проверяем, не является ли это продуктом с итоговым количеством
            // Если component.Ves уже большое число (больше чем обычно для одной порции),
            // вероятно это итоговое количество
            totalWeight = component.Ves;
        }
        else
        {
            // Для компонентов блюд умножаем на количество порций
            totalWeight = component.Ves * safeCount;
        }

        if (component.Fass > 0)
            return totalWeight / component.Fass;

        // Если фасовка не указана, считаем по общему весу
        return totalWeight;
    }
}

