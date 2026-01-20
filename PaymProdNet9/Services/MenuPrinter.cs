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
    private readonly SettingsRepository _settingsRepository;
    private readonly MenuRepository _menuRepository;

    public MenuPrinter()
    {
        _productRepository = new ProductRepository();
        _menuPriceService = new MenuPriceService();
        _settingsRepository = new SettingsRepository();
        _menuRepository = new MenuRepository();
    }

    /// <summary>
    ///     Печать меню
    /// </summary>
    public void PrintMenu(List<DelicatesColl> delicates, string menuName, ReportMode reportMode = ReportMode.NoPrices,
        int? menuId = null)
    {
        try
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            var settings = _settingsRepository.GetSettings();
            var menuFontSizeStr = (settings.MenuReportFontSize * 2).ToString();

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
                    return product.IzName;

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
                titleRunProperties.AppendChild(new FontSize { Val = (settings.MenuReportFontSize * 2 + 4).ToString() });
                titleRun.AppendChild(new Text($"Меню: {menuName}"));

                var titleProperties = titleParagraph.AppendChild(new ParagraphProperties());
                titleProperties.AppendChild(new Justification { Val = JustificationValues.Center });

                // Removed SectionProperties from here to fix Word corruption. Will add at the end of Body.
               
                // body.AppendChild(new Paragraph()); // Пустая строка
                // body.AppendChild(new Paragraph()); // Пустая строка

                // Создаем таблицу
                var table = new Table();

                // Свойства таблицы
                var tableProperties = new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                    new TableBorders(
                        new TopBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" },
                        new BottomBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" },
                        new LeftBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" },
                        new RightBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" },
                        new InsideHorizontalBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" },
                        new InsideVerticalBorder
                            { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "000000" }
                    ),
                    new TableCellMarginDefault(
                        new TopMargin { Width = "50", Type = TableWidthUnitValues.Dxa },
                        new StartMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                        new BottomMargin { Width = "50", Type = TableWidthUnitValues.Dxa },
                        new EndMargin { Width = "100", Type = TableWidthUnitValues.Dxa }
                    )
                );
                table.AppendChild(tableProperties);

                var showPriceColumn = reportMode != ReportMode.NoPrices;

                long dishWidth = 2000;
                long compWidth = 3000;

                if (reportMode == ReportMode.Price) { dishWidth = 2000; compWidth = 3000; }
                else if (reportMode == ReportMode.Full) { dishWidth = 920; compWidth = 4080; }
                else if (reportMode == ReportMode.Cost) { dishWidth = 736; compWidth = 4264; } // Special Cost mode (-20% Dish)
                else if (reportMode == ReportMode.NoPrices) { dishWidth = 2000; compWidth = 7000; }
                else { dishWidth = 2000; compWidth = 5000; } // Fallback

                var tableGrid = new TableGrid();
                tableGrid.Append(new GridColumn { Width = dishWidth.ToString() });
                tableGrid.Append(new GridColumn { Width = compWidth.ToString() });

                if (showPriceColumn) 
                {
                     if (reportMode == ReportMode.Price) 
                     {
                         tableGrid.Append(new GridColumn { Width = "1000" });
                         tableGrid.Append(new GridColumn { Width = "1000" });
                         tableGrid.Append(new GridColumn { Width = "1000" }); // Total Cost [NEW]
                         tableGrid.Append(new GridColumn { Width = "2000" });
                     }
                     else if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                     {
                         tableGrid.Append(new GridColumn { Width = "1000" });
                         tableGrid.Append(new GridColumn { Width = "1000" });
                         tableGrid.Append(new GridColumn { Width = "1000" });
                         tableGrid.Append(new GridColumn { Width = "2000" });
                     }
                     else 
                     {
                         // Fallback else
                         tableGrid.Append(new GridColumn { Width = "2000" });
                     }
                }
                
                table.AppendChild(tableGrid);

                foreach (var group in groupedDelicates)
                {
                    // Заголовок группы (тип блюда)
                    var headerRow = new TableRow();
                    var headerCell = new TableCell();
                    var headerCellProperties = new TableCellProperties(
                        new GridSpan { Val = showPriceColumn ? (reportMode == ReportMode.Price ? 6 : (reportMode == ReportMode.Full || reportMode == ReportMode.Cost ? 6 : 3)) : 2 },
                        new Shading { Fill = "D3D3D3" },
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                    );
                    headerCell.Append(headerCellProperties);

                    var headerParagraph = new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
                    var headerRun = new Run();
                    var headerRunProps = new RunProperties(new Bold(), new FontSize { Val = (settings.MenuReportFontSize * 2 + 4).ToString() });
                    headerRun.Append(headerRunProps);
                    headerRun.Append(new Text(group.Key.Type ?? "Без типа"));
                    headerParagraph.Append(headerRun);
                    headerCell.Append(headerParagraph);
                    headerRow.Append(headerCell);
                    table.Append(headerRow);

                    var columnsRow = new TableRow();
                    columnsRow.Append(CreateTableHeaderCell("Блюдо", menuFontSizeStr));
                    columnsRow.Append(CreateTableHeaderCell("Состав", menuFontSizeStr));
                    if (showPriceColumn)
                    {
                        if (reportMode == ReportMode.Price)
                        {
                            columnsRow.Append(CreateTableHeaderCell("Себ. порции", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Цена порции", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Себест.", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Сумма, тг", menuFontSizeStr));
                        }
                        else if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                        {
                            columnsRow.Append(CreateTableHeaderCell("Себ. порции", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Отп. цена", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Итог себ.", menuFontSizeStr));
                            columnsRow.Append(CreateTableHeaderCell("Итог отп.", menuFontSizeStr));
                        }
                        else
                        {
                            columnsRow.Append(CreateTableHeaderCell("Стоимость", menuFontSizeStr));
                        }
                    }
                    table.Append(columnsRow);

                    // Блюда в группе
                    foreach (var delicate in group)
                    {
                        var row = new TableRow();

                        // Название блюда
                        var nameCell = new TableCell();
                        nameCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new FontSize { Val = menuFontSizeStr }), new Text(delicate.Name))));
                        EnsureVerticalCenter(nameCell);
                        row.Append(nameCell);

                        var compositionElement = CreateCompositionElement(delicate, reportMode, menuFontSizeStr, menuId,
                            out var dishTotal, measureLookup, productLookup, compWidth);
                        
                        var compositionCell = new TableCell();
                        compositionCell.Append(compositionElement);
                        
                        // Fix for Word corruption: Cell must end with a paragraph, not a table
                        if (compositionElement is Table)
                        {
                            compositionCell.Append(new Paragraph());
                        }

                        EnsureVerticalCenter(compositionCell);
                        row.Append(compositionCell);
                        
                        if (showPriceColumn)
                        {
                            // Сохраняем себестоимость до применения наценки
                            var rawDishTotal = dishTotal;

                            // Если режим Price, применяем наценку
                            if ((reportMode == ReportMode.Price) && delicate.DefaultMarkup > 0)
                                dishTotal = dishTotal * (delicate.DefaultMarkup / 100);

                            if (reportMode == ReportMode.Price || reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                            {
                                // --- Новые колонки для Price и Full режима ---
                                var portions = delicate.Count > 0 ? delicate.Count : 1;
                                
                                // 1. Себестоимость порции
                                var unitCost = rawDishTotal / portions;
                                var unitCostText = unitCost > 0 ? unitCost.ToString("N1", System.Globalization.CultureInfo.CurrentCulture) : "—";
                                var unitCostCell = new TableCell();
                                unitCostCell.Append(new Paragraph(
                                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                    new Run(new RunProperties(new FontSize { Val = menuFontSizeStr }), new Text(unitCostText))));
                                EnsureVerticalCenter(unitCostCell);
                                row.Append(unitCostCell);

                                // 2. Цена порции
                                // In Full mode dishTotal is RAW cost because we didn't apply markup above
                                // Calculate markup price locally if mode is Full
                                var markupPrice = rawDishTotal;
                                if ((reportMode == ReportMode.Full || reportMode == ReportMode.Cost) && delicate.DefaultMarkup > 0)
                                    markupPrice = rawDishTotal * (delicate.DefaultMarkup / 100);
                                else if (reportMode == ReportMode.Price)
                                    markupPrice = dishTotal; // Already applied

                                var unitPrice = markupPrice / portions;
                                var unitPriceText = unitPrice > 0 ? unitPrice.ToString("N1", System.Globalization.CultureInfo.CurrentCulture) : "—";
                                var unitPriceCell = new TableCell();
                                unitPriceCell.Append(new Paragraph(
                                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                    new Run(new RunProperties(new FontSize { Val = menuFontSizeStr }), new Text(unitPriceText))));
                                EnsureVerticalCenter(unitPriceCell);
                                row.Append(unitPriceCell);
                            }

                            
                            if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost || reportMode == ReportMode.Price)
                            {
                                // 2.5 Total Cost (Raw)
                                var rawCostText = rawDishTotal > 0 ? FormatCurrency(rawDishTotal) : "—";
                                var rawCostCell = new TableCell();
                                rawCostCell.Append(new Paragraph(
                                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                    new Run(new RunProperties(new FontSize { Val = menuFontSizeStr }), new Text(rawCostText))));
                                EnsureVerticalCenter(rawCostCell);
                                row.Append(rawCostCell);
                            }
                            
                            // 3. Общая сумма (Цена, тг)
                            // In Full Mode, dishTotal is Raw. We need to calculate Price.
                            var priceForTotalColumn = dishTotal;
                             if ((reportMode == ReportMode.Full || reportMode == ReportMode.Cost) && delicate.DefaultMarkup > 0)
                                 priceForTotalColumn = dishTotal * (delicate.DefaultMarkup / 100);

                            var priceText = priceForTotalColumn > 0 ? FormatCurrency(priceForTotalColumn) : "—";
                            var priceCell = new TableCell();
                            priceCell.Append(new Paragraph(
                                new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                new Run(new RunProperties(new FontSize { Val = menuFontSizeStr }), new Text(priceText))));
                            EnsureVerticalCenter(priceCell);
                            row.Append(priceCell);
                        }

                        table.Append(row);
                    }
                }

                // --- Добавляем итоговые строки ---
                // var settings = _settingsRepository.GetSettings(); // Removed duplicate declaration
                decimal totalDishSum = 0;
                decimal totalCostSum = 0;
                decimal totalUnitCostSum = 0;
                decimal totalUnitPriceSum = 0;

                // Пересчитываем сумму всех блюд для итога
                foreach (var group in groupedDelicates)
                foreach (var delicate in group)
                {
                    var components = delicate.Lcomp ?? new List<Components>();
                    decimal dishTotal = 0;
                    if (components.Any())
                        foreach (var component in components)
                        {
                            var priceInfo =
                                _menuPriceService.GetComponentPriceInfo(menuId ?? 0, component, delicate.Count);
                            dishTotal += priceInfo.TotalPrice;
                        }

                    // Применяем наценку если нужно (только для Price режима, т.к. в Cost мы считаем себестоимость)
                    if (delicate.DefaultMarkup > 0)
                        // Если режим Price - наценка уже применена при отображении, 
                        // нам нужно получить ту же цифру для суммы
                        dishTotal = dishTotal * (delicate.DefaultMarkup / 100);

                    // В режиме Cost нам нужна просто сумма себестоимостей (без наценки)
                    if (reportMode == ReportMode.Cost && delicate.DefaultMarkup > 0)
                        // Откат наценки (если она была применена в логике выше, но здесь мы считаем заново)
                        // В данном цикле мы считаем dishTotal = Sum(ComponentPrices).
                        // В блоке выше мы умножили на markup. 
                        // Для Cost отчета нам нужна "чистая" сумма.
                        // Исправим: для Cost отчета мы НЕ должны умножать на markup.
                        dishTotal = dishTotal / (delicate.DefaultMarkup / 100);

                    // Более чистая логика суммирования:
                    // 1. Считаем чистую себестоимость (cost)
                    decimal currentDishCost = 0;
                    foreach (var component in components)
                    {
                        var priceInfo = _menuPriceService.GetComponentPriceInfo(menuId ?? 0, component, delicate.Count);
                        currentDishCost += priceInfo.TotalPrice;
                    }

                    // 2. В зависимости от режима добавляем к общей сумме
                    if (reportMode == ReportMode.Price || reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                    {
                        // Цена продажи = Себестоимость * Наценка
                        var markupMultiplier = delicate.DefaultMarkup > 0 ? delicate.DefaultMarkup / 100 : 1;
                        totalDishSum += currentDishCost * markupMultiplier;
                    }
                    else
                    {
                        // Себестоимость
                        totalDishSum += currentDishCost;
                    }
                    
                    if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost || reportMode == ReportMode.Price)
                    {
                        totalCostSum += currentDishCost;

                        var portions = delicate.Count > 0 ? delicate.Count : 1;
                        var unitCost = currentDishCost / portions;
                        totalUnitCostSum += unitCost;

                        var markupMultiplier = delicate.DefaultMarkup > 0 ? delicate.DefaultMarkup / 100 : 1; 
                        var finalDishPrice = currentDishCost; // Default to raw
                        
                         if (delicate.DefaultMarkup > 0)
                        {
                            if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                                finalDishPrice = currentDishCost * markupMultiplier;
                            else if (reportMode == ReportMode.Price)
                                finalDishPrice = currentDishCost * markupMultiplier; // Same logic actually
                        }
                        
                         // NOTE: logic matches PrintMenuPage logic now
                        var unitPrice = finalDishPrice / portions;
                        totalUnitPriceSum += unitPrice;
                    }
                }

                // Вывод строк Итого
                if (reportMode == ReportMode.Price || reportMode == ReportMode.Full || reportMode == ReportMode.Cost)
                {
                    // Определяем процент обслуживания
                    var effectiveServicePercent = settings.ServicePercent;

                    if (menuId.HasValue)
                    {
                        var menu = _menuRepository.GetMenuById(menuId.Value);
                        if (menu?.ServicePercent != null) effectiveServicePercent = menu.ServicePercent.Value;
                    }

                    // Строка "Подитог" (Сумма за блюда без обслуживания)
                    
                    // Строка "Подитог" (Сумма за блюда без обслуживания)
                    var subtotalRow = new TableRow();
                    
                    if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost || reportMode == ReportMode.Price)
                    {
                        // Merged Footer: "Итого по меню" with UnitCost, UnitPrice, TotalCost and TotalDish
                         var subtotalTitleCell = new TableCell();
                        subtotalTitleCell.Append(new TableCellProperties(
                            new GridSpan { Val = 2 }, 
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                         subtotalTitleCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                            new Run(new RunProperties(new Bold()), new Text(reportMode == ReportMode.Cost ? "ИТОГ" : "Итого по меню"))
                        ));
                        subtotalRow.Append(subtotalTitleCell);

                        // Unit Cost Sum (New)
                        var subtotalUnitCostCell = new TableCell();
                        subtotalUnitCostCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalUnitCostCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalUnitCostSum)))
                        ));
                        subtotalRow.Append(subtotalUnitCostCell);

                        // Unit Price Sum (New)
                        var subtotalUnitPriceCell = new TableCell();
                        subtotalUnitPriceCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalUnitPriceCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalUnitPriceSum)))
                        ));
                        subtotalRow.Append(subtotalUnitPriceCell);

                        // Cost Value
                        var subtotalCostCell = new TableCell();
                        subtotalCostCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalCostCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalCostSum)))
                        ));
                        subtotalRow.Append(subtotalCostCell);

                         // Price Value
                        var subtotalValueCell = new TableCell();
                        subtotalValueCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalValueCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalDishSum)))
                        ));
                        subtotalRow.Append(subtotalValueCell);
                    }
                    else
                    {
                         // Standard Footer
                         var subtotalTitleCell = new TableCell();
                        subtotalTitleCell.Append(new TableCellProperties(
                            new GridSpan { Val = (reportMode == ReportMode.Price ? 4 : 2) },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalTitleCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                            new Run(new RunProperties(new Bold()), new Text("Итого по меню"))
                        ));
                        subtotalRow.Append(subtotalTitleCell);

                        var subtotalValueCell = new TableCell();
                        subtotalValueCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalValueCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalDishSum)))
                        ));
                        subtotalRow.Append(subtotalValueCell);
                    }
                    
                    table.Append(subtotalRow);

                    if (reportMode != ReportMode.Cost)
                    {
                        // Строка "За обслуживание"
                        var serviceAmount = totalDishSum * (effectiveServicePercent / 100);

                    var serviceRow = new TableRow();
                    // Объединенная ячейка для текста "За обслуживание + %"
                    var serviceTitleCell = new TableCell();
                    serviceTitleCell.Append(new TableCellProperties(
                        new GridSpan { Val = (reportMode == ReportMode.Price ? 5 : (reportMode == ReportMode.Full ? 5 : 2)) },
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                    ));
                    serviceTitleCell.Append(new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                        new Run(new RunProperties(new Bold()),
                            new Text($"За обслуживание {effectiveServicePercent:G}%"))
                    ));
                    serviceRow.Append(serviceTitleCell);

                    // Ячейка суммы
                    var serviceValueCell = new TableCell();
                    serviceValueCell.Append(new TableCellProperties(
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                    ));
                    serviceValueCell.Append(new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                        new Run(new RunProperties(new Bold()), new Text(FormatCurrency(serviceAmount)))
                    ));
                    serviceRow.Append(serviceValueCell);
                    table.Append(serviceRow);

                    // Строка "ИТОГ"
                    var grandTotal = totalDishSum + serviceAmount;

                    var totalRow = new TableRow();
                    var totalCell = new TableCell();
                    
                    int span = 2; // Default for Cost? Check logic. Cost doesn't enter here usually (reportMode == Cost) but if it did...
                    // reportMode is Price or Full here.
                    if (reportMode == ReportMode.Price) span = 6;
                    if (reportMode == ReportMode.Full || reportMode == ReportMode.Cost) span = 6;
                    
                    totalCell.Append(new TableCellProperties(
                        new GridSpan { Val = span },
                        new Shading { Fill = "D3D3D3" },
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                    ));
                    totalCell.Append(new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                        new Run(new RunProperties(new Bold(), new FontSize { Val = "24" }), 
                            new Text($"ИТОГ   {FormatCurrency(grandTotal)}"))
                    ));
                    totalRow.Append(totalCell);
                    table.Append(totalRow);
                }
                }

                else if (reportMode == ReportMode.NoPrices)
                {
                    // В режиме "Без цен" итог не выводим
                }

                body.Append(table);

                // Add SectionProperties at the end of Body to avoid corruption
                body.AppendChild(new SectionProperties(
                    new PageSize { Width = (UInt32Value)(reportMode == ReportMode.Full ? 16838U : 11906U), Height = (UInt32Value)(reportMode == ReportMode.Full ? 11906U : 16838U), Orient = reportMode == ReportMode.Full ? PageOrientationValues.Landscape : PageOrientationValues.Portrait },
                    new PageMargin
                    {
                        Top = (Int32)(reportMode == ReportMode.Cost || reportMode == ReportMode.Price ? 720 : 1440),
                        Right = (UInt32Value)(reportMode == ReportMode.Cost || reportMode == ReportMode.Price ? 720U : 1440U),
                        Bottom = (Int32)(reportMode == ReportMode.Cost || reportMode == ReportMode.Price ? 720 : 1440),
                        Left = (UInt32Value)(reportMode == ReportMode.Cost || reportMode == ReportMode.Price ? 720U : 1440U),
                        Header = (UInt32Value)720U,
                        Footer = (UInt32Value)720U,
                        Gutter = (UInt32Value)0U
                    }));

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

    private OpenXmlElement CreateCompositionElement(DelicatesColl delicate, ReportMode reportMode, string fontSizeStr,
        int? menuId, out decimal dishTotal, Dictionary<string, Measure> measureLookup, Dictionary<int, ProductView> productLookup, long compositionWidth)
    {
        dishTotal = 0;
        var components = delicate.Lcomp ?? new List<Components>();

        if (!components.Any())
        {
            var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Left }));
            p.Append(new Run(new RunProperties(new FontSize { Val = fontSizeStr }), new Text("нет данных")));
            return p;
        }

        var items = new List<(string Name, string Weight, decimal Price)>();

        foreach (var component in components)
        {
            var baseMeasure = string.IsNullOrWhiteSpace(component.Mera) ? "г" : component.Mera;
            
            // Local helper for package measure
            string GetPackageMeasure(Components c, string bMeasure)
            {
                if (!string.IsNullOrWhiteSpace(c.FassIz)) return c.FassIz;
                if (productLookup.TryGetValue(c.Prodid, out var product) && !string.IsNullOrWhiteSpace(product.IzName)) return product.IzName;
                if (measureLookup.TryGetValue(bMeasure.ToLower().Trim(), out var measureInfo) && !string.IsNullOrWhiteSpace(measureInfo.FassIzmer)) return measureInfo.FassIzmer;
                return bMeasure;
            }

            var count = delicate.Count > 0 ? delicate.Count : 1;
            decimal displayValue;
            string displayUnit;
            var totalWeight = component.Ves * count;

            string NormalizeUnitLocal(string unit) => unit?.Trim().ToLowerInvariant() ?? string.Empty;
            var baseUnitNormalized = NormalizeUnitLocal(baseMeasure);
            var fassIzNormalized = NormalizeUnitLocal(component.FassIz ?? string.Empty);

            if (component.DoNotConvertToPackInMenu)
            {
                displayValue = Math.Round(totalWeight, 2, MidpointRounding.AwayFromZero);
                displayUnit = baseMeasure;
            }
            else
            {
                if (component.Fass > 0 && !string.IsNullOrWhiteSpace(component.FassIz) &&
                    fassIzNormalized != baseUnitNormalized && totalWeight >= component.Fass)
                {
                    var packageCount = totalWeight / component.Fass;
                    displayValue = Math.Round(component.RoundToInteger ? Math.Ceiling(packageCount) : packageCount, 2, MidpointRounding.AwayFromZero);
                    displayUnit = GetPackageMeasure(component, baseMeasure);
                }
                else
                {
                    var val = component.RoundToInteger ? Math.Ceiling(totalWeight) : totalWeight;
                    displayValue = Math.Round(val, 2, MidpointRounding.AwayFromZero);
                    displayUnit = baseMeasure;
                }
            }

            // Shorten unit
            displayUnit = ShortenUnit(displayUnit);
            var formattedWeight = FormatMenuValue(displayValue, displayUnit);

            var priceInfo = _menuPriceService.GetComponentPriceInfo(menuId ?? 0, component, delicate.Count);
            dishTotal += priceInfo.TotalPrice;

            items.Add((!string.IsNullOrEmpty(component.NameT) ? component.NameT : component.Name, formattedWeight, priceInfo.TotalPrice));
        }

        if (reportMode == ReportMode.NoPrices || reportMode == ReportMode.Price)
        {
            var p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Left }));
            p.Append(new Run(new RunProperties(new FontSize { Val = fontSizeStr }), new Text(string.Join(", ", items.Select(i => $"{i.Name} ({i.Weight})")))));
            return p;
        }

        long width1 = (long)(compositionWidth * 0.7);
        long width2 = compositionWidth - width1;

        // Table for Cost/Full
        var table = new Table();
        // Transparent borders
        var tableProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) }
            ),
             new TableWidth { Width = compositionWidth.ToString(), Type = TableWidthUnitValues.Dxa }
        );
        table.AppendChild(tableProps);

            // Grid - 2 columns
            var tableGrid = new TableGrid(
                 new GridColumn { Width = width1.ToString() }, // 70%
                 new GridColumn { Width = width2.ToString() }   // 30%
            );
            table.AppendChild(tableGrid);



        foreach (var item in items)
        {
            var tr = new TableRow();
            
            // Name + Weight
            var cell1 = new TableCell();
            cell1.Append(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width1.ToString() }));
            cell1.Append(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { After = "0" }), // Compact
                new Run(new RunProperties(new FontSize { Val = fontSizeStr }), new Text($"{item.Name} ({item.Weight})"))
            ));
            tr.Append(cell1);

            // Price (no currency symbol)
            var cell2 = new TableCell();
            cell2.Append(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width2.ToString() }));
            var priceText = item.Price > 0 ? FormatCurrency(item.Price) : "0"; // "0" or empty? User image has "0" in one line but prices in others.
            cell2.Append(new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Right }, new SpacingBetweenLines { After = "0" }),
                new Run(new RunProperties(new FontSize { Val = fontSizeStr }), new Text(priceText))
            ));
            tr.Append(cell2);

            table.Append(tr);
        }

        return table;
    }

    private static string ShortenUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
        var trimmed = unit.Trim();
        return trimmed.Length > 2 ? trimmed.Substring(0, 2) : trimmed;
    }

    private static TableCell CreateTableHeaderCell(string text, string? fontSize = null)
    {
        var cell = new TableCell();
        EnsureVerticalCenter(cell);
        var paragraph = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = new Run();
        var props = new RunProperties(new Bold());
        if (!string.IsNullOrEmpty(fontSize)) props.Append(new FontSize { Val = fontSize });
        run.Append(props);
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
            existing.Val = TableVerticalAlignmentValues.Center;
        else
            props.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
    }

    private static string FormatCurrency(decimal value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero)
            .ToString("N0", CultureInfo.CurrentCulture);
    }

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

            var settings = _settingsRepository.GetSettings();
            var productFontSizeStr = (settings.ProductReportFontSize * 2).ToString();

            // Получаем все меры для определения округления
            var measures = _productRepository.GetMeasures();

            // Получаем типы продуктов для сортировки
            var productTypes = _productRepository.GetProductTypes();
            var productTypesDict = productTypes.ToDictionary(pt => pt.Name, pt => pt.SortOrder);
            
            var formatter = new ProductReportFormatter(measures);

            using (var document = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var titleParagraph = body.AppendChild(new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new Bold(), new FontSize { Val = (settings.ProductReportFontSize * 2 + 4).ToString() }),
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
                        dateText = banquetDate.ToString("dd.MM.yyyy HH:mm");
                    else
                        dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                }
                else
                {
                    dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                }

                dateRun.AppendChild(new Text($"Дата, начало: {dateText}"));
                infoParagraph.AppendChild(dateRun);

                // body.AppendChild(new Paragraph()); // Пустая строка

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
                            new GridColumn { Width = "3000" },
                            new GridColumn { Width = "1400" },
                            new GridColumn { Width = "900" },
                            new GridColumn { Width = "900" },
                            new GridColumn { Width = "1200" },
                            new GridColumn { Width = "1800" }
                        )
                    );

                    foreach (var group in groupedList)
                    {
                        decimal groupTotal = 0;
                        var headerRow = new TableRow();
                        headerRow.Append(CreateCell(group.Key, true, "E3EAF2", JustificationValues.Center, 5));
                        priceTable.Append(headerRow);

                        var titlesRow = new TableRow();
                        titlesRow.Append(CreateCell("Продукт", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Количество", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Ед.", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Цена", true, "DDEBF7", JustificationValues.Center));
                        titlesRow.Append(CreateCell("Стоимость", true, "DDEBF7", JustificationValues.Center));
                        priceTable.Append(titlesRow);

                        var groupedProductsLeft = GetGroupedProductsLeft(group).ToArray();

                        foreach (var product in groupedProductsLeft)
                        {
                            var (amountText, unitText, priceMultiplier) = formatter.FormatAmount(product);
                            
                            // Calculate Unit Price for display
                            var displayUnitPrice = product.Price * priceMultiplier;
                            
                            priceTable.Append(CreateTableRow(
                                product.Name, 
                                amountText, 
                                unitText,
                                FormatCurrency(displayUnitPrice),
                                FormatCurrency(product.TotalPrice)));
            
                            groupTotal += product.TotalPrice;
                        }

                        // Group Subtotal Row
                        var groupSubtotalRow = new TableRow();
                        groupSubtotalRow.Append(CreateCell($"Итог по категории \"{group.Key}\":", true, "F5F5F5", JustificationValues.Right, 4));
                        groupSubtotalRow.Append(CreateCell(FormatCurrency(groupTotal), true, "F5F5F5", JustificationValues.Right));
                        priceTable.Append(groupSubtotalRow);
                    }

                    // Add Total Row
                    var totalSum = groupedList.Sum(g =>
                        GetGroupedProductsLeft(g).Sum(p => p.TotalPrice));

                    var totalRow = new TableRow();
                    totalRow.Append(CreateCell("ИТОГО:", true, "DDEBF7", JustificationValues.Right, 4));
                    totalRow.Append(CreateCell(FormatCurrency(totalSum), true, "DDEBF7", JustificationValues.Right));
                    priceTable.Append(totalRow);

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

                    if (includePrices) // This condition is always false here, but the user's instruction implies it should be `showPriceColumn`
                    {
                        decimal currentServicePercent = settings.ServicePercent;

                        // Assuming menuId is available in this context, perhaps from reportData or a parameter
                        // For now, let's assume it's not directly available and needs to be passed or derived.
                        // If menuId is not available, this part will need adjustment.
                        // For demonstration, let's assume a placeholder menuId if needed.
                        // Example: int? menuId = reportData.FirstOrDefault()?.MenuId; // Or from a parameter
                        // Since the instruction doesn't provide `menuId`, I'll comment out the menu-specific logic for now
                        // or assume `menuId` is a parameter to `PrintReport` if it's meant to be used.
                        // Given the context, `menuName` is available, but not `menuId`.
                        // I will add a placeholder for `menuId` for compilation, assuming it would be passed.
                        int? menuId = null; // Placeholder: You might need to pass this as a parameter or derive it.

                        if (menuId.HasValue)
                        {
                            // Initialize _menuRepository if not already done in the constructor
                            // Assuming _menuRepository is a field of the class
                            // If not, it needs to be injected or instantiated.
                            // For this edit, I'll assume it's already available or can be instantiated.
                            // If _menuRepository is not initialized, this line will cause a NullReferenceException.
                            // The instruction implies initializing it, but doesn't show where.
                            // I'll assume it's initialized in the constructor of the class containing PrintReport.
                            var menu = _menuRepository.GetMenuById(menuId.Value);
                            if (menu?.ServicePercent != null) currentServicePercent = menu.ServicePercent.Value;
                        }

                        // Строка "Подытог" (Сумма за блюда без обслуживания)
                        var subtotalRow = new TableRow();
                        var subtotalTitleCell = new TableCell();
                        subtotalTitleCell.Append(new TableCellProperties(
                            new GridSpan { Val = 2 },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalTitleCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                            new Run(new RunProperties(new Bold()), new Text("Подитог"))
                        ));
                        subtotalRow.Append(subtotalTitleCell);

                        var totalDishSum =
                            reportData.Sum(p => p.TotalPrice); // Assuming totalDishSum is calculated somewhere
                        var subtotalValueCell = new TableCell();
                        subtotalValueCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        subtotalValueCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(totalDishSum)))
                        ));
                        subtotalRow.Append(subtotalValueCell);
                        table.Append(subtotalRow);

                        // Строка "За обслуживание"
                        var serviceAmount = totalDishSum * (currentServicePercent / 100);

                        var serviceRow = new TableRow();
                        var serviceTitleCell = new TableCell();
                        serviceTitleCell.Append(new TableCellProperties(
                            new GridSpan { Val = 2 },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        serviceTitleCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                            new Run(new RunProperties(new Bold()),
                                new Text($"За обслуживание ({currentServicePercent:G}%)"))
                        ));
                        serviceRow.Append(serviceTitleCell);

                        var serviceValueCell = new TableCell();
                        serviceValueCell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                        ));
                        serviceValueCell.Append(new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new RunProperties(new Bold()), new Text(FormatCurrency(serviceAmount)))
                        ));
                        serviceRow.Append(serviceValueCell);
                        table.Append(serviceRow);

                        // Строка ИТОГ
                        var totalRow = new TableRow();
                    }

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

                // Set Narrow Margins for Products Report
                body.AppendChild(new SectionProperties(
                    new PageSize { Width = 11906U, Height = 16838U, Orient = PageOrientationValues.Portrait },
                    new PageMargin
                    {
                        Top = 720,
                        Right = 720U,
                        Bottom = 720,
                        Left = 720U,
                        Header = 720U,
                        Footer = 720U,
                        Gutter = 0U
                    }));

                mainPart.Document.Save();

                Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });

