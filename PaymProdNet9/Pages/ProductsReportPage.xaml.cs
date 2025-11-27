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
    /// Генерация отчета по продуктам
    /// </summary>
    private void GenerateReport(bool includePrices)
    {
        try
        {
            ReportDocument.Blocks.Clear();

            var headerParagraph = new Paragraph();
            headerParagraph.Inlines.Add(new Run("Отчет по продуктам")
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
                var (amountText, unitText) = FormatAmount(product, measures);
                var priceText = FormatPrice(product.TotalPrice);

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

        var lower = measureUnit.ToLower().Trim();

        var exact = measures.FirstOrDefault(m =>
            m.Name.Equals(measureUnit, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        return measures.FirstOrDefault(m =>
            lower.Contains(m.Name.ToLower().Trim()) ||
            m.Name.ToLower().Trim().Contains(lower));
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
            var totalWeight = component.Ves * delicate.Countpor;

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
                var priceInfo = _menuPriceService.GetComponentPriceInfo(MenuId, component, delicate.Countpor);
                item.TotalPrice = priceInfo.TotalPrice;
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

    private (string amount, string unit) FormatAmount(GroupedProduct product, List<Measure> measures)
    {
        var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
        
        // Проверяем flag - если единица фасовки отличается от единицы веса
        var hasDifferentPackageUnit = !string.IsNullOrEmpty(product.FassIz) && 
                                       product.FassIz != product.Mera;

        if (product.Fass > 0)
        {
            var packages = product.TotalPackages > 0
                ? product.TotalPackages
                : (product.Fass == 0 ? 0 : product.TotalWeight / product.Fass);

            var packageUnit = !string.IsNullOrEmpty(product.FassIz) ? product.FassIz : defaultUnit;
            var packagePrecision = 0;
            var packageMeasure = FindMeasure(measures, packageUnit);
            if (packageMeasure != null) packagePrecision = packageMeasure.RoundingPrecision;

            // Логика из старого приложения: если количество < 1 и flag != 1, 
            // конвертируем в граммы (умножаем на 1000)
            if (packages < 1 && !hasDifferentPackageUnit)
            {
                // Конвертируем в граммы
                var gramsValue = packages * 1000;
                var gramsMeasure = FindMeasure(measures, defaultUnit);
                var gramsPrecision = gramsMeasure?.RoundingPrecision ?? 2;
                
                double roundedGrams;
                if (gramsPrecision == 0)
                {
                    roundedGrams = Math.Ceiling((double)gramsValue);
                }
                else
                {
                    var multiplier = Math.Pow(10, gramsPrecision);
                    roundedGrams = Math.Ceiling((double)gramsValue * multiplier) / multiplier;
                }
                
                var formattedGrams = gramsPrecision == 0
                    ? ((int)roundedGrams).ToString()
                    : roundedGrams.ToString($"F{gramsPrecision}");
                    
                return (formattedGrams, defaultUnit);
            }

            double roundedPackages;
            if (packagePrecision == 0)
            {
                // Для flag == 1 (hasDifferentPackageUnit) округляем вверх до целого
                roundedPackages = Math.Ceiling((double)packages);
            }
            else
            {
                var multiplier = Math.Pow(10, packagePrecision);
                roundedPackages = Math.Ceiling((double)packages * multiplier) / multiplier;
            }

            var formattedPackages = packagePrecision == 0
                ? ((int)roundedPackages).ToString()
                : roundedPackages.ToString($"F{packagePrecision}");

            return (formattedPackages, packageUnit);
        }

        var totalValue = (double)product.TotalWeight;
        var roundingPrecision = 2;
        var measure = FindMeasure(measures, defaultUnit);
        if (measure != null) roundingPrecision = measure.RoundingPrecision;

        // Логика из старого приложения: если количество < 1, конвертируем в граммы
        if (totalValue < 1)
        {
            var gramsValue = totalValue * 1000;
            var gramsPrecision = measure?.RoundingPrecision ?? 2;
            
            double roundedGrams;
            if (gramsPrecision == 0)
            {
                roundedGrams = Math.Ceiling(gramsValue);
            }
            else
            {
                var multiplier = Math.Pow(10, gramsPrecision);
                roundedGrams = Math.Ceiling(gramsValue * multiplier) / multiplier;
            }
            
            var formattedGrams = gramsPrecision == 0
                ? ((int)roundedGrams).ToString()
                : roundedGrams.ToString($"F{gramsPrecision}");
                
            return (formattedGrams, defaultUnit);
        }

        double roundedValue;
        if (roundingPrecision == 0)
        {
            roundedValue = Math.Ceiling(totalValue);
        }
        else
        {
            var multiplier = Math.Pow(10, roundingPrecision);
            roundedValue = Math.Ceiling(totalValue * multiplier) / multiplier;
        }

        var formattedNumber = roundingPrecision == 0
            ? ((int)roundedValue).ToString()
            : roundedValue.ToString($"F{roundingPrecision}");

        return (formattedNumber, defaultUnit);
    }

    private string FormatPrice(decimal value) =>
        value > 0 ? value.ToString("N0", CultureInfo.CurrentCulture) : "—";

}