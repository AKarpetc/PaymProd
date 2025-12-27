using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Enums;

namespace PaymProdNet9.Services;

/// <summary>
///     Класс для печати меню в формате Word
/// </summary>
public class MenuPrinter
{
    private readonly ProductRepository _productRepository;
    private readonly MenuPriceService _menuPriceService;

    public MenuPrinter()
    {
        _productRepository = new ProductRepository();
        _menuPriceService = new MenuPriceService();
    }

    /// <summary>
    ///     Печать меню
    /// </summary>
    public void PrintMenu(List<DelicatesColl> delicates, string menuName, ReportMode reportMode = ReportMode.NoPrices, int? menuId = null)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            var measures = _productRepository.GetMeasures();
            // Обрабатываем дубликаты - берем первую меру с таким названием
            var measureLookup = measures
                .GroupBy(m => m.Name.ToLower().Trim())
                .ToDictionary(g => g.Key, g => g.First());
            var products = _productRepository.GetAllProducts();
            var productLookup = products.ToDictionary(p => p.ID, p => p);

            Measure? FindMeasure(string? measureName)
            {
                if (string.IsNullOrWhiteSpace(measureName)) return null;
                var key = measureName.ToLower().Trim();
                return measureLookup.TryGetValue(key, out var measure) ? measure : null;
            }

            string GetBaseMeasure(Components component)
            {
                // Используем основную единицу измерения из компонента (Mera), как в отчете по товарам
                // Не используем product.Ves, так как это может быть единица фасовки
                return string.IsNullOrWhiteSpace(component.Mera) ? "г" : component.Mera;
            }

            string GetPackageMeasure(Components component, string baseMeasure)
            {
                if (!string.IsNullOrWhiteSpace(component.FassIz))
                    return component.FassIz;

                if (productLookup.TryGetValue(component.Prodid, out var product) &&
                    !string.IsNullOrWhiteSpace(product.IzName))
                {
                    return product.IzName;
                }

                var baseMeasureInfo = FindMeasure(baseMeasure);
                if (!string.IsNullOrWhiteSpace(baseMeasureInfo?.FassIzmer))
                    return baseMeasureInfo.FassIzmer!;

                return baseMeasure;
            }

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
                    new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
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

                bool showPriceColumn = reportMode != ReportMode.NoPrices;

                var tableGrid = showPriceColumn
                    ? new TableGrid(
                        new GridColumn { Width = "2000" },
                        new GridColumn { Width = "5000" },
                        new GridColumn { Width = "2000" })
                    : new TableGrid(
                        new GridColumn { Width = "2000" },
                        new GridColumn { Width = "7000" });
                table.AppendChild(tableGrid);