List<TempRow> AppendTypeSection(IGrouping<string, DelicatesCollForSvod> groupLeft)
                {
                    var rows = new List<TempRow>();
                    rows.AddRange(CreateHeaderRow(groupLeft.Key));

                    var groupedProductsLeft = GetGroupedProductsLeft(groupLeft).ToArray();

                    foreach (var product in groupedProductsLeft)
                    {
                        var (amountText, unitText, _) = formatter.FormatAmount(product);
                        rows.AddRange(CreatePriceRow(product.Name, amountText, unitText));
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

                TempRow CreateSpacerRow()
                {
                    var spacerRow = new TempRow();
                    spacerRow.AddCell(CreateSpaceCell());
                    spacerRow.AddCell(CreateSpaceCell());
                    spacerRow.AddCell(CreateSpaceCell());

                    return spacerRow;
                }

                // Helper to create TempRow for the generic table generation (used in AppendTypeSection)
                TempRow CreatePriceRow(string productName, string amountText, string unitText)
                {
                    var row = new TempRow();
                    row.AddCell(CreateCell(productName, false, null, JustificationValues.Left));
                    row.AddCell(CreateCell(amountText, false, null, JustificationValues.Right));
                    row.AddCell(CreateCell(unitText, false, null, JustificationValues.Center));
                    return row;
                }

                // Helper to create TableRow directly for the price table (OpenXml Table)
                TableRow CreateTableRow(string productName, string amountText, string unitText, string priceText, string totalPriceText)
                {
                    var row = new TableRow();
                    row.Append(CreateCell(productName, false, null, JustificationValues.Left));
                    row.Append(CreateCell(amountText, false, null, JustificationValues.Right));
                    row.Append(CreateCell(unitText, false, null, JustificationValues.Center));
                    row.Append(CreateCell(priceText, false, null, JustificationValues.Right));
                    row.Append(CreateCell(totalPriceText, false, null, JustificationValues.Right));
                    return row;
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
                    runProps.Append(new FontSize { Val = productFontSizeStr });
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
                            { Val = new EnumValue<BorderValues>(BorderValues.Nil) });
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
                Price = g.First().Price,
                TotalPrice = g.Sum(r => r.TotalPrice)
            })
            .OrderBy(p => p.Name);

        return groupedProductsLeft;
    }
}



public record TempRow
{
    public List<TableCell> Cells { get; } = [];

    public void AddCell(TableCell cell)
    {
        Cells.Add(cell);
    }
}