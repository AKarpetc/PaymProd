using PaymProdNet9.Enums;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using PaymProdNet9.Data;
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
    private readonly SettingsRepository _settingsRepository;
    private readonly MenuRepository _menuRepository;
    private ReportMode? _currentReportMode;

    public PrintMenuPage()
    {
        InitializeComponent();
        _menuPrinter = new MenuPrinter();
        _menuPriceService = new MenuPriceService();
        _settingsRepository = new SettingsRepository();
        _menuRepository = new MenuRepository();
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
        _currentReportMode = null;
    }

    private void GenerateReportWithPrices_Click(object sender, RoutedEventArgs e)
    {
        // "Отчет с себестоимостью" -> ReportMode.Cost
        GenerateReport(ReportMode.Cost);
    }

    private void GenerateReportWithSalePrices_Click(object sender, RoutedEventArgs e)
    {
        // "Отчет с ценой" -> ReportMode.Price
        GenerateReport(ReportMode.Price);
    }

    private void GenerateReportWithoutPrices_Click(object sender, RoutedEventArgs e)
    {
        // "Отчет без цен" -> ReportMode.NoPrices
        GenerateReport(ReportMode.NoPrices);
    }

    private void GenerateReportFull_Click(object sender, RoutedEventArgs e)
    {
        // "Полный отчет" -> ReportMode.Full
        GenerateReport(ReportMode.Full);
    }

    private void GenerateReport(ReportMode mode)
    {
        try
        {
            if (Delicates == null || Delicates.Count == 0)
            {
                ShowPlaceholder("Нет данных для отображения.");
                return;
            }

            BuildDocument(mode);
            _currentReportMode = mode;
            SaveToWordButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при генерации отчета по меню", ex);
            ShowPlaceholder($"Ошибка при генерации отчета: {ex.Message}");
        }
    }

    private void BuildDocument(ReportMode reportMode)
    {
        if (Delicates == null || Delicates.Count == 0)
        {
            DocumentViewer.Document = new FlowDocument(new Paragraph(new Run("Нет данных для отображения.")));
            return;
        }

        // Landscape for Full Report, Portrait for others (default ~ 793/800)
        // A4 Portrait: Width ~ 793 (approx 21cm), Height ~ 1122
        // A4 Landscape: Width ~ 1122, Height ~ 793
        
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(30),
            ColumnWidth = double.PositiveInfinity,
            PageWidth = reportMode == ReportMode.Full ? 1122 : 980 // 980 was old default ?? A4 is ~793px at 96dpi. 980 is wide. Kept 980 for compat, 1122 for full.
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
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });
        var showPriceColumn = reportMode != ReportMode.NoPrices;
        
        decimal totalCostSum = 0; // Accumulator for Full Report Total Cost (Raw)
        
        if (showPriceColumn)
        {
            if (reportMode == ReportMode.Price)
            {
                table.Columns.Add(new TableColumn { Width = new GridLength(300) }); // Composition reduced
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Portion Cost
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Portion Price
                table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // Total Price
            }
            else if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
            {
                // Full/Cost Report columns
                table.Columns.Add(new TableColumn { Width = new GridLength(220) }); // Composition
                table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // Portion Cost
                table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // Portion Price
                table.Columns.Add(new TableColumn { Width = new GridLength(90) }); // Total Cost (Raw)
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Total Dish (Sum)
            }
            else
            {
                table.Columns.Add(new TableColumn { Width = new GridLength(500) });
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            }
        }
        else
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(650) });
        }

        var rowGroup = new TableRowGroup();
        decimal totalReportSum = 0;

        foreach (var group in groupedDelicates)
        {
            var headerRow = new TableRow();
            var headerCell = new TableCell(new Paragraph(new Run(group.Key.Type ?? "Без типа")
            {
                FontWeight = FontWeights.Bold
            }))
            {
                ColumnSpan = showPriceColumn ? (reportMode == ReportMode.Price ? 5 : (reportMode == ReportMode.Full || reportMode == ReportMode.Cost ? 6 : 3)) : 2,
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
            if (showPriceColumn)
            {
                if (reportMode == ReportMode.Price)
                {
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Себ. порции"));
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Цена порции"));
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Сумма, тг"));
                }
                else if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                {
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Себ.\nпорции"));
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Отп.\nцена"));
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Итог\nсеб."));
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Итог\nотп."));
                }
                else
                {
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Стоимость"));
                }
            }
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

                var compositionParagraph = BuildCompositionParagraph(delicate, reportMode, out var dishPrice);
                
                var compositionCell = new TableCell(compositionParagraph)
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Left
                };
                row.Cells.Add(compositionCell);

                if (showPriceColumn)
                {
                    // Сохраняем себестоимость до применения наценки
                    var rawDishTotal = dishPrice;

                    // Применяем наценку к итоговой стоимости блюда
                    if (reportMode == ReportMode.Price && delicate.DefaultMarkup > 0)
                        // Наценка хранится в DefaultMarkup (передана из MainNavigationWindow)
                        // Считаем, что наценка - это множитель в процентах (например, 200% = x2)
                        dishPrice = dishPrice * (delicate.DefaultMarkup / 100);

                    if (reportMode == ReportMode.Price || reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                    {
                        var portions = delicate.Count > 0 ? delicate.Count : 1;
                        var finalPriceForCalc = dishPrice; // In Full mode dishPrice is RAW cost, in Price mode it is MARKUP price

                         // Calculate portion cost (always based on raw cost)
                        var unitCost = rawDishTotal / portions;
                        var unitCostText = unitCost > 0 ? unitCost.ToString("N1", System.Globalization.CultureInfo.CurrentCulture) : "—";
                        var unitCostCell = new TableCell(new Paragraph(new Run(unitCostText)))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                        row.Cells.Add(unitCostCell);

                        // Calculate portion price
                        // In Price mode: based on markup price
                        // In Full mode: based on markup price?? Login says "all fields from menu report with prices but distinct"
                        // Menu Report (Price) -> Portion Price = (Raw * Markup) / Portions
                        // Full Report -> Portion Price = (Raw * Markup) / Portions ??
                        // "отчет должен быть такой же как отчет по себестоимости... но нужно включить все поля из отчета по меню с ценами... цена порции, себестоимость порции, поле сумма из отчета с ценами но с именем итог блюда"
                        
                        // So logic for Portion Price should be SAME as Price mode.
                        
                        // Calculate markup price locally if mode is Full (since dishPrice is raw in Full mode, see logic below)
                        var markupPrice = rawDishTotal;
                         if ((reportMode == ReportMode.Full || reportMode == ReportMode.Cost) && delicate.DefaultMarkup > 0)
                             markupPrice = rawDishTotal * (delicate.DefaultMarkup / 100);
                         else if (reportMode == ReportMode.Price)
                             markupPrice = dishPrice; // Already applied

                        var unitPrice = markupPrice / portions;
                        var unitPriceText = unitPrice > 0 ? unitPrice.ToString("N1", System.Globalization.CultureInfo.CurrentCulture) : "—";
                        var unitPriceCell = new TableCell(new Paragraph(new Run(unitPriceText)))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                        row.Cells.Add(unitPriceCell);
                    }

                    if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                    {
                        // 2.5 Total Cost (Итог себ.) - Raw Cost
                        var rawCostCell = new TableCell(new Paragraph(new Run(rawDishTotal > 0 ? FormatCurrency(rawDishTotal) : "—")))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                        row.Cells.Add(rawCostCell);
                    }

                    // 3. Общая сумма (Цена, тг)
                    var priceForTotalColumn = dishPrice;
                    if ((reportMode == ReportMode.Full || reportMode == ReportMode.Cost) && delicate.DefaultMarkup > 0)
                        priceForTotalColumn = dishPrice * (delicate.DefaultMarkup / 100);

                    var priceCell = new TableCell(new Paragraph(new Run(priceForTotalColumn > 0 ? FormatCurrency(priceForTotalColumn) : "—")))
                    {
                        Padding = new Thickness(4),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        TextAlignment = TextAlignment.Right
                    };
                    row.Cells.Add(priceCell);

                    // Накапливаем итоговую сумму
                    if ((reportMode == ReportMode.Full || reportMode == ReportMode.Cost) && delicate.DefaultMarkup > 0)
                        totalReportSum += dishPrice * (delicate.DefaultMarkup / 100);
                    else
                        totalReportSum += dishPrice;
                    
                    // For Full Report, also accumulate Total Cost
                    if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                    {
                        totalCostSum += rawDishTotal;
                    }
                }

                rowGroup.Rows.Add(row);
            }
        }

        // Добавляем строки итогов (только если есть колонка цен)
        if (showPriceColumn && Delicates.Any())
        {
            var settings = _settingsRepository.GetSettings();
            var effectiveServicePercent = settings.ServicePercent;

            if (MenuId > 0)
            {
                var menu = _menuRepository.GetMenuById(MenuId);
                if (menu?.ServicePercent != null) effectiveServicePercent = menu.ServicePercent.Value;
            }

            // 1. Подитог (Сумма без обслуживания) & Итог себестоимости (для Full)
            var subtotalRow = new TableRow();
            
            // Если режим Full или Cost, то в этой строке выводим И себестоимость И цену
            if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
            {
                 // Ячейка заголовка "Итого по меню" (Span 4: Dish, Comp, PCost, PPrice)
                 var subtotalTitleCell = new TableCell(new Paragraph(new Run(reportMode == ReportMode.Cost ? "ИТОГ" : "Итого по меню") { FontWeight = FontWeights.Bold }))
                 {
                     ColumnSpan = 4,
                     Padding = new Thickness(4),
                     TextAlignment = TextAlignment.Right
                 };
                 subtotalRow.Cells.Add(subtotalTitleCell);

                 // Ячейка Итог Себестоимость (Col 5)
                 var subtotalCostCell = new TableCell(new Paragraph(new Run(FormatCurrency(totalCostSum)) { FontWeight = FontWeights.Bold }))
                 {
                     Padding = new Thickness(4),
                     BorderBrush = Brushes.Black,
                     BorderThickness = new Thickness(1),
                     TextAlignment = TextAlignment.Right
                 };
                 subtotalRow.Cells.Add(subtotalCostCell);

                 // Ячейка Итог Блюда (Col 6)
                 var subtotalValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(totalReportSum)) { FontWeight = FontWeights.Bold }))
                 {
                     Padding = new Thickness(4),
                     BorderBrush = Brushes.Black,
                     BorderThickness = new Thickness(1),
                     TextAlignment = TextAlignment.Right
                 };
                 subtotalRow.Cells.Add(subtotalValueCell);
            }
            else
            {
                // Стандартный режим (Price)
                var subtotalTitleCell = new TableCell(new Paragraph(new Run("Итого по меню") { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = reportMode == ReportMode.Price ? 4 : 2,
                    Padding = new Thickness(4),
                    TextAlignment = TextAlignment.Right
                };
                subtotalRow.Cells.Add(subtotalTitleCell);

                var subtotalValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(totalReportSum)) { FontWeight = FontWeights.Bold }))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Right
                };
                subtotalRow.Cells.Add(subtotalValueCell);
            }
            
            rowGroup.Rows.Add(subtotalRow);

            // 2. Обслуживание (Только для Price и Full, НЕ для Cost)
            if (reportMode != ReportMode.Cost)
            {
                if (reportMode == ReportMode.Price || reportMode == ReportMode.Full)
                {
                    var serviceSum = totalReportSum * (effectiveServicePercent / 100);

                    var serviceRow = new TableRow();
                    var serviceTitleCell =
                        new TableCell(new Paragraph(new Run($"За обслуживание ({effectiveServicePercent:G}%)"))
                            { FontWeight = FontWeights.Bold })
                        {
                            ColumnSpan = reportMode == ReportMode.Price ? 4 : (reportMode == ReportMode.Full ? 5 : 2),
                            Padding = new Thickness(4),
                            TextAlignment = TextAlignment.Right
                        };
                    serviceRow.Cells.Add(serviceTitleCell);

                    var serviceValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(serviceSum))
                        { FontWeight = FontWeights.Bold }))
                    {
                        Padding = new Thickness(4),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        TextAlignment = TextAlignment.Right
                    };
                    serviceRow.Cells.Add(serviceValueCell);
                    rowGroup.Rows.Add(serviceRow);

                    // 3. ИТОГ - Single cell spanning full width
                    var grandTotal = totalReportSum + serviceSum;

                    var totalRow = new TableRow();
                    
                    // Determine span based on mode
                    int span = 2; // Default (Cost)
                    if (reportMode == ReportMode.Price) span = 5;
                    if (reportMode == ReportMode.Full) span = 6;

                    var totalCell = new TableCell(new Paragraph(new Run($"ИТОГ   {FormatCurrency(grandTotal)}")
                        { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, FontSize = 16 }))
                    {
                        ColumnSpan = span,
                        Padding = new Thickness(4),
                        TextAlignment = TextAlignment.Right
                    };
                    totalRow.Cells.Add(totalCell);
                    
                    rowGroup.Rows.Add(totalRow);
                }
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

    private Paragraph BuildCompositionParagraph(DelicatesColl delicate, ReportMode reportMode, out decimal dishTotal)
    {
        var paragraph = new Paragraph();
        var lines = BuildCompositionLines(delicate, reportMode, out dishTotal);

        if (lines.Count == 0)
        {
            paragraph.Inlines.Add(new Run("Без состава"));
            return paragraph;
        }

        // Если режим Price, мы не показываем цены компонентов, поэтому разделяем запятыми или новой строкой?
        // В старом коде для includePrices=true использовалась новая строка.
        // Для includePrices=false использовалась запятая.
        // Новая логика:
        // Price: новая строка (т.к. состав может быть длинным), но без цен.
        // Cost: новая строка, с ценами (как было).
        // NoPrices: запятая (как было).

        if (reportMode == ReportMode.NoPrices || reportMode == ReportMode.Price)
        {
            paragraph.Inlines.Add(new Run(string.Join(", ", lines)));
            return paragraph;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i] + ", "));
        }

        return paragraph;
    }

    private List<string> BuildCompositionLines(DelicatesColl delicate, ReportMode reportMode, out decimal dishTotal)
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
            string NormalizeUnitLocal(string unit)
            {
                return unit?.Trim().ToLowerInvariant() ?? string.Empty;
            }

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

            // Считаем цену компонента всегда, чтобы накопить dishTotal
            var priceInfo = _menuPriceService.GetComponentPriceInfo(MenuId, component, delicate.Count);
            dishTotal += priceInfo.TotalPrice;

            string line;
            if (reportMode == ReportMode.Cost || reportMode == ReportMode.Full)
                line = priceInfo.TotalPrice > 0
                    ? $"{productName} ({formattedWeight}) — {FormatCurrency(priceInfo.TotalPrice)} тг"
                    : $"{productName} ({formattedWeight}) — цена не указана";
            else
                // Price или NoPrices - без цены
                line = $"{productName} ({formattedWeight})";

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

    private string FormatCurrency(decimal value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.CurrentCulture);
    }

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
            if (!_currentReportMode.HasValue)
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
                _currentReportMode.Value,
                MenuId > 0 ? MenuId : null);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении отчета по меню", ex);
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}