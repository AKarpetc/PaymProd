using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
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

    public ObservableCollection<MenuDel_act>? MenuDelicates { get; set; }
    public List<string>? BanquetInfo { get; set; }

    public ProductsReportPage()
    {
        InitializeComponent();
        
        _menuDelicates = new ObservableCollection<MenuDel_act>();
        _banquetInfo = new List<string>();
        _menuPrinter = new MenuPrinter();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // If data was set from navigation
        if (MenuDelicates != null) _menuDelicates = MenuDelicates;
        if (BanquetInfo != null) _banquetInfo = BanquetInfo;

        GenerateReport();
    }

    /// <summary>
    /// Генерация отчета по продуктам
    /// </summary>
    private void GenerateReport()
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
                ReportDocument.Blocks.Add(new Paragraph(new Run("Нет данных для отображения.")));
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

            var outerTable = new Table
            {
                CellSpacing = 12,
                TextAlignment = TextAlignment.Left
            };
            outerTable.Columns.Add(new TableColumn { Width = GridLength.Auto });
            outerTable.Columns.Add(new TableColumn { Width = GridLength.Auto });

            var outerGroup = new TableRowGroup();
            outerTable.RowGroups.Add(outerGroup);

            for (int i = 0; i < groupedByType.Count; i += 2)
            {
                var row = new TableRow();
                row.Cells.Add(CreateGroupCell(groupedByType[i], measures, TextAlignment.Right));

                if (i + 1 < groupedByType.Count)
                {
                    row.Cells.Add(CreateGroupCell(groupedByType[i + 1], measures, TextAlignment.Left));
                }
                else
                {
                    row.Cells.Add(new TableCell());
                }

                outerGroup.Rows.Add(row);
            }

            var figure = new Figure
            {
                HorizontalAnchor = FigureHorizontalAnchor.PageCenter,
                WrapDirection = WrapDirection.None,
                Width = new FigureLength(1, FigureUnitType.Content)
            };
            figure.Blocks.Add(outerTable);

            var containerParagraph = new Paragraph();
            containerParagraph.Inlines.Add(figure);
            ReportDocument.Blocks.Add(containerParagraph);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private TableCell CreateGroupCell(IGrouping<string, DelicatesCollForSvod> group, List<Measure> measures, TextAlignment alignment)
    {
        var cell = new TableCell
        {
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = alignment == TextAlignment.Right 
                ? new Thickness(0, 0, 10, 0) 
                : new Thickness(10, 0, 0, 0),
            TextAlignment = alignment
        };

        var innerTable = new Table
        {
            CellSpacing = 0
        };
        var productColumn = new TableColumn { Width = new GridLength(280) };
        var quantityColumn = new TableColumn { Width = new GridLength(70) };
        var unitColumn = new TableColumn { Width = new GridLength(50) };
        innerTable.Columns.Add(productColumn);
        innerTable.Columns.Add(quantityColumn);
        innerTable.Columns.Add(unitColumn);

        var innerGroup = new TableRowGroup();
        innerTable.RowGroups.Add(innerGroup);

        var headerRow = new TableRow();
        var headerCell = new TableCell(new Paragraph(new Run(group.Key) { FontWeight = FontWeights.Bold }))
        {
            ColumnSpan = 3,
            TextAlignment = TextAlignment.Center,
            Background = Brushes.LightGray,
            Padding = new Thickness(4)
        };
        headerRow.Cells.Add(headerCell);
        innerGroup.Rows.Add(headerRow);

        var titlesRow = new TableRow();
        titlesRow.Cells.Add(CreateTitleCell("Продукт"));
        titlesRow.Cells.Add(CreateTitleCell("Количество"));
        titlesRow.Cells.Add(CreateTitleCell("Ед."));
        innerGroup.Rows.Add(titlesRow);

        var groupedProducts = group
            .GroupBy(r => r.NameT ?? r.Name)
            .Select(g => new
            {
                Name = g.Key,
                TotalWeight = g.Sum(r => r.Fass > 0 ? r.ItogFass : r.Itog),
                FassIz = g.First().FassIz ?? g.First().Mera ?? "",
                Mera = g.First().Mera ?? "",
                Fass = g.First().Fass
            })
            .OrderBy(p => p.Name);

        foreach (var product in groupedProducts)
        {
            string measureUnit = product.Fass > 0 && !string.IsNullOrEmpty(product.FassIz) 
                ? product.FassIz 
                : (!string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт");

            double totalValue = (double)product.TotalWeight;

            if (product.Fass > 0 &&
                !string.IsNullOrEmpty(product.Mera) &&
                !string.IsNullOrEmpty(product.FassIz) &&
                (product.Mera.ToLower().Contains("г") || product.Mera.ToLower().Contains("грамм")) &&
                (product.FassIz.ToLower().Contains("кг") || product.FassIz.ToLower().Contains("kg")))
            {
                totalValue /= 1000.0;
                measureUnit = "кг";
            }

            int roundingPrecision = 2;
            var measure = FindMeasure(measures, measureUnit);
            if (measure != null)
            {
                roundingPrecision = measure.RoundingPrecision;
            }

            double roundedValue;
            if (roundingPrecision == 0)
            {
                roundedValue = Math.Ceiling(totalValue);
            }
            else
            {
                double multiplier = Math.Pow(10, roundingPrecision);
                roundedValue = Math.Ceiling(totalValue * multiplier) / multiplier;
            }

            string amountText = roundingPrecision == 0
                ? ((int)roundedValue).ToString()
                : roundedValue.ToString($"F{roundingPrecision}");

            var row = new TableRow();
            row.Cells.Add(CreateValueCell(product.Name));
            row.Cells.Add(CreateValueCell(amountText, TextAlignment.Right));
            row.Cells.Add(CreateValueCell(measureUnit, TextAlignment.Center));
            innerGroup.Rows.Add(row);
        }

        cell.Blocks.Add(innerTable);
        return cell;
    }

    private TableCell CreateTitleCell(string text)
    {
        return new TableCell(new Paragraph(new Run(text) { FontWeight = FontWeights.SemiBold }))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(4),
            TextAlignment = TextAlignment.Center,
            Background = Brushes.AliceBlue
        };
    }

    private TableCell CreateValueCell(string text, TextAlignment alignment = TextAlignment.Left)
    {
        return new TableCell(new Paragraph(new Run(text)))
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(4),
            TextAlignment = alignment
        };
    }

    private Measure? FindMeasure(List<Measure> measures, string measureUnit)
    {
        if (string.IsNullOrWhiteSpace(measureUnit))
            return null;

        var lower = measureUnit.ToLower().Trim();

        var exact = measures.FirstOrDefault(m => m.Name.ToLower().Trim() == lower);
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
            var summaryData = GenerateSummaryData();
            _menuPrinter.PrintReport(summaryData, 
                $"{_banquetInfo[0]}, {_banquetInfo[1]} человек, {_banquetInfo[2]}");
        }
        catch (Exception ex)
        {
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
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)ReportDocument).DocumentPaginator, 
                    "Отчет по продуктам");
            }
        }
        catch (Exception ex)
        {
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
        {
            foreach (var component in delicate.Lcomp)
            {
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
                    Itog = component.Ves * delicate.Countpor,
                    ItogFass = component.Fass == 0 
                        ? component.Ves * delicate.Countpor 
                        : Math.Round((component.Ves * delicate.Countpor) / component.Fass, 2)
                };
                
                summaryData.Add(item);
            }
        }

        return summaryData;
    }
}

