## Отчеты и печать

Этот документ описывает:

- `SummaryTablePage` — сводная таблица по меню.
- `ProductsReportPage` — современный отчет по товарам (WPF + Word).
- `ReportPage` / `ReportWindow` — старый отчет по товарам.
- `PrintMenuPage` — отчет по меню (WPF + Word).
- `MenuPrinter` — генерация Word‑документов по меню и по товарам.

Во всех отчетах коэффициенты, фасовка и округление основаны на настройках таблицы `Mera`.

---

### `SummaryTablePage` — сводная таблица компонентов

Описание см. также в `02_menu_and_banquets.md`, здесь — акцент на отчетные аспекты.

- Готовит список `DelicatesCollForSvod` по всем компонентам блюд:

```csharp
var totalWeight = component.Ves * delicate.Countpor;
item.Itog = totalWeight;
item.ItogFass = component.Fass > 0 ? totalWeight / component.Fass : 0;
```

- `ApplyReportRounding()`:
    - `Itog` округляется по `Mera.RoundingPrecision`;
    - `ItogFass` — по мере фасовки (`FassIz`) либо по базовой мере, если `FassIz` не задана.
- Кнопка «Экспорт в Excel»:
    - создает файл с заголовком (банкет, количество гостей, дата/время);
    - выводит все поля `DelicatesCollForSvod`, что удобно для отладки и анализа.

Используется как «сырой» источник данных — именно отсюда берутся агрегаты для отчетов по товарам.

---

### `ProductsReportPage` — современный отчет по товарам

Страница `ProductsReportPage` строит WPF‑документ и Word‑отчет на основе данных `DelicatesCollForSvod`.

#### Входные данные

- `MenuDelicates` — список блюд меню (`ObservableCollection<MenuDel_act>`).
- `BanquetInfo` — `[название, количество гостей, дата/время]`.
- `MenuId` — идентификатор меню (для цен).

#### Генерация WPF‑отчета (`GenerateReport`)

1. Заголовок:

```csharp
var dateText = DateTime.TryParse(_banquetInfo[2], out var date)
    ? date.ToString("dd.MM.yyyy HH:mm")
    : _banquetInfo[2];
HeaderParagraph.Inlines.Add(new Run($"Банкет: {_banquetInfo[0]}"));
HeaderParagraph.Inlines.Add(new Run($"Дата, начало: {dateText}"));
HeaderParagraph.Inlines.Add(new Run($"Количество гостей: {_banquetInfo[1]} человек"));
```

2. `GenerateSummaryData()` — собирает `DelicatesCollForSvod` по всем блюдам и компонентам, учитывая:
    - прямые добавления продуктов (Del_id < 0) — компоненты уже содержат итоговое количество на банкет;
    - компоненты блюд — вес умножается на количество порций (`Countpor`).

3. Данные группируются:
    - сначала по типу продукта (`Type` / `Produkt_Type.SortOrder`);
    - внутри — по названию продукта (`NameT ?? Name`).

4. В зависимости от выбранного режима:
    - **без цен** — строится таблица через `BuildStandardTable` (одна или две колонки с разделителем);
    - **с ценами** — через `BuildSingleColumnTableWithPrices`.

#### Логика округления и расчета цен

- `FormatAmountWithRoundedValue`:
    - определяет, является ли единица дискретной (`шт`, `бут`, `пач` и т.п.) или непрерывной (`г`, `кг`, `л`);
    - для непрерывных единиц:
        - учитывает фасовку (`Fass`, `FassIz`);
        - при необходимости поднимается/опускается по цепочке мер (`г` ↔ `кг` и т.п.);
        - округляет вверх по `MenuRoundingPrecision`.
    - для дискретных единиц:
        - использует количество упаковок (`TotalPackages`) или вес/фасовку.

- `RecalculatePrice(product, roundedAmount, measures)`:
    - вычисляет исходное количество до округления (в весе или упаковках);
    - делит исходную сумму `TotalPrice` на это количество → получает цену за единицу;
    - умножает на **округленное** количество и округляет до 2 знаков.

Это реализует требование: «в отчете по товарам цена должна считаться по *округленному* количеству».

#### Сохранение в Word

Кнопка «Сохранить в Word» вызывает:

```csharp
_menuPrinter.PrintReport(summaryData,
    $"{_banquetInfo[0]}, {_banquetInfo[1]} человек, {_banquetInfo[2]}",
    includePrices: _currentReportWithPrices.Value);
```

