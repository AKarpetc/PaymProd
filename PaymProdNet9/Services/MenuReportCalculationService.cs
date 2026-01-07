using PaymProdNet9.Data;
using PaymProdNet9.Enums;
using PaymProdNet9.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaymProdNet9.Services;

public class MenuReportResult
{
    public List<MenuReportItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ServicePercent { get; set; }
    public decimal ServiceAmount { get; set; }
    public decimal GrandTotal { get; set; }
}

public class MenuReportItem
{
    public DelicatesColl Delicate { get; set; }
    public string Name { get; set; }
    public decimal DishPrice { get; set; }
    public List<string> CompositionLines { get; set; } = new();
}

public class MenuReportCalculationService
{
    private readonly MenuPriceService _priceService;
    private readonly SettingsRepository _settingsRepository;
    private readonly MenuRepository _menuRepository;

    public MenuReportCalculationService()
    {
        _priceService = new MenuPriceService();
        _settingsRepository = new SettingsRepository();
        _menuRepository = new MenuRepository();
    }

    // Constructor for testing with mocks would be better, but sticking to no-DI simplicity for this project
    // We can add setters or virtuals if needed for mocking, but integration testing with DB is fine here.

    public MenuReportResult CalculateReport(List<DelicatesColl> delicates, int menuId, ReportMode reportMode)
    {
        var result = new MenuReportResult();

        // Determine Service Percent
        decimal effectiveServicePercent = 0;
        if (reportMode == ReportMode.Price)
        {
            var settings = _settingsRepository.GetSettings();
            effectiveServicePercent = settings.ServicePercent;

            if (menuId > 0)
            {
                var menu = _menuRepository.GetMenuById(menuId);
                if (menu?.ServicePercent != null) effectiveServicePercent = menu.ServicePercent.Value;
            }
        }

        result.ServicePercent = effectiveServicePercent;

        // Calculate Items
        foreach (var delicate in delicates)
        {
            if (delicate.Lcomp == null || !delicate.Lcomp.Any()) continue;

            var item = new MenuReportItem
            {
                Delicate = delicate,
                Name = delicate.Name
            };

            decimal rawDishPrice = 0;

            // Build composition logic to get total raw price
            foreach (var component in delicate.Lcomp)
            {
                // Calculate component price
                // Always calculate raw price first
                var count = delicate.Count > 0 ? delicate.Count : 1;
                var priceInfo = _priceService.GetComponentPriceInfo(menuId, component, count);
                rawDishPrice += priceInfo.TotalPrice;

                // Formatting logic for lines (optional to include here or keep in UI? 
                // Better to separate formatting. But for price calculation, this loop is what matters.)
            }

            if (reportMode == ReportMode.Price)
            {
                // Apply markup logic
                var finalPrice = rawDishPrice;
                if (delicate.DefaultMarkup > 0)
                    // Markup is percentage multiplier? (e.g. 200 = 2x? or +200%?)
                    // Code said: dishPrice = dishPrice * (delicate.DefaultMarkup / 100);
                    // So 200 means x2.
                    finalPrice = rawDishPrice * (delicate.DefaultMarkup / 100m);
                item.DishPrice = finalPrice;
            }
            else
            {
                // Cost mode or NoPrices
                item.DishPrice = rawDishPrice;
            }

            result.Items.Add(item);
            result.Subtotal += item.DishPrice;
        }

        // Calculate Totals
        if (reportMode == ReportMode.Price)
        {
            result.ServiceAmount = result.Subtotal * (effectiveServicePercent / 100m);
            result.GrandTotal = result.Subtotal + result.ServiceAmount;
        }
        else
        {
            result.ServiceAmount = 0;
            result.GrandTotal = result.Subtotal;
        }

        return result;
    }
}