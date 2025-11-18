using ClosedXML.Excel;
using Microsoft.Win32;
using PaymProdNet9.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PaymProdNet9.Pages;

public partial class SummaryTablePage : Page
{
    private ObservableCollection<MenuDel_act> _menuDelicates;
    private List<string> _banquetInfo;
    private readonly List<DelicatesCollForSvod> _summaryData;

    public ObservableCollection<MenuDel_act>? MenuDelicates { get; set; }
    public List<string>? BanquetInfo { get; set; }

    public SummaryTablePage()
    {
        InitializeComponent();

        _menuDelicates = new ObservableCollection<MenuDel_act>();
        _banquetInfo = new List<string>();
        _summaryData = new List<DelicatesCollForSvod>();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // If data was set from navigation
        if (MenuDelicates != null) _menuDelicates = MenuDelicates;
        if (BanquetInfo != null) _banquetInfo = BanquetInfo;

        GenerateSummaryData();
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
                DefaultExt = "xlsx",
                FileName = $"Сводная_таблица_{_banquetInfo[0]}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Сводная таблица");

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
}