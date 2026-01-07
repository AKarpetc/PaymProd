# Руководство по миграции из старой версии

Это руководство поможет вам перенести данные из старой версии приложения (на .NET Framework 4.8 с SQL Server CE) в новую
версию (на .NET 9 с SQLite).

## Автоматическая миграция (рекомендуется)

### Способ 1: Использование инструмента миграции

1. Скачайте утилиту миграции: `MigrationTool.exe`
2. Запустите утилиту
3. Укажите путь к старой базе данных `MenuCaolc.mdf`
4. Укажите путь для новой базы данных SQLite
5. Нажмите "Начать миграцию"
6. Дождитесь завершения процесса

## Ручная миграция

### Шаг 1: Экспорт данных из SQL Server CE

1.

Установите [SQL Server Compact Toolbox](https://marketplace.visualstudio.com/items?itemName=ErikEJ.SQLServerCompactSQLiteToolbox)

2. Откройте базу данных `MenuCaolc.mdf`
3. Экспортируйте таблицы в SQL скрипты:
    - Меры (Mera)
    - Типы продуктов (Produkt_Type)
    - Продукты (Producrs)
    - Типы блюд (Type_Del)
    - Блюда (Delicates)
    - Компоненты (Components)
    - Меню (Menus)
    - Связи меню-блюда (Menu_Delicates)

### Шаг 2: Адаптация SQL скриптов для SQLite

SQL Server CE и SQLite имеют некоторые отличия в синтаксисе:

#### Типы данных:

- `INT` → `INTEGER`
- `NVARCHAR(MAX)` → `TEXT`
- `DECIMAL(18,2)` → `REAL`
- `DATETIME` → `TEXT` (формат ISO8601: 'YYYY-MM-DD HH:MM:SS')
- `BIT` → `INTEGER` (0 или 1)

#### IDENTITY колонки:

SQL Server CE:

```sql
[Id] INT IDENTITY(1,1) PRIMARY KEY
```

SQLite:

```sql
Id INTEGER PRIMARY KEY AUTOINCREMENT
```

#### Пример конвертации:

**SQL Server CE:**

```sql
CREATE TABLE Producrs (
    Prod_ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Type INT,
    Ves INT,
    Fass DECIMAL(18,2) DEFAULT 1,
    Priz_menu BIT DEFAULT 0
)
```

**SQLite:**

```sql
CREATE TABLE Producrs (
    Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type INTEGER,
    Ves INTEGER,
    Fass REAL DEFAULT 1,
    Priz_menu INTEGER DEFAULT 0
)
```

### Шаг 3: Импорт данных в SQLite

1. Откройте новое приложение один раз (создастся пустая база данных)
2. Используйте DB Browser for SQLite или командную строку:

```bash
sqlite3 MenuCalc.db < migrated_data.sql
```

3. Проверьте, что все данные импортированы корректно

### Шаг 4: Проверка миграции

После миграции проверьте:

✅ Количество записей в каждой таблице совпадает
✅ Все блюда отображаются со своими компонентами
✅ Меню открываются корректно
✅ Продукты имеют правильные типы и меры
✅ Отчеты формируются без ошибок

## Типичные проблемы и решения

### Проблема 1: Неправильная кодировка текста

**Решение:** При экспорте указывайте кодировку UTF-8.

### Проблема 2: Ошибки с датами

**Решение:** Конвертируйте даты в формат ISO8601:

```sql
-- SQL Server CE
SELECT CONVERT(VARCHAR, DateField, 120) FROM Table

-- Результат: '2025-11-05 14:30:00'
```

### Проблема 3: Нарушение внешних ключей

**Решение:** Импортируйте таблицы в правильном порядке:

1. Mera
2. Produkt_Type
3. Type_Del
4. Producrs
5. Delicates
6. Components
7. Menus
8. Menu_Delicates
9. Components1

## Скрипт автоматической конвертации

Вы можете использовать следующий PowerShell скрипт для базовой конвертации:

```powershell
# convert-to-sqlite.ps1

$sqlServerFile = "path\to\MenuCaolc.mdf"
$outputFile = "migrated_data.sql"

# Экспортируем данные из SQL Server CE
# (требуется SQL Server Compact Tools)

# Конвертируем типы данных
$content = Get-Content $outputFile
$content = $content -replace "INT IDENTITY\(1,1\)", "INTEGER PRIMARY KEY AUTOINCREMENT"
$content = $content -replace "NVARCHAR\(\d+\)", "TEXT"
$content = $content -replace "DECIMAL\(\d+,\d+\)", "REAL"
$content = $content -replace "BIT", "INTEGER"
$content = $content -replace "DATETIME", "TEXT"

Set-Content -Path $outputFile -Value $content

Write-Host "Conversion completed! Import $outputFile into SQLite"
```

## Альтернативный метод: Ручной ввод

Если у вас небольшое количество данных, можно:

1. Запустить новое приложение
2. Создать справочники заново
3. Ввести блюда и их состав
4. Воссоздать меню

Это гарантирует чистую базу данных без ошибок миграции.

## Поддержка

Если у вас возникли проблемы с миграцией:

1. Сохраните копию старой базы данных
2. Опишите проблему
3. Приложите скриншоты ошибок
4. Обратитесь к разработчику

---

**Важно:** Перед началом миграции обязательно создайте резервную копию старой базы данных!