                foreach (var group in groupedDelicates)
                {
                    // Заголовок группы (тип блюда)
                    var headerRow = new TableRow();
                    var headerCell = new TableCell();
                    var headerCellProperties = new TableCellProperties(
                        new GridSpan { Val = showPriceColumn ? 3 : 2 },
                        new Shading { Fill = "D3D3D3" },
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                    );
                    headerCell.Append(headerCellProperties);

                    var headerParagraph = new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
                    var headerRun = new Run();
                    var headerRunProps = new RunProperties(new Bold(), new FontSize { Val = "28" });
                    headerRun.Append(headerRunProps);
                    headerRun.Append(new Text(group.Key.Type ?? "Без типа"));
                    headerParagraph.Append(headerRun);
                    headerCell.Append(headerParagraph);
                    headerRow.Append(headerCell);
                    table.Append(headerRow);

                    var columnsRow = new TableRow();
                    columnsRow.Append(CreateTableHeaderCell("Блюдо"));
                    columnsRow.Append(CreateTableHeaderCell("Состав"));
                    if (showPriceColumn) columnsRow.Append(CreateTableHeaderCell("Цена, тг"));
                    table.Append(columnsRow);

                    // Блюда в группе
                    foreach (var delicate in group)
                    {
                        var row = new TableRow();

                        // Название блюда
                        var nameCell = new TableCell();
                        nameCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new Text(delicate.Name))));
                        EnsureVerticalCenter(nameCell);
                        row.Append(nameCell);

                        var compositionParagraph = new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }));

                        var components = delicate.Lcomp ?? new List<Components>();
                        decimal dishTotal = 0;
                        if (components.Any())
                        {
                            var componentLines = new List<string>();
                            foreach (var component in components)
                            {
                                var baseMeasure = GetBaseMeasure(component);
                                var productName = !string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name;
                                var count = delicate.Count > 0 ? delicate.Count : 1;

                                // Логика как в отчете по товарам: показываем основную единицу, если нет перерасчета в фасовку
                                string displayValue;
                                var totalWeight = component.Ves * count;
                                
                                // Локальная функция для нормализации единиц
                                string NormalizeUnitLocal(string unit) => unit?.Trim().ToLowerInvariant() ?? string.Empty;
                                
                                // Нормализуем единицы для сравнения (как в отчете по товарам)
                                var baseUnitNormalized = NormalizeUnitLocal(baseMeasure);
                                var fassIzNormalized = NormalizeUnitLocal(component.FassIz ?? string.Empty);
                                
                                // Если на продукте стоит флаг "не переводить в фасованные" — всегда показываем в базовой единице
                                if (component.DoNotConvertToPackInMenu)
                                {
                                    displayValue = FormatMenuValue(Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero), baseMeasure);
                                }
                                else
                                {
                                    // Проверяем, нужно ли пересчитывать в фасовку (как в отчете по товарам)
                                    // Пересчитываем только если: есть фасовка, единица фасовки отличается от базовой, и вес >= фасовка
                                    if (component.Fass > 0 && 
                                        !string.IsNullOrWhiteSpace(component.FassIz) && 
                                        fassIzNormalized != baseUnitNormalized &&
                                        totalWeight >= component.Fass)
                                    {
                                        // Есть перерасчет в фасовку - показываем в фасовке
                                        var packageCount = totalWeight / component.Fass;
                                        var packageUnit = GetPackageMeasure(component, baseMeasure);
                                        displayValue = FormatMenuValue(Math.Round(packageCount, 2, MidpointRounding.AwayFromZero), packageUnit);
                                    }
                                    else
                                    {
                                        // Нет перерасчета в фасовку - показываем в основных единицах
                                        displayValue = FormatMenuValue(Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero), baseMeasure);
                                    }
                                }

                                var line = BuildComponentLine(reportMode, component, productName, displayValue,
                                    menuId, delicate.Count, ref dishTotal);
                                componentLines.Add(line);
                            }

                            // Для режима Price не показываем цены компонентов в составе
                            // В режиме Cost и NoPrices - цены компонентов уже включены в строку (или нет, в зависимости от логики BuildComponentLine)
                            // Если Price - то мы просто перечисляем компоненты без цен
                            
                            if (reportMode == ReportMode.NoPrices || reportMode == ReportMode.Price)
                            {
                                compositionParagraph.Append(new Break());
                                compositionParagraph.Append(new Run(new Text(string.Join(", ", componentLines))));
                            }
                            else
                            {
                                foreach (var line in componentLines)
                                {
                                    compositionParagraph.Append(new Break());
                                    compositionParagraph.Append(new Run(new Text(line)));
                                }
                            }
                        }
                        else
                        {
                            compositionParagraph.Append(new Break());
                            compositionParagraph.Append(new Run(new Text("нет данных")));
                        }

                        var compositionCell = new TableCell();
                        compositionCell.Append(compositionParagraph);
                        EnsureVerticalCenter(compositionCell);
                        row.Append(compositionCell);

                        if (showPriceColumn)
                        {
                            // Если режим Price, применяем наценку
                            if (reportMode == ReportMode.Price && delicate.DefaultMarkup > 0)
                            {
                                 dishTotal = dishTotal * (delicate.DefaultMarkup / 100);
                            }

                            var priceText = dishTotal > 0 ? FormatCurrency(dishTotal) : "—";
                            var priceCell = new TableCell();
                            priceCell.Append(new Paragraph(
                                new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                new Run(new Text(priceText))));
                            EnsureVerticalCenter(priceCell);
                            row.Append(priceCell);
                        }

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
            Logger.Error("Ошибка при создании документа меню", ex);
            throw new Exception($"Ошибка при создании документа: {ex.Message}", ex);
        }
    }

    private string BuildComponentLine(ReportMode reportMode, Components component, string productName, string displayValue,
        int? menuId, decimal dishCount, ref decimal dishTotal)
    {
        // Считаем цену компонента всегда, чтобы накопить dishTotal
        var priceInfo = _menuPriceService.GetComponentPriceInfo(menuId ?? 0, component, dishCount);
        dishTotal += priceInfo.TotalPrice;

        // Если режим Price или NoPrices - не показываем цену компонента в строке
        if (reportMode == ReportMode.Price || reportMode == ReportMode.NoPrices)
            return $"{productName} ({displayValue})";

        // Если режим Cost - показываем цену компонента
        if (priceInfo.TotalPrice <= 0)
            return $"{productName} ({displayValue}) — цена не указана";

        return $"{productName} ({displayValue}) — {FormatCurrency(priceInfo.TotalPrice)} тг";
    }

    private static TableCell CreateTableHeaderCell(string text)
    {
        var cell = new TableCell();
        EnsureVerticalCenter(cell);
        var paragraph = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = new Run();
        run.Append(new RunProperties(new Bold()));
        run.Append(new Text(text));
        paragraph.Append(run);
        cell.Append(paragraph);
        return cell;
    }

    private static void EnsureVerticalCenter(TableCell cell)
    {
        var props = cell.GetFirstChild<TableCellProperties>();
        if (props == null)
        {
            props = new TableCellProperties();
            cell.PrependChild(props);
        }

        var existing = props.GetFirstChild<TableCellVerticalAlignment>();
        if (existing != null)
        {
            existing.Val = TableVerticalAlignmentValues.Center;
        }
        else
        {
            props.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        }
    }

    private static string FormatCurrency(decimal value) =>
        Math.Round(value, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.CurrentCulture);

    /// <summary>
    /// Форматирование значения для меню по логике старого приложения
    /// </summary>
    private static string FormatMenuValue(decimal value, string unit)
    {
        // В старом приложении использовалось Math.Round с 2 знаками
        // Если значение целое, показываем без дробной части
        if (value == Math.Truncate(value))
            return $"{(int)value}{unit}";
        
        return $"{value:F2}{unit}";
    }

    /// <summary>
    ///     Печать отчета с продуктами
    /// </summary>
    public void PrintReport(List<DelicatesCollForSvod> reportData, string menuName, bool includePrices = false)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            // Получаем все меры для определения округления
            var measures = _productRepository.GetMeasures();
            // Обрабатываем дубликаты - берем первую меру с таким названием
            static Measure PickPreferred(IEnumerable<Measure> candidates) =>
                candidates
                    .OrderByDescending(m => m.Fass > 1 ? 1 : 0)
                    .ThenBy(m => m.Id)
                    .First();

            var measuresDict = measures
                .GroupBy(m => m.Name.ToLower().Trim())
                .ToDictionary(g => g.Key, PickPreferred);

            // Функция для поиска меры по имени (с учетом вариаций)
            Measure? FindMeasure(string? measureName)
            {
                if (string.IsNullOrEmpty(measureName)) return null;

                var lowerName = measureName.ToLower().Trim();

                if (measuresDict.ContainsKey(lowerName))
                    return measuresDict[lowerName];

                var partial = measures.Where(m =>
                    lowerName.Contains(m.Name.ToLower().Trim()) ||
                    m.Name.ToLower().Trim().Contains(lowerName));
                if (partial.Any())
                    return PickPreferred(partial);

                return null;
            }

            string NormalizeUnit(string unit) =>
                unit?.Trim().ToLowerInvariant() ?? string.Empty;

            Measure? FindChildMeasure(string? parentUnit)
            {
                if (string.IsNullOrWhiteSpace(parentUnit))
                    return null;

                var normalizedParent = NormalizeUnit(parentUnit);
                return measures.FirstOrDefault(m =>
                    m.Fass > 0 &&
                    !string.IsNullOrWhiteSpace(m.FassIzmer) &&
                    NormalizeUnit(m.FassIzmer) == normalizedParent);
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
                        new Text("Отчет по товарам"))
                ));

                var infoParagraph = body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                ));
                infoParagraph.AppendChild(new Run(new Text($"Банкет: {menuName}")));
                infoParagraph.AppendChild(new Break());
                var dateRun = new Run();
                // Пытаемся извлечь дату из menuName (формат: "название, количество человек, дата")
                string dateText;
                if (menuName.Contains(","))
                {
                    var parts = menuName.Split(',');
                    if (parts.Length >= 3 && DateTime.TryParse(parts[2].Trim(), out var banquetDate))
                    {
                        dateText = banquetDate.ToString("dd.MM.yyyy HH:mm");
                    }
                    else
                    {
                        dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    }
                }
                else
                {
                    dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                }
                dateRun.AppendChild(new Text($"Дата, начало: {dateText}"));
                infoParagraph.AppendChild(dateRun);

                body.AppendChild(new Paragraph()); // Пустая строка

                var groupedList = reportData
                    .GroupBy(r => r.Type ?? "Без типа")
                    .OrderBy(g => productTypesDict.ContainsKey(g.Key) ? productTypesDict[g.Key] : int.MaxValue)
                    .ThenBy(g => g.Key)
                    .ToList();

                if (includePrices)
                {
                    var priceTable = new Table(
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
                            new GridColumn { Width = "3600" },
                            new GridColumn { Width = "1400" },
                            new GridColumn { Width = "900" },
                            new GridColumn { Width = "1800" }
                        )
                    );

                    foreach (var group in groupedList)
                    {
                        var headerRow = new TableRow();
                        headerRow.Append(CreateCell(group.Key, true, "E3EAF2", JustificationValues.Center, 4));
                        priceTable.Append(headerRow);

                        var titlesRow = new TableRow();
                        titlesRow.Append(CreateCell("Продукт", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Количество", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Ед.", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Цена", true, "DDEBF7", JustificationValues.Center));
                        priceTable.Append(titlesRow);

                        var groupedProductsLeft = GetGroupedProductsLeft(group).ToArray();

                        foreach (var product in groupedProductsLeft)
                        {
                            var (amountText, unitText) = FormatAmount(product);
                            priceTable.Append(CreatePriceRow(product.Name, amountText, unitText,
                                FormatCurrency(product.TotalPrice)));
                        }
                    }

                    body.Append(priceTable);
                }
                else
                {
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
                }
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

                TableRow CreatePriceRow(string productName, string amountText, string unitText, string priceText)
                {
                    var row = new TableRow();
                    row.Append(CreateCell(productName, false, null, JustificationValues.Left));
                    row.Append(CreateCell(amountText, false, null, JustificationValues.Right));
                    row.Append(CreateCell(unitText, false, null, JustificationValues.Center));
                    row.Append(CreateCell(priceText, false, null, JustificationValues.Right));
                    return row;
                }

                (string amount, string unit) FormatAmount(GroupedProduct product)
                {
                    var defaultUnit = !string.IsNullOrEmpty(product.Mera) ? product.Mera : "шт";
                    var normalizedUnit = NormalizeUnit(defaultUnit);

                    if (!IsDiscreteUnit(normalizedUnit))
                    {
                        return FormatContinuous(product, defaultUnit, normalizedUnit);
                    }

                    return FormatDiscrete(product, defaultUnit);
                }

                bool IsDiscreteUnit(string unit)
                {
                    if (string.IsNullOrEmpty(unit)) return false;
                    string[] discreteKeywords = { "шт", "бут", "бан", "пач", "рулон", "компл", "уп", "набор" };
                    return discreteKeywords.Any(unit.Contains);
                }

                (string amount, string unit) FormatContinuous(
                    GroupedProduct product,
                    string originalUnit,
                    string normalizedUnit)
                {
                    var measure = FindMeasure(originalUnit);
                    var roundingPrecision = measure?.RoundingPrecision ?? 2;
                    var totalValue = (double)product.TotalWeight;
                    var displayUnit = originalUnit;
                    var currentMeasure = measure;
                    const int maxUnitHops = 10;

                    var baseUnitNormalized = normalizedUnit;

                    if (product.Fass > 0 && !string.IsNullOrWhiteSpace(product.FassIz) &&
                        NormalizeUnit(product.FassIz) != baseUnitNormalized &&
                        totalValue >= (double)product.Fass)
                    {
                        totalValue /= (double)product.Fass;
                        displayUnit = product.FassIz;
                        normalizedUnit = NormalizeUnit(displayUnit);

                        currentMeasure = FindMeasure(product.FassIz) ?? currentMeasure;
                        if (currentMeasure != null)
                        {
                            roundingPrecision = currentMeasure.RoundingPrecision;
                        }
                    }

                    if (currentMeasure != null)
                    {
                        var hop = 0;
                        while (hop++ < maxUnitHops &&
                               currentMeasure.Fass > 0 &&
                               totalValue >= currentMeasure.Fass &&
                               !string.IsNullOrWhiteSpace(currentMeasure.FassIzmer))
                        {
                            var parent = FindMeasure(currentMeasure.FassIzmer);
                            if (parent == null)
                            {
                                break;
                            }

                            if (NormalizeUnit(parent.Name) == NormalizeUnit(displayUnit))
                            {
                                break;
                            }

                            totalValue /= currentMeasure.Fass;
                            currentMeasure = parent;
                            displayUnit = currentMeasure.Name;
                            roundingPrecision = currentMeasure.RoundingPrecision;
                        }

                        normalizedUnit = NormalizeUnit(displayUnit);

                        hop = 0;
                        while (totalValue < 1 && hop++ < maxUnitHops)
                        {
                            var child = FindChildMeasure(normalizedUnit);
                            if (child == null || child.Fass <= 0)
                            {
                                break;
                            }

                            if (NormalizeUnit(child.Name) == normalizedUnit)
                            {
                                break;
                            }

                            totalValue *= child.Fass;
                            currentMeasure = child;
                            displayUnit = child.Name;
                            roundingPrecision = child.RoundingPrecision;
                            normalizedUnit = NormalizeUnit(displayUnit);

                            if (totalValue >= 1)
                            {
                                break;
                            }
                        }
                    }

                    double roundedValue;
                    if (roundingPrecision <= 0)
                    {
                        roundedValue = Math.Ceiling(totalValue);
                    }
                    else
                    {
                        var multiplier = Math.Pow(10, roundingPrecision);
                        roundedValue = Math.Ceiling(totalValue * multiplier) / multiplier;
                    }

                    var formatted = roundingPrecision <= 0
                        ? ((int)roundedValue).ToString(CultureInfo.CurrentCulture)
                        : roundedValue.ToString($"F{roundingPrecision}", CultureInfo.CurrentCulture);

                    return (formatted, displayUnit);
                }

                (string amount, string unit) FormatDiscrete(GroupedProduct product, string defaultUnit)
                {
                    var measure = FindMeasure(defaultUnit);
                    var effectivePackSize = product.Fass > 0
                        ? (double)product.Fass
                        : measure?.Fass > 0
                            ? measure.Fass
                            : 1d;

                    var value = product.TotalPackages > 0
                        ? (double)product.TotalPackages
                        : effectivePackSize > 0
                            ? (double)(product.TotalWeight / (decimal)effectivePackSize)
                            : (double)product.TotalWeight;

                    var precision = measure?.MenuRoundingPrecision ?? measure?.RoundingPrecision ?? 0;

                    double roundedValue;
                    if (precision <= 0)
                    {
                        roundedValue = Math.Ceiling(value);
                    }
                    else
                    {
                        var multiplier = Math.Pow(10, precision);
                        roundedValue = Math.Ceiling(value * multiplier) / multiplier;
                    }

                    var formatted = precision <= 0
                        ? ((int)roundedValue).ToString(CultureInfo.CurrentCulture)
                        : roundedValue.ToString($"F{precision}", CultureInfo.CurrentCulture);

                    var unitText = !string.IsNullOrWhiteSpace(product.FassIz)
                        ? product.FassIz
                        : defaultUnit;

                    return (formatted, unitText);
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
            Logger.Error("Ошибка при создании отчета по продуктам", ex);
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
                Fass = g.First().Fass,
                TotalPrice = g.Sum(r => r.TotalPrice)
            })
            .OrderBy(p => p.Name);

        return groupedProductsLeft;
    }
}

public record GroupedProduct
{
    public string Name { get; init; } = string.Empty;
    public decimal TotalWeight { get; init; }
    public decimal TotalPackages { get; init; }
    public string? FassIz { get; init; }
    public string? Mera { get; init; }
    public decimal Fass { get; init; }
    public decimal TotalPrice { get; init; }
}

public record TempRow
{
    public List<TableCell> Cells { get; } = [];

    public void AddCell(TableCell cell)
    {
        Cells.Add(cell);
    }
}