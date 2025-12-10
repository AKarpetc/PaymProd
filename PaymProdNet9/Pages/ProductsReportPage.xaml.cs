using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PaymProdNet9.Pages;

public partial class ProductsReportPage : Page
{
    private ObservableCollection<MenuDel_act> _menuDelicates;
    private List<string> _banquetInfo;
    private readonly MenuPrinter _menuPrinter;
    private readonly MenuPriceService _menuPriceService;
    private bool? _currentReportWithPrices;

    public ObservableCollection<MenuDel_act>? MenuDelicates { get; set; }
    public List<string>? BanquetInfo { get; set; }
    public int MenuId { get; set; }

    public ProductsReportPage()
    {
        InitializeComponent();

        _menuDelicates = new ObservableCollection<MenuDel_act>();
        _banquetInfo = new List<string>();
        _menuPrinter = new MenuPrinter();
        _menuPriceService = new MenuPriceService();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // If data was set from navigation
        if (MenuDelicates != null) _menuDelicates = MenuDelicates;
        if (BanquetInfo != null) _banquetInfo = BanquetInfo;

        ShowPlaceholder();
    }

    private void ShowPlaceholder(string? message = null)
    {
        ReportDocument.Blocks.Clear();
        var text = message ?? "Выберите тип отчета.";
        ReportDocument.Blocks.Add(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            FontStyle = FontStyles.Italic
        });
        SaveToWordButton.Visibility = Visibility.Collapsed;
        PrintButton.Visibility = Visibility.Collapsed;
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

