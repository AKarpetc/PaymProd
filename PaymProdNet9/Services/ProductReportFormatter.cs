using PaymProdNet9.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PaymProdNet9.Services;

public class ProductReportFormatter
{
    private readonly List<Measure> _measures;

    public ProductReportFormatter(List<Measure> measures)
    {
        _measures = measures;
    }

    public (string amount, string unit, double roundedAmount, decimal priceMultiplier) FormatAmountWithRoundedValue(GroupedProduct product)
    {
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        var normalizedUnit = NormalizeUnit(defaultUnit);
        var measure = FindMeasure(defaultUnit);

        if (!IsDiscreteUnit(normalizedUnit))
            return FormatContinuousAmountWithRoundedValue(product, defaultUnit, normalizedUnit, measure);

        return FormatDiscreteAmountWithRoundedValue(product, defaultUnit, measure);
    }

    public decimal RecalculatePrice(GroupedProduct product, double roundedAmount)
    {
        if (product.TotalPrice <= 0 || roundedAmount <= 0)
            return product.TotalPrice;

        double originalAmount;
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        var normalizedUnit = NormalizeUnit(defaultUnit);
        var measure = FindMeasure(defaultUnit);

        if (!IsDiscreteUnit(normalizedUnit))
        {
            if (product.Fass > 0 && !string.IsNullOrWhiteSpace(product.FassIz))
                originalAmount = (double)product.TotalPackages;
            else
                originalAmount = (double)product.TotalWeight;
        }
        else
        {
            var effectivePackSize = product.Fass > 0
                ? (double)product.Fass
                : measure?.Fass > 0
                    ? measure.Fass
                    : 1d;

            originalAmount = product.TotalPackages > 0
                ? (double)product.TotalPackages
                : effectivePackSize > 0
                    ? (double)(product.TotalWeight / (decimal)effectivePackSize)
                    : (double)product.TotalWeight;
        }

        if (originalAmount <= 0)
            return product.TotalPrice;

        var unitPrice = product.TotalPrice / (decimal)originalAmount;
        return decimal.Round(unitPrice * (decimal)roundedAmount, 2, MidpointRounding.AwayFromZero);
    }

    public (string amount, string unit, decimal priceMultiplier) FormatAmount(GroupedProduct product)
    {
        // Wrapper for simpler format requirement (as used in MenuPrinter)
        var result = FormatAmountWithRoundedValue(product);
        return (result.amount, result.unit, result.priceMultiplier);
    }
    
    // --- Private / Helper Methods ---

    private Measure? FindMeasure(string? measureUnit)
    {
        if (string.IsNullOrWhiteSpace(measureUnit))
            return null;

        static Measure? PickPreferred(IEnumerable<Measure> candidates)
        {
            return candidates
                .OrderByDescending(m => m.Fass > 1 ? 1 : 0)
                .ThenBy(m => m.Id)
                .FirstOrDefault();
        }

        var lower = measureUnit.ToLower().Trim();

        var exactMatches = _measures.Where(m =>
            m.Name.Equals(measureUnit, StringComparison.OrdinalIgnoreCase));
        var exact = PickPreferred(exactMatches);
        if (exact != null)
            return exact;

        var partialMatches = _measures.Where(m =>
            lower.Contains(m.Name.ToLower().Trim()) ||
            m.Name.ToLower().Trim().Contains(lower));
        return PickPreferred(partialMatches);
    }

