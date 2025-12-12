using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PaymProdNet9.Pages;

public partial class PrintMenuPage : Page
{
    public List<DelicatesColl> Delicates { get; set; } = new();
    public List<string> BanquetInfo { get; set; } = new();
    public int MenuId { get; set; }

    private readonly MenuPrinter _menuPrinter;
    private readonly MenuPriceService _menuPriceService;
    private bool? _currentReportWithPrices;

    public PrintMenuPage()
    {
        InitializeComponent();
        _menuPrinter = new MenuPrinter();
        _menuPriceService = new MenuPriceService();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ShowPlaceholder();
    }

    private void ShowPlaceholder(string? message = null)
    {
        var text = message ?? "Выберите тип отчета для отображения.";
        DocumentViewer.Document = new FlowDocument(new Paragraph(new Run(text)));
        SaveToWordButton.Visibility = Visibility.Collapsed;
        _currentReportWithPrices = null;
    }

    private void GenerateReportWithPrices_Click(object sender, RoutedEventArgs e)
    {
        GenerateReport(true);
    }

    private void GenerateReportWithoutPrices_Click(object sender, RoutedEventArgs e)
    {
        GenerateReport(false);
    }

    private void GenerateReport(bool includePrices)
    {
        try
        {
            if (Delicates == null || Delicates.Count == 0)
            {
                ShowPlaceholder("Нет данных для отображения.");
                return;
            }

            BuildDocument(includePrices);
            _currentReportWithPrices = includePrices;
            SaveToWordButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при генерации отчета по меню", ex);
            ShowPlaceholder($"Ошибка при генерации отчета: {ex.Message}");
        }
    }

    private void BuildDocument(bool includePrices)
    {
        if (Delicates == null || Delicates.Count == 0)
        {
            DocumentViewer.Document = new FlowDocument(new Paragraph(new Run("Нет данных для отображения.")));
            return;
        }

        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(30),
            ColumnWidth = double.PositiveInfinity,
            PageWidth = 980
        };

        var titleParagraph = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var menuTitle = BanquetInfo.Count >= 3
            ? $"{BanquetInfo[0]} • {BanquetInfo[1]} человек • {BanquetInfo[2]}"
            : "Меню банкета";
        titleParagraph.Inlines.Add(menuTitle);
        document.Blocks.Add(titleParagraph);

        var groupedDelicates = Delicates
            .Where(d => d.Lcomp != null && d.Lcomp.Any())
            .GroupBy(d => new { d.Type, d.TypeSortOrder })
            .OrderBy(g => g.Key.TypeSortOrder)
            .ThenBy(g => g.Key.Type);