    /// <summary>
    /// Генерация отчета по товарам
    /// </summary>
    private void GenerateReport(bool includePrices)
    {
        try
        {
            ReportDocument.Blocks.Clear();

            var headerParagraph = new Paragraph();
            headerParagraph.Inlines.Add(new Run("Отчет по товарам")
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold
            });
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(new Run($"Банкет: {_banquetInfo[0]}"));
            headerParagraph.Inlines.Add(new LineBreak());
            var dateText = DateTime.TryParse(_banquetInfo[2], out var date)
                ? date.ToString("dd.MM.yyyy")
                : _banquetInfo[2];
            headerParagraph.Inlines.Add(new Run($"Дата: {dateText}"));
            headerParagraph.Inlines.Add(new LineBreak());
            headerParagraph.Inlines.Add(new Run($"Количество гостей: {_banquetInfo[1]} человек"));
            headerParagraph.TextAlignment = TextAlignment.Center;
            headerParagraph.FontSize = 16;
            headerParagraph.FontWeight = FontWeights.Bold;
            ReportDocument.Blocks.Add(headerParagraph);
            ReportDocument.Blocks.Add(new Paragraph()); // пустая строка

            var summaryData = GenerateSummaryData();
            if (!summaryData.Any())
            {
                ShowPlaceholder("Нет данных для отображения.");
                return;
            }

            var productRepository = new ProductRepository();
            var measures = productRepository.GetMeasures();
            var productTypes = productRepository.GetProductTypes();
            var productTypesDict = productTypes.ToDictionary(pt => pt.Name, pt => pt.SortOrder);

            var groupedByType = summaryData
                .GroupBy(r => r.Type ?? "Без типа")
                .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                .ThenBy(g => g.Key)
                .ToList();

            if (includePrices)
                BuildSingleColumnTableWithPrices(groupedByType, measures);
            else
                BuildStandardTable(groupedByType, measures, summaryData.Count);

            _currentReportWithPrices = includePrices;
            SaveToWordButton.Visibility = Visibility.Visible;
            PrintButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при генерации отчета по продуктам", ex);
            MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            ShowPlaceholder("Ошибка при генерации отчета.");
        }
    }

    private void BuildStandardTable(List<IGrouping<string, DelicatesCollForSvod>> groupedByType,
        List<Measure> measures, int totalItems)
    {
        var singleColumnMode = totalItems < 20;

        var mainTable = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        if (singleColumnMode)
        {
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(280) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(70) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
        }
        else
        {
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(280) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(70) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(280) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(70) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
        }

        var mainGroup = new TableRowGroup();
        mainTable.RowGroups.Add(mainGroup);

        var rows = new List<List<TableCell>>();

        foreach (var group in groupedByType)
        {
            var groupRows = CreateTypeSectionRows(group, measures);
            rows.AddRange(groupRows);
            rows.Add(CreateSpacerRow());
        }

        if (rows.Count > 0 && rows.Last().All(c => string.IsNullOrWhiteSpace(GetCellText(c))))
            rows.RemoveAt(rows.Count - 1);

        var leftRows = rows.ToList();
        var rightRows = new List<List<TableCell>>();

        if (!singleColumnMode)
        {
            var middleIndex = rows.Count % 2 == 0 ? rows.Count / 2 : rows.Count / 2 + 1;
            leftRows = rows.Take(middleIndex).ToList();
            rightRows = rows.Skip(middleIndex).ToList();
        }

        var maxRows = Math.Max(leftRows.Count, rightRows.Count);
        for (var i = 0; i < maxRows; i++)
        {
            var row = new TableRow();

            if (singleColumnMode)
            {
                var leftCells = i < leftRows.Count ? leftRows[i] : CreateSpacerRow();
                foreach (var cell in leftCells)
                    row.Cells.Add(cell);
            }
            else
            {
                var leftCells = i < leftRows.Count ? leftRows[i] : CreateSpacerRow();
                var rightCells = i < rightRows.Count ? rightRows[i] : CreateSpacerRow();

                foreach (var cell in leftCells)
                    row.Cells.Add(cell);

                row.Cells.Add(CreateSeparatorCell());

                foreach (var cell in rightCells)
                    row.Cells.Add(cell);
            }

            mainGroup.Rows.Add(row);
        }

        AddTableToDocument(mainTable);
    }

    private void BuildSingleColumnTableWithPrices(List<IGrouping<string, DelicatesCollForSvod>> groupedByType,
        List<Measure> measures)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(350) });
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        foreach (var typeGroup in groupedByType)
        {
            var headerRow = new TableRow();
            headerRow.Cells.Add(CreateHeaderCell(typeGroup.Key, 4));
            group.Rows.Add(headerRow);

            var titlesRow = new TableRow();
            titlesRow.Cells.Add(CreateTitleCell("Продукт"));
            titlesRow.Cells.Add(CreateTitleCell("Количество"));
            titlesRow.Cells.Add(CreateTitleCell("Ед."));
            titlesRow.Cells.Add(CreateTitleCell("Цена"));
            group.Rows.Add(titlesRow);

            var groupedProducts = GetGroupedProducts(typeGroup);

            foreach (var product in groupedProducts)
            {
                var (amountText, unitText, roundedAmount) = FormatAmountWithRoundedValue(product, measures);
                
                // Пересчитываем цену на основе округленного количества
                var recalculatedPrice = RecalculatePrice(product, roundedAmount, measures);
                var priceText = FormatPrice(recalculatedPrice);

                var dataRow = new TableRow();
                dataRow.Cells.Add(CreateValueCell(product.Name));
                dataRow.Cells.Add(CreateValueCell(amountText, TextAlignment.Right));
                dataRow.Cells.Add(CreateValueCell(unitText, TextAlignment.Center));
                dataRow.Cells.Add(CreateValueCell(priceText, TextAlignment.Right));
                group.Rows.Add(dataRow);
            }
        }

        AddTableToDocument(table);
    }

    private void AddTableToDocument(Table table)
    {
        var figure = new Figure
        {
            HorizontalAnchor = FigureHorizontalAnchor.PageCenter,
            WrapDirection = WrapDirection.None,
            Width = new FigureLength(1, FigureUnitType.Content)
        };
        figure.Blocks.Add(table);

        var containerParagraph = new Paragraph();
        containerParagraph.Inlines.Add(figure);
        ReportDocument.Blocks.Add(containerParagraph);
    }

    private List<List<TableCell>> CreateTypeSectionRows(IGrouping<string, DelicatesCollForSvod> group,
        List<Measure> measures)
    {
        var rows = new List<List<TableCell>>();

        // Заголовок типа продукта (занимает 3 колонки)
        var headerRow = new List<TableCell>
        {
            CreateHeaderCell(group.Key, 3)
        };
        rows.Add(headerRow);

        // Заголовки колонок
        var titlesRow = new List<TableCell>
        {
            CreateTitleCell("Продукт"),
            CreateTitleCell("Количество"),
            CreateTitleCell("Ед.")
        };
        rows.Add(titlesRow);

        // Строки с продуктами
        var groupedProducts = GetGroupedProducts(group);

        foreach (var product in groupedProducts)
        {
            var (amountText, unitText) = FormatAmount(product, measures);

            var productRow = new List<TableCell>
            {
                CreateValueCell(product.Name),
                CreateValueCell(amountText, TextAlignment.Right),
                CreateValueCell(unitText, TextAlignment.Center)
            };
            rows.Add(productRow);
        }

        return rows;
    }

    private List<TableCell> CreateSpacerRow()
    {
        return new List<TableCell>
        {
            CreateEmptyCell(),
            CreateEmptyCell(),
            CreateEmptyCell()
        };
    }

    private TableCell CreateSeparatorCell()
    {
        return new TableCell(new Paragraph(new Run(" ")))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1, 0, 1, 0), // Только левая и правая границы
            Padding = new Thickness(10, 0, 10, 0),
            Background = Brushes.White
        };
    }

    private TableCell CreateEmptyCell()
    {
        return new TableCell(new Paragraph(new Run(" ")))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            Background = Brushes.White
        };
    }

    private TableCell CreateHeaderCell(string text, int columnSpan)
    {
        var cell = new TableCell(new Paragraph(new Run(text) { FontWeight = FontWeights.Bold }))
        {
            ColumnSpan = columnSpan,
            TextAlignment = TextAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(227, 234, 242)), // E3EAF2
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };
        return cell;
    }

    private string GetCellText(TableCell cell)
    {
        if (cell.Blocks.Count > 0 && cell.Blocks.ToArray()[0] is Paragraph para && para.Inlines.Count > 0 &&
            para.Inlines.ToArray()[0] is Run run) return run.Text;
        return string.Empty;
    }

    private TableCell CreateTitleCell(string text)
    {
        return new TableCell(new Paragraph(new Run(text) { FontWeight = FontWeights.SemiBold }))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            TextAlignment = TextAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(221, 235, 247)) // DDEBF7
        };
    }

    private TableCell CreateValueCell(string text, TextAlignment alignment = TextAlignment.Left)
    {
        return new TableCell(new Paragraph(new Run(text)))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            TextAlignment = alignment,
            Background = Brushes.White
        };
    }

    private Measure? FindMeasure(List<Measure> measures, string? measureUnit)
    {
        if (string.IsNullOrWhiteSpace(measureUnit))
            return null;

        static Measure? PickPreferred(IEnumerable<Measure> candidates) =>
            candidates
                .OrderByDescending(m => m.Fass > 1 ? 1 : 0)
                .ThenBy(m => m.Id)
                .FirstOrDefault();

        var lower = measureUnit.ToLower().Trim();

        var exactMatches = measures.Where(m =>
            m.Name.Equals(measureUnit, StringComparison.OrdinalIgnoreCase));
        var exact = PickPreferred(exactMatches);
        if (exact != null)
            return exact;

        var partialMatches = measures.Where(m =>
            lower.Contains(m.Name.ToLower().Trim()) ||
            m.Name.ToLower().Trim().Contains(lower));
        return PickPreferred(partialMatches);
    }

    /// <summary>
    /// Сохранение в Word
    /// </summary>
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

            var summaryData = GenerateSummaryData();
            _menuPrinter.PrintReport(summaryData,
                $"{_banquetInfo[0]}, {_banquetInfo[1]} человек, {_banquetInfo[2]}",
                includePrices: _currentReportWithPrices.Value);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении отчета по продуктам в Word", ex);
            MessageBox.Show($"Ошибка при создании документа: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Печать отчета
    /// </summary>
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_currentReportWithPrices.HasValue)
            {
                MessageBox.Show("Сначала сформируйте отчет.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)ReportDocument).DocumentPaginator,
                    "Отчет по продуктам");
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при печати отчета по продуктам", ex);
            MessageBox.Show($"Ошибка при печати: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Генерация сводных данных для экспорта
    /// </summary>
    private List<DelicatesCollForSvod> GenerateSummaryData()
    {
        var summaryData = new List<DelicatesCollForSvod>();

        foreach (var delicate in _menuDelicates.Where(d => d.Lcomp != null && d.Lcomp.Any()))
        foreach (var component in delicate.Lcomp)
        {
            // Для продуктов, добавленных напрямую (отрицательный Del_id), component.Ves уже содержит итоговое количество на банкет
            // Для компонентов блюд нужно умножать на количество порций
            decimal totalWeight;
            decimal dishCountForPrice;
            
            if (delicate.Del_id < 0)
            {
                // Это продукт, добавленный напрямую - используем component.Ves как есть
                totalWeight = component.Ves;
                dishCountForPrice = 1; // Для расчета цены используем 1, так как Ves уже содержит итоговое количество
            }
            else
            {
                // Это компонент блюда - умножаем на количество порций
                totalWeight = component.Ves * delicate.Countpor;
                dishCountForPrice = delicate.Countpor;
            }

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

            if (MenuId > 0)
            {
                // Для продуктов, добавленных напрямую (отрицательный Del_id), рассчитываем цену на основе итогового количества
                if (delicate.Del_id < 0)
                {
                    // Получаем цену за единицу и умножаем на итоговое количество
                    var unitPrice = _menuPriceService.GetUnitPrice(MenuId, component.Prodid);
                    if (component.Fass > 0)
                    {
                        // Если есть фасовка, рассчитываем количество упаковок
                        var packageCount = totalWeight / component.Fass;
                        item.TotalPrice = decimal.Round(unitPrice * packageCount, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        // Если нет фасовки, рассчитываем по общему весу
                        item.TotalPrice = decimal.Round(unitPrice * totalWeight, 2, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    // Для компонентов блюд используем стандартный расчет
                    var priceInfo = _menuPriceService.GetComponentPriceInfo(MenuId, component, dishCountForPrice);
                    item.TotalPrice = priceInfo.TotalPrice;
                }
            }

            summaryData.Add(item);
        }

        return summaryData;
    }

    private IEnumerable<GroupedProduct> GetGroupedProducts(IGrouping<string, DelicatesCollForSvod> group)
    {
        return group
            .GroupBy(r => r.NameT ?? r.Name)
            .Select(g => new GroupedProduct
            {
                Name = g.Key,
                TotalWeight = g.Sum(r => r.Itog),
                TotalPackages = g.Sum(r => r.Fass > 0 ? r.ItogFass : 0),
                FassIz = g.First().FassIz ?? g.First().Mera ?? "",
                Mera = g.First().Mera ?? "",
                Fass = g.First().Fass,
                TotalPrice = g.Sum(r => r.TotalPrice)
            })
            .OrderBy(p => p.Name);
    }

    private (string amount, string unit, double roundedAmount) FormatAmountWithRoundedValue(GroupedProduct product, List<Measure> measures)
    {
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        var normalizedUnit = NormalizeUnit(defaultUnit);
        var measure = FindMeasure(measures, defaultUnit);

        if (!IsDiscreteUnit(normalizedUnit))
        {
            return FormatContinuousAmountWithRoundedValue(product, defaultUnit, normalizedUnit, measure, measures);
        }

        return FormatDiscreteAmountWithRoundedValue(product, defaultUnit, measure);
    }

    private (string amount, string unit) FormatAmount(GroupedProduct product, List<Measure> measures)
    {
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        var normalizedUnit = NormalizeUnit(defaultUnit);
        var measure = FindMeasure(measures, defaultUnit);

        if (!IsDiscreteUnit(normalizedUnit))
        {
            return FormatContinuousAmount(product, defaultUnit, normalizedUnit, measure, measures);
        }

        return FormatDiscreteAmount(product, defaultUnit, measure);
    }

    private static string NormalizeUnit(string unit) =>
        unit?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool IsDiscreteUnit(string unit)
    {
        if (string.IsNullOrEmpty(unit)) return false;

        string[] discreteKeywords = { "шт", "бут", "бан", "пач", "рулон", "компл", "уп", "набор" };
        return discreteKeywords.Any(unit.Contains);
    }

    private static Measure? FindChildMeasure(List<Measure> measures, string? parentUnit)
    {
        if (string.IsNullOrWhiteSpace(parentUnit))
        {
            return null;
        }

        var normalizedParent = NormalizeUnit(parentUnit);
        return measures.FirstOrDefault(m =>
            m.Fass > 0 &&
            !string.IsNullOrWhiteSpace(m.FassIzmer) &&
            NormalizeUnit(m.FassIzmer) == normalizedParent);
    }

    private (string amount, string unit) FormatContinuousAmount(
        GroupedProduct product,
        string originalUnit,
        string normalizedUnit,
        Measure? measure,
        List<Measure> measures)
    {
        var (formatted, displayUnit, _) = FormatContinuousAmountWithRoundedValue(product, originalUnit, normalizedUnit, measure, measures);
        return (formatted, displayUnit);
    }

    private (string amount, string unit, double roundedAmount) FormatContinuousAmountWithRoundedValue(
        GroupedProduct product,
        string originalUnit,
        string normalizedUnit,
        Measure? measure,
        List<Measure> measures)
    {
        var roundingPrecision = measure?.RoundingPrecision ?? 2;
        var totalValue = (double)product.TotalWeight;
        var displayUnit = originalUnit;
        var currentMeasure = measure;
        const int maxUnitHops = 10;

        if (product.Fass > 0 && !string.IsNullOrWhiteSpace(product.FassIz))
        {
            totalValue /= (double)product.Fass;
            displayUnit = product.FassIz;
            normalizedUnit = NormalizeUnit(displayUnit);

            currentMeasure = FindMeasure(measures, product.FassIz) ?? currentMeasure;
            if (currentMeasure != null)
            {
                roundingPrecision = currentMeasure.RoundingPrecision;
            }
        }

        if (currentMeasure != null)
        {
            // Конвертация вверх (например, грамм -> кг) при достижении фасовки
            var hop = 0;
            while (hop++ < maxUnitHops &&
                   currentMeasure.Fass > 0 &&
                   totalValue >= currentMeasure.Fass &&
                   !string.IsNullOrWhiteSpace(currentMeasure.FassIzmer))
            {
                var parent = FindMeasure(measures, currentMeasure.FassIzmer);
                if (parent == null)
                {
                    break;
                }

                if (NormalizeUnit(parent.Name) == NormalizeUnit(displayUnit))
                {
                    break;
                }

                totalValue /= currentMeasure.Fass;
                currentMeasure = parent;
                displayUnit = currentMeasure.Name;
                roundingPrecision = currentMeasure.RoundingPrecision;
            }

            normalizedUnit = NormalizeUnit(displayUnit);

            // Конвертация вниз (например, кг -> грамм) если итог меньше 1
            hop = 0;
            while (totalValue < 1 && hop++ < maxUnitHops)
            {
                var child = FindChildMeasure(measures, normalizedUnit);
                if (child == null || child.Fass <= 0)
                {
                    break;
                }

                if (NormalizeUnit(child.Name) == normalizedUnit)
                {
                    break;
                }

                totalValue *= child.Fass;
                currentMeasure = child;
                displayUnit = child.Name;
                roundingPrecision = child.RoundingPrecision;
                normalizedUnit = NormalizeUnit(displayUnit);

                if (totalValue >= 1)
                {
                    break;
                }
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

        return (formatted, displayUnit, roundedValue);
    }

    private (string amount, string unit) FormatDiscreteAmount(
        GroupedProduct product,
        string defaultUnit,
        Measure? measure)
    {
        var (formatted, unitText, _) = FormatDiscreteAmountWithRoundedValue(product, defaultUnit, measure);
        return (formatted, unitText);
    }

    private (string amount, string unit, double roundedAmount) FormatDiscreteAmountWithRoundedValue(
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

        return (formatted, unitText, roundedValue);
    }

    /// <summary>
    /// Пересчитывает цену на основе округленного количества
    /// </summary>
    private decimal RecalculatePrice(GroupedProduct product, double roundedAmount, List<Measure> measures)
    {
        if (product.TotalPrice <= 0 || roundedAmount <= 0)
            return product.TotalPrice;

        // Определяем исходное количество для расчета единичной цены
        // Используем ту же логику, что и в FormatAmount для определения исходного количества
        double originalAmount;
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        var normalizedUnit = NormalizeUnit(defaultUnit);
        var measure = FindMeasure(measures, defaultUnit);

        if (!IsDiscreteUnit(normalizedUnit))
        {
            // Для непрерывных единиц: если есть фасовка, используем TotalPackages, иначе TotalWeight
            // Это соответствует логике в FormatContinuousAmount
            if (product.Fass > 0 && !string.IsNullOrWhiteSpace(product.FassIz))
            {
                // Исходное количество в единицах фасовки (до округления)
                originalAmount = (double)product.TotalPackages;
            }
            else
            {
                // Исходное количество в базовых единицах
                originalAmount = (double)product.TotalWeight;
            }
        }
        else
        {
            // Для дискретных единиц: используем TotalPackages или TotalWeight / Fass
            // Это соответствует логике в FormatDiscreteAmount
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

        // Вычисляем единичную цену на основе исходного количества
        var unitPrice = product.TotalPrice / (decimal)originalAmount;

        // Пересчитываем цену на основе округленного количества
        return decimal.Round(unitPrice * (decimal)roundedAmount, 2, MidpointRounding.AwayFromZero);
    }


    private string FormatPrice(decimal value) =>
        value > 0 ? value.ToString("N0", CultureInfo.CurrentCulture) : "—";

}