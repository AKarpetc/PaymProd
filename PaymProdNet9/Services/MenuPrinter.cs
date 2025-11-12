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

                // Группируем по типам продуктов и сортируем по SortOrder
                var groupedByType = reportData
                    .GroupBy(r => r.Type ?? "Без типа")
                    .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                    .ThenBy(g => g.Key);

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

                foreach (var group in groupedByType)
                {
                    // Заголовок группы (тип продукта)
                    var groupHeaderRow = new TableRow();
                    var groupHeaderCell = new TableCell();
                    var groupHeaderCellProperties = new TableCellProperties(
                        new GridSpan { Val = 2 },
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
                        .GroupBy(r => r.NameT ?? r.Name)
                        .Select(g => new
                        {
                            Name = g.Key,
                            TotalWeight = g.Sum(r => r.Fass > 0 ? r.ItogFass : r.Itog), // Используем фасовку если Fass > 0, иначе обычный вес
                            FassIz = g.First().FassIz ?? g.First().Mera ?? "",
                            Mera = g.First().Mera ?? "",
                            Fass = g.First().Fass
                        })
                        .OrderBy(p => p.Name);

                    foreach (var product in groupedProducts)
                    {
                        // Определяем единицу измерения для отображения
                        // Если есть фасовка (Fass > 0), используем FassIz, иначе Mera
                        string measureUnit = product.Fass > 0 && !string.IsNullOrEmpty(product.FassIz) 
                            ? product.FassIz 
                            : (!string.IsNullOrEmpty(product.Mera) ? product.Mera : "");
                        
                        if (string.IsNullOrEmpty(measureUnit))
                        {
                            measureUnit = "шт"; // По умолчанию штуки, если единица не указана
                        }
                        
                        double totalValue = (double)product.TotalWeight;
                        
                        // Конвертируем граммы в килограммы, если исходная мера в граммах, а фасовка в килограммах
                        bool convertedToKg = false;
                        string? mera = product.Mera;
                        string? fassIz = product.FassIz;
                        
                        // Конвертируем только если:
                        // 1. Исходная мера в граммах (г, грамм)
                        // 2. И фасовка в килограммах (кг, kg)
                        // 3. И используется фасовка (Fass > 0)
                        if (product.Fass > 0 && 
                            !string.IsNullOrEmpty(mera) && 
                            !string.IsNullOrEmpty(fassIz) &&
                            (mera.ToLower().Contains("г") || mera.ToLower().Contains("грамм") || mera.ToLower() == "г") && 
                            (fassIz.ToLower().Contains("кг") || fassIz.ToLower().Contains("kg") || fassIz.ToLower() == "кг"))
                        {
                            totalValue = totalValue / 1000.0;
                            measureUnit = "кг";
                            convertedToKg = true;
                        }
                        
                        // Получаем точность округления из справочника мер
                        int roundingPrecision = 2; // По умолчанию до сотых
                        var measure = FindMeasure(measureUnit);
                        if (measure != null)
                        {
                            roundingPrecision = measure.RoundingPrecision;
                        }
                        else if (!convertedToKg)
                        {
                            // Если не конвертировали, пробуем найти по исходной мере
                            measure = FindMeasure(product.Mera);
                            if (measure != null)
                            {
                                roundingPrecision = measure.RoundingPrecision;
                            }
                        }
                        
                        // Округляем значение ВСЕГДА В БОЛЬШУЮ СТОРОНУ (вверх)
                        double roundedValue;
                        if (roundingPrecision == 0)
                        {
                            // Округляем до целого вверх
                            roundedValue = Math.Ceiling(totalValue);
                        }
                        else
                        {
                            // Округляем до нужного количества знаков вверх
                            double multiplier = Math.Pow(10, roundingPrecision);
                            roundedValue = Math.Ceiling(totalValue * multiplier) / multiplier;
                        }
                        
                        // Форматируем единицу измерения
                        string formattedValue;
                        if (roundingPrecision == 0)
                        {
                            formattedValue = $"{(int)roundedValue}{measureUnit}";
                        }
                        else
                        {
                            // Форматируем с нужным количеством знаков после запятой
                            formattedValue = $"{roundedValue.ToString($"F{roundingPrecision}")}{measureUnit}";
                        }
                        
                        var row = new TableRow();
                        
                        // Название продукта
                        row.Append(new TableCell(new Paragraph(new Run(new Text(product.Name)))));
                        
                        // Количество с единицей измерения
                        row.Append(new TableCell(new Paragraph(new Run(new Text(formattedValue)))));
                        
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

