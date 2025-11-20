using System.Diagnostics;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PaymProdNet9.Data;
using PaymProdNet9.Models;

namespace PaymProdNet9.Services;

/// <summary>
///     Класс для печати меню в формате Word
/// </summary>
public class MenuPrinter
{
    private readonly ProductRepository _productRepository;

    public MenuPrinter()
    {
        _productRepository = new ProductRepository();
    }

    /// <summary>
    ///     Печать меню
    /// </summary>
    public void PrintMenu(List<DelicatesColl> delicates, string menuName)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            // Группируем блюда по типам и сортируем по SortOrder
            var groupedDelicates = delicates
                .Where(d => d.Lcomp != null && d.Lcomp.Any())
                .GroupBy(d => new { d.Type, SortOrder = d.TypeSortOrder })
                .OrderBy(g => g.Key.SortOrder)
                .ThenBy(g => g.Key.Type);

            // Создаем документ
            using (var document = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                var titleParagraph = body.AppendChild(new Paragraph());
                var titleRun = titleParagraph.AppendChild(new Run());
                var titleRunProperties = titleRun.AppendChild(new RunProperties());
                titleRunProperties.AppendChild(new Bold());
                titleRunProperties.AppendChild(new FontSize { Val = "32" });
                titleRun.AppendChild(new Text($"Меню: {menuName}"));

                var titleProperties = titleParagraph.AppendChild(new ParagraphProperties());
                titleProperties.AppendChild(new Justification { Val = JustificationValues.Center });

                body.AppendChild(new Paragraph()); // Пустая строка

                // Создаем таблицу
                var table = new Table();

                // Свойства таблицы
                var tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    )
                );
                table.AppendChild(tableProperties);

                foreach (var group in groupedDelicates)
                {
                    // Заголовок группы (тип блюда)
                    var headerRow = new TableRow();
                    var headerCell = new TableCell();
                    var headerCellProperties = new TableCellProperties(
                        new GridSpan { Val = 2 },
                        new Shading { Fill = "D3D3D3" }
                    );
                    headerCell.Append(headerCellProperties);

                    var headerParagraph = new Paragraph();
                    var headerRun = new Run();
                    var headerRunProps = new RunProperties(new Bold(), new FontSize { Val = "28" });
                    headerRun.Append(headerRunProps);
                    headerRun.Append(new Text(group.Key.Type ?? "Без типа"));
                    headerParagraph.Append(headerRun);
                    headerCell.Append(headerParagraph);
                    headerRow.Append(headerCell);
                    table.Append(headerRow);

                    // Блюда в группе
                    foreach (var delicate in group)
                    {
                        var row = new TableRow();

                        // Название блюда
                        var nameCell = new TableCell();
                        nameCell.Append(new Paragraph(new Run(new Text(delicate.Name))));
                        row.Append(nameCell);

                        // Состав
                        var composition = "Состав: ";
                        foreach (var component in delicate.Lcomp)
                        {
                            var fass = component.Fass;
                            var fassIzmer = component.FassIz;
                            var ves = component.Ves * delicate.Count;

                            // Используем NameT (название продукта) вместо Name
                            var productName = !string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name;

                            if (fass > 0)
                            {
                                var fassSumm = Math.Round(ves / fass, 2);
                                if (fassSumm < 1)
                                {
                                    fassSumm = ves;
                                    fassIzmer = component.Mera;
                                }

                                composition += $"{productName}({fassSumm}{fassIzmer}), ";
                            }
                            else
                            {
                                composition += $"{productName}({ves}{component.Mera}), ";
                            }
                        }

                        if (composition.EndsWith(", "))
                            composition = composition.Substring(0, composition.Length - 2);

                        var compositionCell = new TableCell();
                        compositionCell.Append(new Paragraph(new Run(new Text(composition))));
                        row.Append(compositionCell);

                        table.Append(row);
                    }
                }

                body.Append(table);
                mainPart.Document.Save();
            }

            // Открываем файл
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при создании документа: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Печать отчета с продуктами
    /// </summary>
    public void PrintReport(List<DelicatesCollForSvod> reportData, string menuName)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            // Получаем все меры для определения округления
            var measures = _productRepository.GetMeasures();
            var measuresDict = measures.ToDictionary(m => m.Name.ToLower().Trim(), m => m);

            // Функция для поиска меры по имени (с учетом вариаций)
            Measure? FindMeasure(string? measureName)
            {
                if (string.IsNullOrEmpty(measureName)) return null;

                var lowerName = measureName.ToLower().Trim();

                // Прямое совпадение
                if (measuresDict.ContainsKey(lowerName))
                    return measuresDict[lowerName];

                // Поиск по частичному совпадению
                foreach (var measure in measures)
                    if (lowerName.Contains(measure.Name.ToLower()) || measure.Name.ToLower().Contains(lowerName))
                        return measure;

                return null;
            }

            // Получаем типы продуктов для сортировки
            var productTypes = _productRepository.GetProductTypes();
            var productTypesDict = productTypes.ToDictionary(pt => pt.Name, pt => pt.SortOrder);

            using (var document = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var titleParagraph = body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new Bold(), new FontSize { Val = "32" }),
                        new Text("Отчет по продуктам"))
                ));

                var infoParagraph = body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                ));
                infoParagraph.AppendChild(new Run(new Text($"Банкет: {menuName}")));
                infoParagraph.AppendChild(new Break());
                var dateRun = new Run();
                dateRun.AppendChild(new Text($"Дата: {DateTime.Now:dd.MM.yyyy}"));
                infoParagraph.AppendChild(dateRun);

                body.AppendChild(new Paragraph()); // Пустая строка

                var groupedList = reportData
                    .GroupBy(r => r.Type ?? "Без типа")
                    .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                    .ThenBy(g => g.Key)
                    .ToList();

                var table = new Table(
                    new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "9000" },
                        new TableJustification { Val = TableRowAlignmentValues.Center },
                        new TableBorders(
                            new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new InsideHorizontalBorder
                                { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new InsideVerticalBorder
                                { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                        )
                    ),
                    new TableGrid(
                        new GridColumn { Width = "2800" },
                        new GridColumn { Width = "900" },
                        new GridColumn { Width = "600" },
                        new GridColumn { Width = "500" },
                        new GridColumn { Width = "2800" },
                        new GridColumn { Width = "900" },
                        new GridColumn { Width = "600" }
                    )
                );

                var rows = new List<TempRow>();
                foreach (var t in groupedList)
                {
                    rows.AddRange(AppendTypeSection(t));
                    rows.AddRange(CreateSpacerRow());
                }

                if (rows.Count > 0) rows.Remove(rows.Last());

                var rowsMiddleNumber = rows.Count % 2 == 0 ? rows.Count / 2 : rows.Count / 2 + 1;

                var left = rows.GetRange(0, rowsMiddleNumber);
                var right = rows.GetRange(rowsMiddleNumber, rows.Count - rowsMiddleNumber);

                if (rows.Count < 20)
                {
                    left = rows;
                    right = [];
                }

                var count = left.Count > right.Count ? left.Count : right.Count;

                for (var i = 0; i < count; i++)
                {
                    var leftRow = left.Count <= i ? null : left[i];
                    var rightRow = right.Count <= i ? null : right[i];

                    if (leftRow == null && rightRow != null) left.Add(CreateSpacerRow());

                    var row = new TableRow();

                    if (leftRow != null) row.Append(leftRow.Cells.ToArray());

                    row.Append(CreateCell(" ", false, null, JustificationValues.Center, 1,
                        false, true));

                    if (rightRow != null) row.Append(rightRow.Cells.ToArray());

                    table.Append(row);
                }


                body.Append(table);
                mainPart.Document.Save();

                Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });

                List<TempRow> AppendTypeSection(IGrouping<string, DelicatesCollForSvod> groupLeft)
                {
                    var rows = new List<TempRow>();
                    rows.AddRange(CreateHeaderRow(groupLeft.Key));

                    var groupedProductsLeft = GetGroupedProductsLeft(groupLeft).ToArray();

                    foreach (var product in groupedProductsLeft)
                    {
                    var (amountText, unitText) = FormatAmount(product);
                        rows.AddRange(CreateProductRow(product.Name, amountText, unitText));
                    }

                    return rows;
                }

                TempRow CreateHeaderRow(string typeName)
                {
                    var row = new TempRow();

                    row.AddCell(CreateCell(typeName, true, "E3EAF2", JustificationValues.Center));
                    row.AddCell(CreateCell(" ", true, "E3EAF2", JustificationValues.Left));
                    row.AddCell(CreateCell(" ", true, "E3EAF2", JustificationValues.Left));
                    return row;
                }

                TempRow CreateProductRow(params string?[] values)
                {
                    var texts = values.Select((value, index) => new { Value = value, Index = index })
                        .ToDictionary(item => item.Index, item => item.Value ?? string.Empty);

                    var row = new TempRow();
                    for (var col = 0; col < 3; col++)
                    {
                        var justify = JustificationValues.Center;
                        var text = texts.GetValueOrDefault(col) ?? string.Empty;

                        row.AddCell(CreateCell(text, false, null, justify));
                    }

                    return row;
                }

                TempRow CreateSpacerRow()
                {
                    var spacerRow = new TempRow();
                    spacerRow.AddCell(CreateSpaceCell());
                    spacerRow.AddCell(CreateSpaceCell());
                    spacerRow.AddCell(CreateSpaceCell());

                    return spacerRow;
                }

                (string amount, string unit) FormatAmount(GroupedProduct product)
                {
                    var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";

                    if (product.Fass > 0)
                    {
                        var packages = product.TotalPackages > 0
                            ? product.TotalPackages
                            : (product.Fass == 0 ? 0 : product.TotalWeight / product.Fass);

                    var packageUnit = !string.IsNullOrEmpty(product.FassIz) ? product.FassIz : defaultUnit;
                    var packageMeasure = FindMeasure(packageUnit);
                    var packagePrecision = packageMeasure?.RoundingPrecision ?? 0;

                    double roundedPackages;
                    if (packagePrecision == 0)
                    {
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
                    var measure = FindMeasure(defaultUnit);
                    if (measure != null) roundingPrecision = measure.RoundingPrecision;

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

                TableCell CreateSpaceCell()
                {
                    return CreateCell(" ", false, null, JustificationValues.Left, 0, true);
                }

                TableCell CreateCell(string text, bool bold, string? shading, JustificationValues justify, int span = 1,
                    bool onlyHorizontalBorder = false, bool withoutBorders = false)
                {
                    var run = new Run(new Text(text ?? string.Empty));
                    var runProps = new RunProperties();
                    if (bold) runProps.Append(new Bold());
                    run.PrependChild(runProps);

                    var paragraph = new Paragraph(new ParagraphProperties(new Justification { Val = justify }), run);

                    var cell = new TableCell(paragraph);
                    var props = new TableCellProperties();
                    if (span > 1) props.Append(new GridSpan { Val = span });
                    if (!string.IsNullOrEmpty(shading))
                        props.Append(new Shading { Fill = shading, Val = ShadingPatternValues.Clear });

                    if (withoutBorders)
                    {
                        var borders = new TableCellBorders();
                        borders.Append(new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
                        borders.Append(new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
                        borders.Append(new LeftBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
                        borders.Append(new RightBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Nil)});
                        props.Append(borders);

                        // Добавляем отступы для лучшей видимости
                        props.Append(new TableCellMargin(
                            new TopMargin { Width = "0" },
                            new BottomMargin { Width = "0" },
                            new LeftMargin { Width = "100" },
                            new RightMargin { Width = "100" }
                        ));
                    }
                    else if (onlyHorizontalBorder)
                    {
                        var borders = new TableCellBorders();
                        borders.Append(new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
                        borders.Append(new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
                        borders.Append(new TopBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 });
                        borders.Append(new BottomBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 });
                        props.Append(borders);

                        // Добавляем отступы для лучшей видимости
                        props.Append(new TableCellMargin(
                            new TopMargin { Width = "0" },
                            new BottomMargin { Width = "0" },
                            new LeftMargin { Width = "100" },
                            new RightMargin { Width = "100" }
                        ));
                    }
                    else
                    {
                        // Все границы
                        props.Append(new TableCellBorders(
                            new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                            new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                        ));
                    }


                    cell.Append(props);
                    return cell;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при создании отчета: {ex.Message}", ex);
        }
    }

    private static IOrderedEnumerable<GroupedProduct> GetGroupedProductsLeft(
        IGrouping<string, DelicatesCollForSvod> groupLeft)
    {
        var groupedProductsLeft = groupLeft
            .GroupBy(r => r.NameT ?? r.Name)
            .Select(g => new GroupedProduct
            {
                Name = g.Key,
                TotalWeight = g.Sum(r => r.Itog),
                TotalPackages = g.Sum(r => r.Fass > 0 ? r.ItogFass : 0),
                FassIz = g.First().FassIz,
                Mera = g.First().Mera,
                Fass = g.First().Fass
            })
            .OrderBy(p => p.Name);

        return groupedProductsLeft;
    }
}

public record GroupedProduct
{
    public string Name { get; init; }
    public decimal TotalWeight { get; init; }
    public decimal TotalPackages { get; init; }
    public string FassIz { get; init; }
    public string Mera { get; init; }
    public decimal Fass { get; init; }
}

public record TempRow
{
    public List<TableCell> Cells { get; } = [];

    public void AddCell(TableCell cell)
    {
        Cells.Add(cell);
    }
}