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
            // Заголовок
            HeaderParagraph.Inlines.Add(new Run(
                $"Банкет: {_banquetInfo[0]}\n" +
                $"Начало: {_banquetInfo[2]}\n" +
                $"Количество гостей: {_banquetInfo[1]} человек"));

            // Генерируем сводные данные для группировки
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

            // Получаем типы продуктов для сортировки
            var productRepository = new ProductRepository();
            var productTypes = productRepository.GetProductTypes();
            var productTypesDict = productTypes.ToDictionary(pt => pt.Name, pt => pt.SortOrder);

            // Группируем по типам продуктов и сортируем по SortOrder
            var groupedByType = summaryData
                .GroupBy(r => r.Type ?? "Без типа")
                .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                .ThenBy(g => g.Key);

            var rowGroup = new TableRowGroup();
            
            foreach (var group in groupedByType)
            {
                // Заголовок группы (тип продукта)
                var headerRow = new TableRow();
                var headerCell = new TableCell(new Paragraph(new Run(group.Key) 
                    { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = 2,
                    TextAlignment = TextAlignment.Center,
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };
                headerRow.Cells.Add(headerCell);
                rowGroup.Rows.Add(headerRow);

                // Продукты в группе
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
                    var row = new TableRow();
                    
                    // Название продукта
                    var nameCell = new TableCell(new Paragraph(new Run(product.Name)))
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };
                    row.Cells.Add(nameCell);

                    // Количество с единицей измерения
                    string measureUnit = product.Fass > 0 && !string.IsNullOrEmpty(product.FassIz) 
                        ? product.FassIz 
                        : (!string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт");
                    
                    double totalValue = (double)product.TotalWeight;
                    
                    // Конвертируем граммы в килограммы, если нужно
                    if (product.Fass > 0 && 
                        !string.IsNullOrEmpty(product.Mera) && 
                        !string.IsNullOrEmpty(product.FassIz) &&
                        (product.Mera.ToLower().Contains("г") || product.Mera.ToLower().Contains("грамм")) && 
                        (product.FassIz.ToLower().Contains("кг") || product.FassIz.ToLower().Contains("kg")))
                    {
                        totalValue = totalValue / 1000.0;
                        measureUnit = "кг";
                    }
                    
                    // Получаем точность округления
                    var measures = productRepository.GetMeasures();
                    int roundingPrecision = 2;
                    var measure = measures.FirstOrDefault(m => 
                        m.Name.ToLower().Trim() == measureUnit.ToLower().Trim() ||
                        measureUnit.ToLower().Contains(m.Name.ToLower()) ||
                        m.Name.ToLower().Contains(measureUnit.ToLower()));
                    if (measure != null)
                    {
                        roundingPrecision = measure.RoundingPrecision;
                    }
                    
                    // Округляем вверх
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
                    
                    string formattedValue;
                    if (roundingPrecision == 0)
                    {
                        formattedValue = $"{(int)roundedValue}{measureUnit}";
                    }
                    else
                    {
                        formattedValue = $"{roundedValue.ToString($"F{roundingPrecision}")}{measureUnit}";
                    }
                    
                    var countCell = new TableCell(new Paragraph(new Run(formattedValue)))
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5),
                        TextAlignment = TextAlignment.Center
                    };
                    row.Cells.Add(countCell);

                    rowGroup.Rows.Add(row);
                }
            }

            ReportTable.RowGroups.Add(rowGroup);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при генерации отчета: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

