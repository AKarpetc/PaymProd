using ClosedXML.Excel;
using Microsoft.Win32;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PaymProdNet9.Pages;

public partial class ReportPage : Page
{
    private ObservableCollection<MenuDel_act> _menuDelicates;
    private List<string> _banquetInfo;
    private readonly List<DelicatesCollForSvod> _summaryData;
    private readonly MenuPrinter _menuPrinter;

    public ObservableCollection<MenuDel_act>? MenuDelicates { get; set; }
    public List<string>? BanquetInfo { get; set; }

    public ReportPage()
    {
        InitializeComponent();

        _menuDelicates = new ObservableCollection<MenuDel_act>();
        _banquetInfo = new List<string>();
        _summaryData = new List<DelicatesCollForSvod>();
        _menuPrinter = new MenuPrinter();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // If data was set from navigation
        if (MenuDelicates != null) _menuDelicates = MenuDelicates;
        if (BanquetInfo != null) _banquetInfo = BanquetInfo;

        GenerateReport();
        GenerateSummaryData();
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
                        : Math.Round(component.Ves * delicate.Countpor / component.Fass, 2)
                };
                summaryData.Add(item);
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
                    var measureUnit = product.Fass > 0 && !string.IsNullOrEmpty(product.FassIz)
                        ? product.FassIz
                        : !string.IsNullOrEmpty(product.Mera)
                            ? product.Mera
                            : "шт";

                    var totalValue = (double)product.TotalWeight;

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
                    var roundingPrecision = 2;
                    var measure = measures.FirstOrDefault(m =>
                        m.Name.ToLower().Trim() == measureUnit.ToLower().Trim() ||
                        measureUnit.ToLower().Contains(m.Name.ToLower()) ||
                        m.Name.ToLower().Contains(measureUnit.ToLower()));
                    if (measure != null) roundingPrecision = measure.RoundingPrecision;

                    // Округляем вверх
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

                    string formattedValue;
                    if (roundingPrecision == 0)
                        formattedValue = $"{(int)roundedValue}{measureUnit}";
                    else
                        formattedValue = $"{roundedValue.ToString($"F{roundingPrecision}")}{measureUnit}";

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
    /// Генерация сводных данных
    /// </summary>
    private void GenerateSummaryData()
    {
        try
        {
            _summaryData.Clear();

            foreach (var delicate in _menuDelicates.Where(d => d.Lcomp != null && d.Lcomp.Any()))
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
                        : Math.Round(component.Ves * delicate.Countpor / component.Fass, 2)
                };

                _summaryData.Add(item);
            }

            SummaryDataGrid.ItemsSource = _summaryData;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при генерации сводных данных: {ex.Message}",
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
            _menuPrinter.PrintReport(_summaryData,
                $"{_banquetInfo[0]}, {_banquetInfo[1]} человек, {_banquetInfo[2]}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании документа: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Экспорт в Excel
    /// </summary>
    private void ExportToExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Отчет");

                // Заголовок
                worksheet.Cell(1, 1).Value = $"Банкет: {_banquetInfo[0]}";
                worksheet.Cell(2, 1).Value = $"Количество гостей: {_banquetInfo[1]}";
                worksheet.Cell(3, 1).Value = $"Дата: {_banquetInfo[2]}";

                // Заголовки колонок
                var headers = new[]
                {
                    "Блюдо", "Количество", "Продукт", "Тип", "Вес", "Мера",
                    "Фасовка", "Мера фасовки", "Сумма продукта в нат ед", "Сумма продукта"
                };

                for (var i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(5, i + 1).Value = headers[i];
                    worksheet.Cell(5, i + 1).Style.Font.Bold = true;
                }

                // Данные
                var row = 6;
                foreach (var item in _summaryData)
                {
                    worksheet.Cell(row, 1).Value = item.Del;
                    worksheet.Cell(row, 2).Value = (double)item.Countpor;
                    worksheet.Cell(row, 3).Value = item.Name;
                    worksheet.Cell(row, 4).Value = item.Type;
                    worksheet.Cell(row, 5).Value = (double)item.Ves;
                    worksheet.Cell(row, 6).Value = item.Mera;
                    worksheet.Cell(row, 7).Value = (double)item.Fass;
                    worksheet.Cell(row, 8).Value = item.FassIz;
                    worksheet.Cell(row, 9).Value = (double)item.Itog;
                    worksheet.Cell(row, 10).Value = (double)item.ItogFass;
                    row++;
                }

                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(dialog.FileName);

                MessageBox.Show("Файл успешно сохранен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}",
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
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)ReportDocument).DocumentPaginator,
                    "Отчет по меню");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при печати: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}