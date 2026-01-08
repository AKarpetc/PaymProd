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
        var showPriceColumn = reportMode != ReportMode.NoPrices;
        
        if (showPriceColumn)
        {
            if (reportMode == ReportMode.Price)
            {
                table.Columns.Add(new TableColumn { Width = new GridLength(300) }); // Composition reduced
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Portion Cost
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Portion Price
                table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // Total Price
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
                ColumnSpan = showPriceColumn ? (reportMode == ReportMode.Price ? 5 : 3) : 2,
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
                else
                {
                    columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Цена, тг"));
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

                    if (reportMode == ReportMode.Price)
                    {
                        var portions = delicate.Count > 0 ? delicate.Count : 1;

                        // 1. Себестоимость порции
                        var unitCost = rawDishTotal / portions;
                        var unitCostCell = new TableCell(new Paragraph(new Run(unitCost > 0 ? FormatCurrency(unitCost) : "—")))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                        row.Cells.Add(unitCostCell);

                        // 2. Цена порции
                        var unitPrice = dishPrice / portions;
                        var unitPriceCell = new TableCell(new Paragraph(new Run(unitPrice > 0 ? FormatCurrency(unitPrice) : "—")))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                        row.Cells.Add(unitPriceCell);
                    }

                    var priceCell =
                        new TableCell(new Paragraph(new Run(dishPrice > 0 ? FormatCurrency(dishPrice) : "—")))
                        {
                            Padding = new Thickness(4),
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Right
                        };
                    row.Cells.Add(priceCell);

                    // Накапливаем итоговую сумму
                    totalReportSum += dishPrice;
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

            // 1. Подитог (Сумма без обслуживания)
            var subtotalRow = new TableRow();
            // Объединяем ячейки названия и состава
            var subtotalTitleCell =
                new TableCell(new Paragraph(new Run("Итого по меню") { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = reportMode == ReportMode.Price ? 4 : 2,
                    Padding = new Thickness(4),
                    TextAlignment = TextAlignment.Right
                };
            subtotalRow.Cells.Add(subtotalTitleCell);

            var subtotalValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(totalReportSum))
                { FontWeight = FontWeights.Bold }))
            {
                Padding = new Thickness(4),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Right
            };
            subtotalRow.Cells.Add(subtotalValueCell);
            rowGroup.Rows.Add(subtotalRow);

            // 2. Обслуживание
            if (reportMode ==
                ReportMode.Price) // Обычно обслуживание показывается только в Price отчете, но если и в Cost, то убрать условие
            {
                var serviceSum = totalReportSum * (effectiveServicePercent / 100);

                var serviceRow = new TableRow();
                var serviceTitleCell =
                    new TableCell(new Paragraph(new Run($"За обслуживание ({effectiveServicePercent:G}%)"))
                        { FontWeight = FontWeights.Bold })
                    {
                        ColumnSpan = reportMode == ReportMode.Price ? 4 : 2,
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

                // 3. ИТОГ
                var grandTotal = totalReportSum + serviceSum;

                var totalRow = new TableRow();
                var totalTitleCell = new TableCell(new Paragraph(new Run("ИТОГ")
                    { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, FontSize = 16 }))
                {
                    ColumnSpan = reportMode == ReportMode.Price ? 4 : 2,
                    Padding = new Thickness(4),
                    TextAlignment = TextAlignment.Right
                };
                totalRow.Cells.Add(totalTitleCell);

                var totalValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(grandTotal))
                    { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, FontSize = 16 }))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Right
                };
                totalRow.Cells.Add(totalValueCell);
                rowGroup.Rows.Add(totalRow);
            }
            else if (reportMode == ReportMode.Cost)
            {
                // Для отчета по себестоимости тоже можно вывести ИТОГ (сумму себестоимостей)
                var totalRow = new TableRow();
                var totalTitleCell = new TableCell(new Paragraph(new Run("ИТОГ") { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = 2,
                    Padding = new Thickness(4),
                    TextAlignment = TextAlignment.Right
                };
                totalRow.Cells.Add(totalTitleCell);

                var totalValueCell = new TableCell(new Paragraph(new Run(FormatCurrency(totalReportSum))
                    { FontWeight = FontWeights.Bold }))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Right
                };
                totalRow.Cells.Add(totalValueCell);
                rowGroup.Rows.Add(totalRow);
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
            paragraph.Inlines.Add(new Run(lines[i]));
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
            if (reportMode == ReportMode.Cost)
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