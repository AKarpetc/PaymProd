using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System.Diagnostics;
using System.IO;

namespace PaymProdNet9.Services;

/// <summary>
/// Класс для печати меню в формате Word
/// </summary>
public class MenuPrinter
{
    private readonly ProductRepository _productRepository;
    
    public MenuPrinter()
    {
        _productRepository = new ProductRepository();
    }
    /// <summary>
    /// Печать меню
    /// </summary>
    public void PrintMenu(List<DelicatesColl> delicates, string menuName)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            
            // Группируем блюда по типам и сортируем по SortOrder
            var groupedDelicates = delicates
                .Where(d => d.Lcomp != null && d.Lcomp.Any())
                .GroupBy(d => new { Type = d.Type, SortOrder = d.TypeSortOrder })
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
    /// Печать отчета с продуктами
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
                {
                    if (lowerName.Contains(measure.Name.ToLower()) || measure.Name.ToLower().Contains(lowerName))
                        return measure;
                }
                
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

                // Заголовок
                var titleParagraph = body.AppendChild(new Paragraph());
                var titleRun = titleParagraph.AppendChild(new Run());
                var titleRunProperties = titleRun.AppendChild(new RunProperties());
                titleRunProperties.AppendChild(new Bold());
                titleRunProperties.AppendChild(new FontSize { Val = "32" });
                titleRun.AppendChild(new Text($"Отчет по меню: {menuName}"));
                
                var titleProperties = titleParagraph.AppendChild(new ParagraphProperties());
                titleProperties.AppendChild(new Justification { Val = JustificationValues.Center });

                body.AppendChild(new Paragraph()); // Пустая строка

                var groupedList = reportData
                    .GroupBy(r => r.Type ?? "Без типа")
                    .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                    .ThenBy(g => g.Key)
                    .ToList();

                var table = new Table(
                    new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Pct, Width = "10000" },
                        new TableBorders(
                            new TopBorder { Val = BorderValues.None },
                            new BottomBorder { Val = BorderValues.None },
                            new LeftBorder { Val = BorderValues.None },
                            new RightBorder { Val = BorderValues.None },
                            new InsideHorizontalBorder { Val = BorderValues.None },
                            new InsideVerticalBorder { Val = BorderValues.None }
                        )
                    ),
                    new TableGrid(
                        new GridColumn { Width = "4200" },
                        new GridColumn { Width = "1500" },
                        new GridColumn { Width = "1000" },
                        new GridColumn { Width = "400" },
                        new GridColumn { Width = "4200" },
                        new GridColumn { Width = "1500" },
                        new GridColumn { Width = "1000" }
                    )
                );

                for (int i = 0; i < groupedList.Count; i++)
                {
                    AppendTypeSection(table, groupedList[i], i);
                    table.Append(CreateSpacerRow());
                }

                body.Append(table);
                mainPart.Document.Save();

                Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });

                void AppendTypeSection(Table tbl, IGrouping<string, DelicatesCollForSvod> group, int index)
                {
                    bool isLeft = index % 2 == 0;
                    int startCol = isLeft ? 0 : 4;

                    tbl.Append(CreateHeaderRow(group.Key, startCol));

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
                        var (amountText, unitText) = FormatAmount(product);
                        tbl.Append(CreateProductRow(product.Name, amountText, unitText, startCol));
                    }
                }

                TableRow CreateHeaderRow(string typeName, int startCol)
                {
                    var row = new TableRow();
                    for (int col = 0; col < 7; col++)
                    {
                        string text = string.Empty;
                        bool bold = false;
                        string? shading = null;
                        var justify = JustificationValues.Left;

                        if (col == startCol)
                        {
                            text = typeName;
                            bold = true;
                            shading = "E3EAF2";
                        }
                        else if (col == startCol + 1)
                        {
                            text = "Кол-во";
                            bold = true;
                            shading = "DDEBF7";
                            justify = JustificationValues.Center;
                        }
                        else if (col == startCol + 2)
                        {
                            text = "ед.";
                            bold = true;
                            shading = "DDEBF7";
                            justify = JustificationValues.Center;
                        }

                        row.Append(CreateCell(text, bold, shading, justify));
                    }

                    return row;
                }

                TableRow CreateProductRow(string name, string amount, string unit, int startCol)
                {
                    var row = new TableRow();
                    for (int col = 0; col < 7; col++)
                    {
                        string text = string.Empty;
                        var justify = JustificationValues.Left;

                        if (col == startCol)
                        {
                            text = name;
                        }
                        else if (col == startCol + 1)
                        {
                            text = amount;
                            justify = JustificationValues.Center;
                        }
                        else if (col == startCol + 2)
                        {
                            text = unit;
                            justify = JustificationValues.Center;
                        }

                        row.Append(CreateCell(text, false, null, justify));
                    }

                    return row;
                }

                TableRow CreateSpacerRow()
                {
                    var spacerCell = CreateCell(" ", false, null, JustificationValues.Left, span: 7);
                    spacerCell.TableCellProperties ??= new TableCellProperties();
                    spacerCell.TableCellProperties.TableCellBorders = new TableCellBorders(
                        new TopBorder { Val = BorderValues.None },
                        new BottomBorder { Val = BorderValues.None },
                        new LeftBorder { Val = BorderValues.None },
                        new RightBorder { Val = BorderValues.None }
                    );
                    return new TableRow(spacerCell);
                }

                (string amount, string unit) FormatAmount(dynamic product)
                {
                    string measureUnit = product.Fass > 0 && !string.IsNullOrEmpty(product.FassIz)
                        ? product.FassIz
                        : (!string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт");

                    if (string.IsNullOrEmpty(measureUnit))
                    {
                        measureUnit = "шт";
                    }

                    double totalValue = (double)product.TotalWeight;
                    bool convertedToKg = false;
                    string? mera = product.Mera;
                    string? fassIz = product.FassIz;

                    if (product.Fass > 0 &&
                        !string.IsNullOrEmpty(mera) &&
                        !string.IsNullOrEmpty(fassIz) &&
                        (mera.ToLower().Contains("г") || mera.ToLower().Contains("грамм") || "мера".ToLower() == "г") &&
                        (fassIz.ToLower().Contains("кг") || fassIz.ToLower().Contains("kg") || fassIz.ToLower() == "кг"))
                    {
                        totalValue /= 1000.0;
                        measureUnit = "кг";
                        convertedToKg = true;
                    }

                    int roundingPrecision = 2;
                    var measure = FindMeasure(measureUnit);
                    if (measure != null)
                    {
                        roundingPrecision = measure.RoundingPrecision;
                    }
                    else if (!convertedToKg)
                    {
                        measure = FindMeasure(product.Mera);
                        if (measure != null)
                        {
                            roundingPrecision = measure.RoundingPrecision;
                        }
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

                    string formattedNumber = roundingPrecision == 0
                        ? ((int)roundedValue).ToString()
                        : roundedValue.ToString($"F{roundingPrecision}");

                    return (formattedNumber, measureUnit);
                }

                TableCell CreateCell(string text, bool bold, string? shading, JustificationValues justify, int span = 1)
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
                    {
                        props.Append(new Shading { Fill = shading, Val = ShadingPatternValues.Clear });
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
}

