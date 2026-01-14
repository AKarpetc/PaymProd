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
    private List<int> _lastSelectedMenuIds = new();

    // Backups for single menu data
    private ObservableCollection<MenuDel_act> _singleMenuDelicates;
    private List<string> _singleBanquetInfo;
    private int _singleMenuId;

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
        if (MenuDelicates != null)
        {
            _menuDelicates = MenuDelicates;
            // Backup for restoration
            _singleMenuDelicates = new ObservableCollection<MenuDel_act>(MenuDelicates);
        }

        if (BanquetInfo != null)
        {
            _banquetInfo = BanquetInfo;
            // Backup
            _singleBanquetInfo = new List<string>(BanquetInfo);
        }

        _singleMenuId = MenuId;

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
        _currentReportWithPrices = null;
    }

    private void RestoreSingleMenuState()
    {
        if (_singleMenuDelicates != null)
        {
            _menuDelicates.Clear();
            foreach (var item in _singleMenuDelicates) _menuDelicates.Add(item);
        }

        if (_singleBanquetInfo != null)
            _banquetInfo = new List<string>(_singleBanquetInfo);

        MenuId = _singleMenuId;
    }

    private void GenerateReportWithPrices_Click(object sender, RoutedEventArgs e)
    {
        RestoreSingleMenuState();
        GenerateReport(true);
    }

    private void GenerateReportWithoutPrices_Click(object sender, RoutedEventArgs e)
    {
        RestoreSingleMenuState();
        GenerateReport(false);
    }

    // ... GenerateReport ...

    private void GenerateMultiMenuReport_Click(object sender, RoutedEventArgs e)
    {
        var window = new MultiMenuSelectionWindow(_lastSelectedMenuIds);
        if (window.ShowDialog() == true)
        {
            _lastSelectedMenuIds = window.SelectedMenuIds;
            var includePrices = window.IncludePrices;

            LoadMultipleMenus(_lastSelectedMenuIds);
            GenerateReport(includePrices);
        }
    }

    private void LoadMultipleMenus(List<int> menuIds)
    {
        _menuDelicates.Clear();
        var repo = new MenuRepository();
        var menus = new List<Menus>();

        foreach (var id in menuIds)
        {
            var m = repo.GetMenuById(id);
            if (m != null) menus.Add(m);

            var items = repo.GetMenuDelicates(id);
            foreach (var item in items) _menuDelicates.Add(item);
        }

        var names = string.Join(" + ", menus.Select(m => m.Name));
        if (names.Length > 50) names = names.Substring(0, 47) + "...";

        var totalGuests = menus.Sum(m => m.CountP);

        // Update BanquetInfo to reflect multiple menus
        // Format: Name, Guests, Date
        _banquetInfo = new List<string>
        {
            $"Сводный: {names}",
            totalGuests.ToString(),
            DateTime.Now.ToString("dd.MM.yyyy HH:mm")
        };

        // Reset MenuId to ensure we don't rely on a single menu context
        MenuId = 0;
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
                ? date.ToString("dd.MM.yyyy HH:mm")
                : _banquetInfo[2];
            headerParagraph.Inlines.Add(new Run($"Дата, начало: {dateText}"));
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
            var formatter = new ProductReportFormatter(measures);

            var groupedByType = summaryData
                .GroupBy(r => r.Type ?? "Без типа")
                .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                .ThenBy(g => g.Key)
                .ToList();

            if (includePrices)
                BuildSingleColumnTableWithPrices(groupedByType, formatter);
            else
                BuildStandardTable(groupedByType, formatter, summaryData.Count);

            _currentReportWithPrices = includePrices;
            SaveToWordButton.Visibility = Visibility.Visible;
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
        ProductReportFormatter formatter, int totalItems)
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
            var groupRows = CreateTypeSectionRows(group, formatter);
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
        ProductReportFormatter formatter)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(290) });
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        decimal grandTotal = 0;

        foreach (var typeGroup in groupedByType)
        {
            decimal groupTotal = 0;
            var headerRow = new TableRow();
            headerRow.Cells.Add(CreateHeaderCell(typeGroup.Key, 5));
            group.Rows.Add(headerRow);

            var titlesRow = new TableRow();
            titlesRow.Cells.Add(CreateTitleCell("Продукт"));
            titlesRow.Cells.Add(CreateTitleCell("Количество"));
            titlesRow.Cells.Add(CreateTitleCell("Ед."));
            titlesRow.Cells.Add(CreateTitleCell("Цена"));
            titlesRow.Cells.Add(CreateTitleCell("Стоимость"));
            group.Rows.Add(titlesRow);

            var groupedProducts = GetGroupedProducts(typeGroup);

            foreach (var product in groupedProducts)
            {
                var (amountText, unitText, roundedAmount, priceMultiplier) = formatter.FormatAmountWithRoundedValue(product);

                // Пересчитываем цену на основе округленного количества (для итоговой суммы)
                // Note: RecalculatePrice logic might also need adjustment if it relies on exact unit match, 
                // but for now we trust it works for Grand Total sum.
                // We focus on the "Price" column display.
                
                var recalculatedPrice = formatter.RecalculatePrice(product, roundedAmount);
                grandTotal += recalculatedPrice;
                groupTotal += recalculatedPrice;
                
                // Calculate Unit Price for display based on the Displayed Unit
                var displayUnitPrice = product.Price * priceMultiplier;
                // If price is 0 (e.g. calculated for dish), try to derive from TotalPrice if possible? 
                // No, product.Price should be populated correctly now. 
                // If it is 0, FormatPrice returns "-".
                
                var priceText = FormatPrice(grandTotal > 0 ? recalculatedPrice : 0); // Total Cost column

                var dataRow = new TableRow();
                dataRow.Cells.Add(CreateValueCell(product.Name));
                dataRow.Cells.Add(CreateValueCell(amountText, TextAlignment.Right));
                dataRow.Cells.Add(CreateValueCell(unitText, TextAlignment.Center));
                dataRow.Cells.Add(CreateValueCell(FormatPrice(displayUnitPrice), TextAlignment.Right));
                dataRow.Cells.Add(CreateValueCell(priceText, TextAlignment.Right));
                group.Rows.Add(dataRow);
            }

            // Group Subtotal Row
            var groupSubtotalRow = new TableRow();
            var groupTitleCell = new TableCell(new Paragraph(new Run($"Итог по категории \"{typeGroup.Key}\"") { FontWeight = FontWeights.Bold }))
            {
                ColumnSpan = 4,
                TextAlignment = TextAlignment.Right,
                Background = Brushes.WhiteSmoke, // Light gray for group totals
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4)
            };
            groupSubtotalRow.Cells.Add(groupTitleCell);

            var groupValueCell = new TableCell(new Paragraph(new Run(FormatPrice(groupTotal)) { FontWeight = FontWeights.Bold }))
            {
                TextAlignment = TextAlignment.Right,
                 Background = Brushes.WhiteSmoke,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4)
            };
            groupSubtotalRow.Cells.Add(groupValueCell);
            group.Rows.Add(groupSubtotalRow);
        }

        // Add Grand Total Row
        var totalRow = new TableRow();
        // Remove duplicate line
        // "ИТОГО" usually right aligned? CreateHeaderCell is centered.
        // Let's customize it or reuse logic. 
        // CreateHeaderCell uses colspan, bold, centered, gray background.
        // Maybe better to create custom cells for total to align right.

        var totalTitleCell = new TableCell(new Paragraph(new Run("ИТОГО") { FontWeight = FontWeights.Bold }))
        {
            ColumnSpan = 4,
            TextAlignment = TextAlignment.Right,
            Background = new SolidColorBrush(Color.FromRgb(221, 235, 247)), // DDEBF7 matches title cell
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var totalValueCell =
            new TableCell(new Paragraph(new Run(FormatPrice(grandTotal)) { FontWeight = FontWeights.Bold }))
            {
                TextAlignment = TextAlignment.Right,
                Background = new SolidColorBrush(Color.FromRgb(221, 235, 247)),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4)
            };

        totalRow.Cells.Add(totalTitleCell);
        totalRow.Cells.Add(totalValueCell);
        group.Rows.Add(totalRow);

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
        ProductReportFormatter formatter)
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
            var (amountText, unitText, _) = formatter.FormatAmount(product);

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
                _currentReportWithPrices.Value);
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

        var items = _menuDelicates.Where(d => d.Lcomp != null && d.Lcomp.Any() && !d.HideInProductReport);

        // Если включена галочка "Только продукты из меню", скрываем те, у которых HideInMenu = true
        if (OnlyMenuProductsCheckBox.IsChecked == true)
        {
            items = items.Where(d => !d.HideInMenu);
        }

        foreach (var delicate in items)
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

            if (delicate.Idmen > 0 || MenuId > 0)
            {
                var priceMenuId = delicate.Idmen > 0 ? delicate.Idmen : MenuId;

                // Для продуктов, добавленных напрямую (отрицательный Del_id), рассчитываем цену на основе итогового количества
                if (delicate.Del_id < 0)
                {
                    // Получаем цену за единицу и умножаем на итоговое количество
                    var unitPrice = _menuPriceService.GetUnitPrice(priceMenuId, component.Prodid);
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
                    item.Price = unitPrice;
                }
                else
                {
                    // Для компонентов блюд используем стандартный расчет
                    var priceInfo = _menuPriceService.GetComponentPriceInfo(priceMenuId, component, dishCountForPrice);
                    item.Price = priceInfo.UnitPrice;
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
                Price = g.First().Price,
                TotalPrice = g.Sum(r => r.TotalPrice)
            })
            .OrderBy(p => p.Name);
    }

    


    private string FormatPrice(decimal value)
    {
        return value > 0 ? value.ToString("N0", CultureInfo.CurrentCulture) : "—";
    }
}