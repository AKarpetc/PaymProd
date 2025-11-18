using ClosedXML.Excel;
using Microsoft.Win32;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PaymProdNet9.Windows;

public partial class ReportWindow : Window
{
    private readonly ObservableCollection<MenuDel_act> _menuDelicates;
    private readonly List<string> _banquetInfo;
    private readonly List<DelicatesCollForSvod> _summaryData;
    private readonly MenuPrinter _menuPrinter;

    public ReportWindow(ObservableCollection<MenuDel_act> menuDelicates, List<string> banquetInfo)
    {
        InitializeComponent();

        _menuDelicates = menuDelicates;
        _banquetInfo = banquetInfo;
        _summaryData = new List<DelicatesCollForSvod>();
        _menuPrinter = new MenuPrinter();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        GenerateReport();
        GenerateSummaryData();
    }

    /// <summary>
    /// Генерация отчета
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

            // Группируем блюда по типам
            var groupedDelicates = _menuDelicates
                .Where(d => d.Lcomp != null && d.Lcomp.Any())
                .GroupBy(d => d.Lcomp.FirstOrDefault()?.Type ?? "Без типа")
                .OrderBy(g => g.Key);

            var rowGroup = new TableRowGroup();

            foreach (var group in groupedDelicates)
            {
                // Заголовок группы
                var headerRow = new TableRow();
                var headerCell = new TableCell(new Paragraph(new Run(group.Key)
                    { FontWeight = FontWeights.Bold }))
                {
                    ColumnSpan = 3,
                    TextAlignment = TextAlignment.Center,
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };
                headerRow.Cells.Add(headerCell);
                rowGroup.Rows.Add(headerRow);

                // Блюда
                foreach (var delicate in group)
                {
                    var row = new TableRow();

                    // Название блюда
                    var nameCell = new TableCell(new Paragraph(new Run(delicate.Del)))
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };
                    row.Cells.Add(nameCell);

                    // Состав
                    var composition = string.Join(", ", delicate.Lcomp.Select(c => c.NameT));
                    var compositionCell = new TableCell(new Paragraph(new Run(composition)))
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5)
                    };
                    row.Cells.Add(compositionCell);

                    // Количество
                    var countCell = new TableCell(new Paragraph(new Run(delicate.Countpor.ToString())))
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