    private static string NormalizeUnit(string unit)
    {
        return unit?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsDiscreteUnit(string unit)
    {
        if (string.IsNullOrEmpty(unit)) return false;

        string[] discreteKeywords = { "шт", "бут", "бан", "пач", "рулон", "компл", "уп", "набор" };
        return discreteKeywords.Any(unit.Contains);
    }

    private Measure? FindChildMeasure(string? parentUnit)
    {
        if (string.IsNullOrWhiteSpace(parentUnit)) return null;

        var normalizedParent = NormalizeUnit(parentUnit);
        return _measures.FirstOrDefault(m =>
            m.Fass > 0 &&
            !string.IsNullOrWhiteSpace(m.FassIzmer) &&
            NormalizeUnit(m.FassIzmer) == normalizedParent);
    }

    private (string amount, string unit, double roundedAmount, decimal priceMultiplier) FormatContinuousAmountWithRoundedValue(
        GroupedProduct product,
        string originalUnit,
        string normalizedUnit,
        Measure? measure)
    {
        var roundingPrecision = measure?.RoundingPrecision ?? 2;
        var totalValue = (double)product.TotalWeight;
        var displayUnit = originalUnit;
        var currentMeasure = measure;
        const int maxUnitHops = 10;
        decimal priceMultiplier = 1m;

        if (product.Fass > 0 && !string.IsNullOrWhiteSpace(product.FassIz))
        {
            totalValue /= (double)product.Fass;
            priceMultiplier = 1m; 
            
            displayUnit = product.FassIz;
            normalizedUnit = NormalizeUnit(displayUnit);

            currentMeasure = FindMeasure(product.FassIz) ?? currentMeasure;
            if (currentMeasure != null) roundingPrecision = currentMeasure.RoundingPrecision;
        }

        if (currentMeasure != null)
        {
            var hop = 0;
            while (hop++ < maxUnitHops &&
                   currentMeasure.Fass > 0 &&
                   totalValue >= currentMeasure.Fass &&
                   !string.IsNullOrWhiteSpace(currentMeasure.FassIzmer))
            {
                var parent = FindMeasure(currentMeasure.FassIzmer);
                if (parent == null) break;

                if (NormalizeUnit(parent.Name) == NormalizeUnit(displayUnit)) break;

                totalValue /= currentMeasure.Fass;
                priceMultiplier *= (decimal)currentMeasure.Fass;

                currentMeasure = parent;
                displayUnit = currentMeasure.Name;
                roundingPrecision = currentMeasure.RoundingPrecision;
            }

            normalizedUnit = NormalizeUnit(displayUnit);

            hop = 0;
            while (product.Fass <= 0 && totalValue < 1 && hop++ < maxUnitHops)
            {
                var child = FindChildMeasure(normalizedUnit);
                if (child == null || child.Fass <= 0) break;

                if (NormalizeUnit(child.Name) == normalizedUnit) break;

                totalValue *= child.Fass;
                priceMultiplier /= (decimal)child.Fass;

                currentMeasure = child;
                displayUnit = child.Name;
                roundingPrecision = child.RoundingPrecision;
                normalizedUnit = NormalizeUnit(displayUnit);

                if (totalValue >= 1) break;
            }
        }

        double roundedValue;
        if (roundingPrecision <= 0)
        {
            roundedValue = Math.Ceiling(totalValue);
        }
        else
        {
            var multiplier = Math.Pow(10, roundingPrecision);
            roundedValue = Math.Ceiling(totalValue * multiplier) / multiplier;
        }

        var formatted = roundingPrecision <= 0
            ? ((int)roundedValue).ToString(CultureInfo.CurrentCulture)
            : roundedValue.ToString($"F{roundingPrecision}", CultureInfo.CurrentCulture);

        return (formatted, displayUnit, roundedValue, priceMultiplier);
    }

    private (string amount, string unit, double roundedAmount, decimal priceMultiplier) FormatDiscreteAmountWithRoundedValue(
        GroupedProduct product,
        string defaultUnit,
        Measure? measure)
    {
        var effectiveMeasure = measure;
        var effectivePackSize = product.Fass > 0
            ? (double)product.Fass
            : effectiveMeasure?.Fass > 0
                ? effectiveMeasure.Fass
                : 1d;

        var value = product.TotalPackages > 0
            ? (double)product.TotalPackages
            : effectivePackSize > 0
                ? (double)(product.TotalWeight / (decimal)effectivePackSize)
                : (double)product.TotalWeight;

        var priceMultiplier = (decimal)effectivePackSize;
        if (priceMultiplier <= 0) priceMultiplier = 1;

        var precision = measure?.MenuRoundingPrecision ?? measure?.RoundingPrecision ?? 0;
        double roundedValue;

        if (precision <= 0)
        {
            roundedValue = Math.Ceiling(value);
        }
        else
        {
            var multiplier = Math.Pow(10, precision);
            roundedValue = Math.Ceiling(value * multiplier) / multiplier;
        }

        var formatted = precision <= 0
            ? ((int)roundedValue).ToString(CultureInfo.CurrentCulture)
            : roundedValue.ToString($"F{precision}", CultureInfo.CurrentCulture);

        var unitText = !string.IsNullOrWhiteSpace(product.FassIz)
            ? product.FassIz
            : defaultUnit;

        return (formatted, unitText, roundedValue, priceMultiplier);
    }
}
