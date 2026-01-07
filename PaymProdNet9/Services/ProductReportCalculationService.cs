using PaymProdNet9.Models;
using System.Collections.Generic;
using System.Linq;

namespace PaymProdNet9.Services;

public class ProductReportCalculationService
{
    private readonly MenuPriceService _priceService;

    public ProductReportCalculationService()
    {
        _priceService = new MenuPriceService();
    }

    public List<DelicatesCollForSvod> CalculateSummary(IEnumerable<MenuDel_act> items, int defaultMenuId = 0)
    {
        var summaryData = new List<DelicatesCollForSvod>();

        foreach (var delicate in items.Where(d => d.Lcomp != null && d.Lcomp.Any() && !d.HideInProductReport))
        foreach (var component in delicate.Lcomp)
        {
            // Logic extracted from ProductsReportPage/ReportPage
            decimal totalWeight;
            decimal dishCountForPrice;

            if (delicate.Del_id < 0)
            {
                // Raw product added directly
                totalWeight = component.Ves;
                dishCountForPrice = 1; // Ves is already total
            }
            else
            {
                // Dish component
                totalWeight = component.Ves * delicate.Countpor;
                dishCountForPrice = delicate.Countpor;
            }

            // Determine Menu ID context
            var priceMenuId = delicate.Idmen > 0 ? delicate.Idmen : defaultMenuId;

            var item = new DelicatesCollForSvod
            {
                Del = delicate.Del,
                Del_id = delicate.Del_id,
                Countpor = delicate.Countpor,
                Name = component.Name,
                Type = component.Type,
                Ves = component.Ves,
                Mera = component.Mera,
                Fass = component.Fass,
                FassIz = component.FassIz,
                NameT = component.NameT,
                Itog = totalWeight,
                ItogFass = component.Fass > 0
                    ? totalWeight / component.Fass
                    : 0
            };

            // Helper to perform price check
            if (priceMenuId > 0)
            {
                if (delicate.Del_id < 0)
                {
                    // Special case for raw products logic mirrored from UI
                    var unitPrice = _priceService.GetUnitPrice(priceMenuId, component.Prodid);
                    if (component.Fass > 0)
                    {
                        var packageCount = totalWeight / component.Fass;
                        item.TotalPrice = decimal.Round(unitPrice * packageCount, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        item.TotalPrice = decimal.Round(unitPrice * totalWeight, 2, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    var priceInfo = _priceService.GetComponentPriceInfo(priceMenuId, component, dishCountForPrice);
                    item.TotalPrice = priceInfo.TotalPrice;
                }
            }

            summaryData.Add(item);
        }

        return summaryData;
    }
}