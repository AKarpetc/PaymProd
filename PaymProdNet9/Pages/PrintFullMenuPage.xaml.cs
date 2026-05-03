using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PaymProdNet9.Pages;

public partial class PrintFullMenuPage : Page
{
    private readonly bool _showCost;
    private readonly bool _showPrice;
    private readonly List<int> _selectedCategoryIds;
    
    private readonly MenuPrinter _menuPrinter;
    private readonly DelicateRepository _delicateRepository;
    private readonly MenuPriceService _menuPriceService;
    
    private List<DelicatesColl> _filteredDelicates = new();

    public PrintFullMenuPage(bool showCost, bool showPrice, List<int> selectedCategoryIds)
    {
        InitializeComponent();
        
        _showCost = showCost;
        _showPrice = showPrice;
        _selectedCategoryIds = selectedCategoryIds;
        
        _menuPrinter = new MenuPrinter();
        _delicateRepository = new DelicateRepository();
        _menuPriceService = new MenuPriceService();
        
        this.SizeChanged += Page_SizeChanged;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        GenerateReport();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DocumentViewer.Document is FlowDocument doc)
        {
            if (DocumentViewer.ActualWidth < 1000)
            {
                doc.PageWidth = 1000;
            }
            else
            {
                doc.PageWidth = double.NaN;
            }
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        NavigationService?.GoBack();
    }

    private void GenerateReport()
    {
        try
        {
            var allDelicates = _delicateRepository.GetAvailableDelicatesForMenu(null);
            _filteredDelicates = allDelicates
                .Where(d => d.IDType.HasValue && _selectedCategoryIds.Contains(d.IDType.Value))
                .ToList();

            if (_filteredDelicates.Count == 0)
            {
                DocumentViewer.Document = new FlowDocument(new Paragraph(new Run("Нет блюд для отображения в выбранных категориях.")));
                SaveToWordButton.Visibility = Visibility.Collapsed;
                return;
            }

            BuildDocument();
            SaveToWordButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при генерации отчета по всему меню", ex);
            DocumentViewer.Document = new FlowDocument(new Paragraph(new Run($"Ошибка при генерации отчета: {ex.Message}")));
        }
    }

