using PaymProdNet9.Models;
using PaymProdNet9.Services;
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

    public PrintMenuPage()
    {
        InitializeComponent();
        _menuPrinter = new MenuPrinter();
        _menuPriceService = new MenuPriceService();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BuildDocument();
    }

    private void BuildDocument()
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
        table.Columns.Add(new TableColumn { Width = new GridLength(500) });
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });

        var rowGroup = new TableRowGroup();

        foreach (var group in groupedDelicates)
        {
            var headerRow = new TableRow();
            var headerCell = new TableCell(new Paragraph(new Run(group.Key.Type ?? "Без типа")
            {
                FontWeight = FontWeights.Bold
            }))
            {
                ColumnSpan = 3,
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
            columnsHeaderRow.Cells.Add(CreateColumnHeaderCell("Цена, тг"));
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

                var compositionParagraph = BuildCompositionParagraph(delicate, out var dishPrice);
                var compositionCell = new TableCell(compositionParagraph)
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Left
                };
                row.Cells.Add(compositionCell);

                var priceCell = new TableCell(new Paragraph(new Run(dishPrice > 0 ? FormatCurrency(dishPrice) : "—")))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Right
                };
                row.Cells.Add(priceCell);

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

    private Paragraph BuildCompositionParagraph(DelicatesColl delicate, out decimal dishTotal)
    {
        var paragraph = new Paragraph();
        var lines = BuildCompositionLines(delicate, out dishTotal);

        if (lines.Count == 0)
        {
            paragraph.Inlines.Add(new Run("Без состава"));
            return paragraph;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i]));
        }

        return paragraph;
    }

    private List<string> BuildCompositionLines(DelicatesColl delicate, out decimal dishTotal)
    {
        var lines = new List<string>();
        dishTotal = 0;
        if (delicate.Lcomp == null || !delicate.Lcomp.Any()) return lines;

        foreach (var component in delicate.Lcomp)
        {
            var productName = !string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name;
            var baseUnit = !string.IsNullOrWhiteSpace(component.Mera) ? component.Mera : "г";
            var totalWeight = component.Ves * (delicate.Count > 0 ? delicate.Count : 1);
            var formattedWeight = FormatValue(totalWeight, baseUnit);

            var priceInfo = _menuPriceService.GetComponentPriceInfo(MenuId, component, delicate.Count);
            dishTotal += priceInfo.TotalPrice;

            if (component.Fass > 0)
            {
                var packageUnit = !string.IsNullOrWhiteSpace(component.FassIz) ? component.FassIz : baseUnit;
                var packageCount = component.Fass == 0 ? 0 : (component.Ves * delicate.Count) / component.Fass;
                if (packageCount >= 1)
                {
                    formattedWeight = FormatValue(packageCount, packageUnit);
                }
            }

            string line = priceInfo.TotalPrice > 0
                ? $"{productName} ({formattedWeight}) — {FormatCurrency(priceInfo.TotalPrice)} тг"
                : $"{productName} ({formattedWeight}) — цена не указана";

            lines.Add(line);
        }
        return lines;
    }

    private string FormatCurrency(decimal value) =>
        Math.Round(value, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.CurrentCulture);

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
            var menuTitle = BanquetInfo.Count >= 3
                ? $"{BanquetInfo[0]}, {BanquetInfo[1]} человек, {BanquetInfo[2]}"
                : "Меню";

            _menuPrinter.PrintMenu(Delicates, menuTitle);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveToWordWithPrices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Delicates.Count == 0)
            {
                MessageBox.Show("Нет данных для сохранения.", "Информация", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var menuTitle = BanquetInfo.Count >= 3
                ? $"{BanquetInfo[0]}, {BanquetInfo[1]} человек, {BanquetInfo[2]}"
                : "Меню";

            _menuPrinter.PrintMenu(Delicates, menuTitle, includePrices: true, menuId: MenuId > 0 ? MenuId : null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

