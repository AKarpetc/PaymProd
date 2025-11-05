# Исправления при сборке проекта

## Дата: 5 ноября 2025

### Проблемы и их решения

#### 1. Ошибка с библиотекой Docx
**Проблема:** Не удалось найти тип или имя пространства имен "Docx"

**Решение:** 
- Заменена библиотека `Docx` (версия 5.0.0, несовместимый API) на `DocumentFormat.OpenXml` (версия 3.1.1)
- `DocumentFormat.OpenXml` - это официальная бесплатная библиотека от Microsoft для работы с форматами Office Open XML
- Переписан класс `MenuPrinter.cs` для использования нового API

**Изменения в файлах:**
- `PaymProdNet9.csproj`: Замена пакета Docx на DocumentFormat.OpenXml
- `Services/MenuPrinter.cs`: Полная переработка для использования DocumentFormat.OpenXml API

#### 2. Конфликт версий DocumentFormat.OpenXml
**Проблема:** Обнаружено понижение версии пакета DocumentFormat.OpenXml с 3.1.1 на 3.1.0

**Решение:** 
- Обновлена версия `DocumentFormat.OpenXml` с 3.1.0 до 3.1.1
- Это минимальная версия, требуемая пакетом ClosedXML

#### 3. Отсутствие using для PrintDialog
**Проблема:** Не удалось найти тип или имя пространства имен "PrintDialog"

**Решение:** 
- Добавлен `using System.Windows.Controls;` в файл `ReportWindow.xaml.cs`
- PrintDialog - это стандартный WPF компонент из этого пространства имен

#### 4. Предупреждения о nullable reference
**Проблема:** Предупреждения CS8604 и CS8602 о возможных null-ссылках

**Решение:** 
- Разделены проверки на null для лучшей ясности кода
- Добавлена явная проверка `if (card == null) return;` перед использованием
- Создана промежуточная переменная для `delicateId` с явным приведением типа

**Изменения:**
```csharp
// Было:
var data = card?.DataContext as dynamic;
if (data == null || card == null) return;

// Стало:
if (card == null) return;
var data = card.DataContext as dynamic;
if (data == null) return;
```

## Финальный результат

✅ **Проект успешно собран без ошибок и предупреждений**

### Используемые бесплатные библиотеки:
- **MaterialDesignThemes** (5.1.0) - UI компоненты Material Design
- **MaterialDesignColors** (3.1.0) - цветовые темы для Material Design
- **Microsoft.Data.Sqlite** (9.0.0) - работа с SQLite
- **DocumentFormat.OpenXml** (3.1.1) - создание Word документов
- **ClosedXML** (0.104.2) - работа с Excel файлами
- **System.Data.SQLite** (1.0.119) - провайдер SQLite для ADO.NET

### Команды для работы с проектом:

```powershell
# Восстановление зависимостей
dotnet restore

# Сборка проекта
dotnet build

# Запуск приложения
dotnet run

# Публикация приложения
dotnet publish -c Release -r win-x64 --self-contained
```

## Дополнительная информация

Все коммерческие библиотеки (Telerik UI, Microsoft.Office.Interop.Word) были успешно заменены на бесплатные аналоги с сохранением функциональности приложения.

Приложение полностью мигрировано на .NET 9 и использует современные WPF компоненты.