    private void BuildDocument()
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(5),
            ColumnWidth = double.PositiveInfinity
        };

        var titleTable = new Table { CellSpacing = 0, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 0, 10) };
        titleTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var titleRowGroup = new TableRowGroup();
        var titleRow = new TableRow();
        var titleCell = new TableCell(new Paragraph(new Run("МЕНЮ") { FontSize = 22, FontWeight = FontWeights.Bold }) { TextAlignment = TextAlignment.Center })
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
        titleRow.Cells.Add(titleCell);
        titleRowGroup.Rows.Add(titleRow);
        titleTable.RowGroups.Add(titleRowGroup);
        document.Blocks.Add(titleTable);

        var groupedDelicates = _filteredDelicates
            .Where(d => d.Lcomp != null && d.Lcomp.Any())
            .GroupBy(d => new { d.Type, d.TypeSortOrder })
            .OrderBy(g => g.Key.TypeSortOrder)
            .ThenBy(g => g.Key.Type);

        var table = new Table();
        
        table.Columns.Add(new TableColumn { Width = new GridLength(2.0, GridUnitType.Star) }); // Блюдо
        table.Columns.Add(new TableColumn { Width = new GridLength(4.0, GridUnitType.Star) }); // Состав
        
        if (_showCost)
            table.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
        if (_showPrice)
            table.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });

        var rowGroup = new TableRowGroup();

        foreach (var group in groupedDelicates)
        {
            var headerRow = new TableRow();
            int span = 2 + (_showCost ? 1 : 0) + (_showPrice ? 1 : 0);
            
            var headerCell = new TableCell(new Paragraph(new Run(group.Key.Type ?? "Без типа")
            {
                FontWeight = FontWeights.Bold
            }))
            {
                ColumnSpan = span,
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
            
            if (_showCost) columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Себестоимость"));
            if (_showPrice) columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Цена"));
            
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

                var compositionBlock = BuildCompositionTable(delicate, out var dishCost);
                
                var compositionCell = new TableCell(compositionBlock)
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Left
                };
                row.Cells.Add(compositionCell);

                if (_showCost)
                {
                    var costText = dishCost > 0 ? FormatCurrency(dishCost) : "—";
                    var costCell = new TableCell(new Paragraph(new Run(costText)))
                    {
                        Padding = new Thickness(4),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        TextAlignment = TextAlignment.Right
                    };
                    row.Cells.Add(costCell);
                }
                
                if (_showPrice)
                {
                    var price = dishCost;
                    if (delicate.DefaultMarkup > 0)
                        price = dishCost * (delicate.DefaultMarkup / 100);
                        
                    var priceText = price > 0 ? FormatCurrency(price) : "—";
                    var priceCell = new TableCell(new Paragraph(new Run(priceText)))
                    {
                        Padding = new Thickness(4),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        TextAlignment = TextAlignment.Right
                    };
                    row.Cells.Add(priceCell);
                }

                rowGroup.Rows.Add(row);
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

    private static string ShortenUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
        var trimmed = unit.Trim();
        return trimmed.Length > 2 ? trimmed.Substring(0, 2) : trimmed;
    }

    private Block BuildCompositionTable(DelicatesColl delicate, out decimal dishCost)
    {
        var items = GetCompositionItems(delicate, out dishCost);

        if (items.Count == 0)
        {
            return new Paragraph(new Run("Без состава"));
        }

        var simpleLines = items.Select(i => $"{i.Name} ({i.Weight})").ToList();
        return new Paragraph(new Run(string.Join(", ", simpleLines)));
    }

    private struct CompositionItem
    {
        public string Name;
        public string Weight;
        public decimal TotalPrice;
    }

    private List<CompositionItem> GetCompositionItems(DelicatesColl delicate, out decimal dishCost)
    {
        var result = new List<CompositionItem>();
        dishCost = 0;
        if (delicate.Lcomp == null || !delicate.Lcomp.Any()) return result;

        foreach (var component in delicate.Lcomp)
        {
            var productName = !string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name;
            var baseUnit = !string.IsNullOrWhiteSpace(component.Mera) ? component.Mera : "г";
            var totalWeight = component.Ves; // 1 порция

            string NormalizeUnitLocal(string unit) => unit?.Trim().ToLowerInvariant() ?? string.Empty;
            var baseUnitNormalized = NormalizeUnitLocal(baseUnit);
            var fassIzNormalized = NormalizeUnitLocal(component.FassIz ?? string.Empty);

            decimal displayValue;
            string displayUnit;

            if (component.DoNotConvertToPackInMenu)
            {
                displayValue = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
                displayUnit = baseUnit;
            }
            else
            {
                if (component.Fass > 0 && !string.IsNullOrWhiteSpace(component.FassIz) &&
                    fassIzNormalized != baseUnitNormalized && totalWeight >= component.Fass)
                {
                    var packageCount = totalWeight / component.Fass;
                    displayValue = Math.Round(packageCount, 2, MidpointRounding.AwayFromZero);
                    displayUnit = !string.IsNullOrWhiteSpace(component.FassIz) ? component.FassIz : baseUnit;
                }
                else
                {
                    displayValue = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
                    displayUnit = baseUnit;
                }
            }

            displayUnit = ShortenUnit(displayUnit);
            var formattedWeight = FormatValueOld(displayValue, displayUnit);

            var priceInfo = _menuPriceService.GetComponentPriceInfo(0, component, 1);
            dishCost += priceInfo.TotalPrice;

            result.Add(new CompositionItem
            {
                Name = productName,
                Weight = formattedWeight,
                TotalPrice = priceInfo.TotalPrice
            });
        }

        return result;
    }

    private static string FormatValueOld(decimal value, string unit)
    {
        if (value == Math.Truncate(value))
            return $"{(int)value}{unit}";
        return $"{value:F2}{unit}";
    }

    private string FormatCurrency(decimal value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.CurrentCulture);
    }

    private void SaveToWord_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _menuPrinter.PrintCustomFullMenu(_filteredDelicates, _showCost, _showPrice);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении отчета по всему меню", ex);
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