Дальнейшая логика описана в разделе `MenuPrinter` ниже.

---

### `ReportPage` и `ReportWindow` — старый отчет по товарам

`ReportPage` и `ReportWindow` реализуют упрощенный вариант отчета по товарам:

- группировка по типу продукта;
- сумма в фасовке или в натуральных единицах;
- округление вверх по `MenuRoundingPrecision`.

Важное отличие от `ProductsReportPage`:

- нет перерасчета цены по округленному количеству;
- меньше учета сложных сценариев фасовки.

Сейчас основным отчетом по товарам является `ProductsReportPage`; `ReportPage`/`ReportWindow` можно использовать как
«быстрый» просмотр.

---

### `PrintMenuPage` — отчет по меню

См. также описание в `02_menu_and_banquets.md`.

Ключевые особенности:

- Блюда группируются по типу (`Type`, `TypeSortOrder`).
- Состав блюда формируется с использованием той же логики мер и фасовки, что и в отчете по товарам (`MenuPrinter`).
- Возможны два варианта:
    - **с ценами** — выводится стоимость каждого блюда, сумма по компонентам;
    - **без цен** — только состав.

**Пример строки состава (логика в `MenuPrinter.BuildComponentLine`):**

```csharp
var line = includePrices
    ? $"{productName} ({formattedWeight}) — {FormatCurrency(priceInfo.TotalPrice)} тг"
    : $"{productName} ({formattedWeight})";
```

---

### `MenuPrinter` — Word‑отчеты по меню и товарам

Класс `MenuPrinter` отвечает за формирование **DOCX** файлов:

- `PrintMenu(List<DelicatesColl> delicates, string menuName, bool includePrices, int? menuId)` — отчет по меню.
- `PrintReport(List<DelicatesCollForSvod> reportData, string menuName, bool includePrices)` — отчет по товарам.

#### Общие моменты

- Файлы сохраняются во временный каталог:

```csharp
var fileName = Path.Combine(Path.GetTempPath(), $"Menu_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
```

- После сохранения файл открывается через `Process.Start` с `UseShellExecute = true`, чтобы запустить ассоциированное
  приложение (обычно Word).

#### Заголовок отчетов

- В `PrintReport` (товары):
    - параметр `menuName` содержит строку вида:  
      `"Название, N человек, 22.11.2025 14:30"`;
    - код пытается извлечь дату из третьей части и отформатировать ее как `dd.MM.yyyy HH:mm`:

```csharp
if (menuName.Contains(","))
{
    var parts = menuName.Split(',');
    if (parts.Length >= 3 && DateTime.TryParse(parts[2].Trim(), out var banquetDate))
        dateText = banquetDate.ToString("dd.MM.yyyy HH:mm");
    else
        dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
}
```

#### Логика единиц и фасовки (общая для меню и товаров)

- `GetBaseMeasure(Components component)`:
    - использует `component.Mera` (основную единицу), а не `product.Ves` — важно, чтобы не путать базовую меру и
      фасовку.
- `GetPackageMeasure`:
    - приоритетно берет `component.FassIz`;
    - если нет, использует дефолт из `Mera` или базовой меры.
- При расчете `displayValue` для строки компонента:
    - если `Fass > 0`, `FassIz` задана и отличается от базовой меры, и суммарное количество ≥ фасовки — значение
      переводится в фасовку;
    - иначе остается в основных единицах.

Эта логика синхронизирована с отчетом по товарам (`ProductsReportPage`) и сводной таблицей.

---

### Практические советы по отладке отчетов

1. **Проверяйте единицы измерения:**
    - убедитесь, что для продукта правильно заданы `Mera`, `Fass`, `FassIz` и точности округления;
    - некорректная настройка мер приводит к «странным» значениям (например, 0.45 кг вместо 450 г).

2. **Сравнивайте отчеты:**
    - если отчет по меню и отчет по товарам показывают разные единицы — смотрите, где используется фасовка, а где нет;
    - отчет по меню считает **себестоимость блюда**, а отчет по товарам — стоимость **закупки** продуктов (с учетом
      округления).

3. **Имейте в виду авто‑добавленные продукты:**
    - хозтовары с флагом `AutoAdd` могут попадать в отчет по товарам, даже если их явно не видно в меню;
    - переключатель «Показать товары» в `CurrentMenuPage` влияет только на отображение, а не на расчет отчетов.