        var table = new Table();
        table.Columns.Add(new TableColumn { Width = new GridLength(250) });
        table.Columns.Add(new TableColumn { Width = includePrices ? new GridLength(500) : new GridLength(650) });
        if (includePrices)
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });

        var rowGroup = new TableRowGroup();

        foreach (var group in groupedDelicates)
        {
            var headerRow = new TableRow();
            var headerCell = new TableCell(new Paragraph(new Run(group.Key.Type ?? "Без типа")
            {
                FontWeight = FontWeights.Bold
            }))
            {
                ColumnSpan = includePrices ? 3 : 2,
                Background = Brushes.LightGray,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(4),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };
            headerRow.Cells.Add(headerCell);
            rowGroup.Rows.Add(headerRow);

            var columnsHeaderRow = new TableRow();
            columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Блюдо"));
            columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Состав"));
            if (includePrices)
                columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Цена, тг"));
            rowGroup.Rows.Add(columnsHeaderRow);

            foreach (var delicate in group)
            {
                var row = new TableRow();
                var nameCell = new TableCell(new Paragraph(new Run(delicate.Name)))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    FontWeight = FontWeights.SemiBold
                };
                row.Cells.Add(nameCell);

                var compositionParagraph = BuildCompositionParagraph(delicate, includePrices, out var dishPrice);
                var compositionCell = new TableCell(compositionParagraph)
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Left
                };
                row.Cells.Add(compositionCell);

                if (includePrices)
                {
                    var priceCell = new TableCell(new Paragraph(new Run(dishPrice > 0 ? FormatCurrency(dishPrice) : "—")))
                    {
                        Padding = new Thickness(4),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        TextAlignment = TextAlignment.Right
                    };
                    row.Cells.Add(priceCell);
                }

                rowGroup.Rows.Add(row);
            }
        }

        table.RowGroups.Add(rowGroup);
        document.Blocks.Add(table);

        DocumentViewer.Document = document;
    }

    private static TableCell CreateColumnHeaderCell(string text)
    {
        var headerText = new Run(text) { FontWeight = FontWeights.SemiBold };
        var cell = new TableCell(new Paragraph(headerText))
        {
            Background = Brushes.WhiteSmoke,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            TextAlignment = TextAlignment.Center
        };
        return cell;
    }

    private Paragraph BuildCompositionParagraph(DelicatesColl delicate, bool includePrices, out decimal dishTotal)
    {
        var paragraph = new Paragraph();
        var lines = BuildCompositionLines(delicate, includePrices, out dishTotal);

        if (lines.Count == 0)
        {
            paragraph.Inlines.Add(new Run("Без состава"));
            return paragraph;
        }

        if (!includePrices)
        {
            paragraph.Inlines.Add(new Run(string.Join(", ", lines)));
            return paragraph;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i]));
        }

        return paragraph;
    }

    private List<string> BuildCompositionLines(DelicatesColl delicate, bool includePrices, out decimal dishTotal)
    {
        var lines = new List<string>();
        dishTotal = 0;
        if (delicate.Lcomp == null || !delicate.Lcomp.Any()) return lines;

        foreach (var component in delicate.Lcomp)
        {
            var productName = !string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name;
            var baseUnit = !string.IsNullOrWhiteSpace(component.Mera) ? component.Mera : "г";
            var count = delicate.Count > 0 ? delicate.Count : 1;
            
            // Логика как в отчете по товарам: показываем основную единицу, если нет перерасчета в фасовку
            decimal displayValue;
            string displayUnit;
            var totalWeight = component.Ves * count;
            
            // Локальная функция для нормализации единиц
            string NormalizeUnitLocal(string unit) => unit?.Trim().ToLowerInvariant() ?? string.Empty;
            
            // Нормализуем единицы для сравнения (как в отчете по товарам)
            var baseUnitNormalized = NormalizeUnitLocal(baseUnit);
            var fassIzNormalized = NormalizeUnitLocal(component.FassIz ?? string.Empty);
            
            // Если на продукте стоит флаг "не переводить в фасованные" — всегда показываем в базовой единице
            if (component.DoNotConvertToPackInMenu)
            {
                displayValue = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
                displayUnit = baseUnit;
            }
            else
            {
                // Проверяем, нужно ли пересчитывать в фасовку (как в отчете по товарам)
                // Пересчитываем только если: есть фасовка, единица фасовки отличается от базовой, и вес >= фасовка
                if (component.Fass > 0 && 
                    !string.IsNullOrWhiteSpace(component.FassIz) && 
                    fassIzNormalized != baseUnitNormalized &&
                    totalWeight >= component.Fass)
                {
                    // Есть перерасчет в фасовку - показываем в фасовке
                    var packageCount = totalWeight / component.Fass;
                    displayValue = Math.Round(packageCount, 2, MidpointRounding.AwayFromZero);
                    displayUnit = !string.IsNullOrWhiteSpace(component.FassIz) ? component.FassIz : baseUnit;
                }
                else
                {
                    // Нет перерасчета в фасовку - показываем в основных единицах
                    displayValue = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
                    displayUnit = baseUnit;
                }
            }
            
            var formattedWeight = FormatValueOld(displayValue, displayUnit);

            string line;
            if (includePrices)
            {
                var priceInfo = _menuPriceService.GetComponentPriceInfo(MenuId, component, delicate.Count);
                dishTotal += priceInfo.TotalPrice;
                line = priceInfo.TotalPrice > 0
                    ? $"{productName} ({formattedWeight}) — {FormatCurrency(priceInfo.TotalPrice)} тг"
                    : $"{productName} ({formattedWeight}) — цена не указана";
            }
            else
            {
                line = $"{productName} ({formattedWeight})";
            }

            lines.Add(line);
        }
        return lines;
    }
    
    /// <summary>
    /// Форматирование значения по логике старого приложения (Math.Round)
    /// </summary>
    private static string FormatValueOld(decimal value, string unit)
    {
        // В старом приложении использовалось Math.Round с 2 знаками
        // Если значение целое, показываем без дробной части
        if (value == Math.Truncate(value))
            return $"{(int)value}{unit}";
        
        return $"{value:F2}{unit}";
    }

    private string FormatCurrency(decimal value) =>
        Math.Round(value, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.CurrentCulture);

    private static string FormatValue(decimal value, string unit)
    {
        var precision = unit.ToLower(CultureInfo.CurrentCulture).Contains("шт") ? 0 : 2;
        if (precision <= 0)
            return $"{Math.Ceiling(value)} {unit}";

        var multiplier = (decimal)Math.Pow(10, precision);
        var rounded = Math.Ceiling(value * multiplier) / multiplier;
        return $"{rounded.ToString($"F{precision}", CultureInfo.CurrentCulture)} {unit}";
    }

    private void SaveToWord_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentReportWithPrices.HasValue)
            {
                MessageBox.Show("Сначала сформируйте отчет.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var menuTitle = BanquetInfo.Count >= 3
                ? $"{BanquetInfo[0]}, {BanquetInfo[1]} человек, {BanquetInfo[2]}"
                : "Меню";

            _menuPrinter.PrintMenu(
                Delicates,
                menuTitle,
                includePrices: _currentReportWithPrices.Value,
                menuId: MenuId > 0 ? MenuId : null);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении отчета по меню", ex);
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

