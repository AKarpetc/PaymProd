using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PaymProdNet9.Models;
using System.Diagnostics;
using System.IO;

namespace PaymProdNet9.Services;

/// <summary>
/// Класс для печати меню в формате Word
/// </summary>
public class MenuPrinter
{
    /// <summary>
    /// Печать меню
    /// </summary>
    public void PrintMenu(List<DelicatesColl> delicates, string menuName)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            
            // Группируем блюда по типам
            var groupedDelicates = delicates
                .Where(d => d.Lcomp != null && d.Lcomp.Any())
                .GroupBy(d => d.Type)
                .OrderBy(g => g.Key);

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
                    headerRun.Append(new Text(group.Key ?? "Без типа"));
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

                // Группируем по типам продуктов
                var groupedByType = reportData
                    .GroupBy(r => r.Type)
                    .OrderBy(g => g.Key);

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

                // Заголовок таблицы
                var headerRow = new TableRow();
                
                var headers = new[] { "Продукт", "Количество порций", "Вес не фасованный", "Фасованный вес" };
                foreach (var headerText in headers)
                {
                    var headerCell = new TableCell();
                    var headerParagraph = new Paragraph();
                    var headerRun = new Run();
                    var headerRunProps = new RunProperties(new Bold());
                    headerRun.Append(headerRunProps);
                    headerRun.Append(new Text(headerText));
                    headerParagraph.Append(headerRun);
                    headerCell.Append(headerParagraph);
                    headerRow.Append(headerCell);
                }
                table.Append(headerRow);

                foreach (var group in groupedByType)
                {
                    // Заголовок группы
                    var groupHeaderRow = new TableRow();
                    var groupHeaderCell = new TableCell();
                    var groupHeaderCellProperties = new TableCellProperties(
                        new GridSpan { Val = 4 },
                        new Shading { Fill = "ADD8E6" }
                    );
                    groupHeaderCell.Append(groupHeaderCellProperties);
                    
                    var groupHeaderParagraph = new Paragraph();
                    var groupHeaderRun = new Run();
                    var groupHeaderRunProps = new RunProperties(new Bold(), new FontSize { Val = "24" });
                    groupHeaderRun.Append(groupHeaderRunProps);
                    groupHeaderRun.Append(new Text(group.Key));
                    groupHeaderParagraph.Append(groupHeaderRun);
                    groupHeaderCell.Append(groupHeaderParagraph);
                    groupHeaderRow.Append(groupHeaderCell);
                    table.Append(groupHeaderRow);

                    // Продукты в группе
                    var groupedProducts = group
                        .GroupBy(r => r.Name)
                        .Select(g => new
                        {
                            Name = g.Key,
                            Count = g.Sum(r => r.Countpor),
                            Itog = g.Sum(r => r.Itog),
                            ItogFass = g.Sum(r => r.ItogFass),
                            Mera = g.First().Mera,
                            FassIz = g.First().FassIz
                        });

                    foreach (var product in groupedProducts)
                    {
                        var row = new TableRow();
                        
                        row.Append(new TableCell(new Paragraph(new Run(new Text(product.Name)))));
                        row.Append(new TableCell(new Paragraph(new Run(new Text(product.Count.ToString())))));
                        row.Append(new TableCell(new Paragraph(new Run(new Text($"{Math.Round(product.Itog, 2)}{product.Mera}")))));
                        row.Append(new TableCell(new Paragraph(new Run(new Text($"{Math.Round(product.ItogFass, 2)}{product.FassIz}")))));
                        
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
            throw new Exception($"Ошибка при создании отчета: {ex.Message}", ex);
        }
    }
}

