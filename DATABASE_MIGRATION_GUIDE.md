# Руководство по миграции базы данных

## SQL Server CE → SQLite

---

## 📋 Обзор

Этот документ описывает процесс миграции данных из **MenuCaolc.mdf** (SQL Server CE) в **MenuCalc.db** (SQLite) для приложения PaymProdNet9.

---

## 🎯 Способы миграции

### Способ 1: Автоматическая миграция (PowerShell) ⭐ Рекомендуется

#### Требования:
- Windows PowerShell 5.1 или выше
- SQL Server Compact 4.0 Runtime

#### Шаги:

1. **Установите SQL Server Compact 4.0** (если не установлен):
   - Скачайте: https://www.microsoft.com/en-us/download/details.aspx?id=17876
   - Установите: `SSCERuntime_x64-ENU.exe`

2. **Запустите скрипт миграции**:
   ```powershell
   .\Migrate-Database.ps1
   ```

3. **Дождитесь завершения** - скрипт:
   - Прочитает данные из MenuCaolc.mdf
   - Создаст новую SQLite базу
   - Скопирует все данные
   - Покажет статистику

4. **Готово!** База данных будет в:
   ```
   C:\Users\<Пользователь>\AppData\Local\PaymProdNet9\MenuCalc.db
   ```

---

### Способ 2: Ручная миграция через DB Browser

Если автоматическая миграция не работает, используйте графический инструмент.

#### Требования:
- DB Browser for SQLite: https://sqlitebrowser.org/

#### Шаги:

1. **Экспорт из SQL Server CE**:
   
   Используйте SQL Server Management Studio или любой клиент SQL Server CE для экспорта данных в SQL скрипты:
   
   ```sql
   -- Экспортируйте каждую таблицу как INSERT statements
   ```

2. **Создайте новую SQLite базу**:
   
   ```powershell
   # Создайте файл базы данных
   $dbPath = "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
   New-Item -ItemType File -Path $dbPath -Force
   ```

3. **Откройте в DB Browser**:
   - Запустите DB Browser for SQLite
   - File → Open Database → выберите MenuCalc.db

4. **Создайте структуру таблиц** (Execute SQL):
   
   Скопируйте и выполните скрипт из файла `create_tables.sql` (см. ниже)

5. **Импортируйте данные**:
   - File → Import → Table from CSV file
   - Или выполните SQL INSERT statements

---

### Способ 3: Программная миграция (.NET)

Если у вас есть опыт программирования на C#.

#### Создайте консольное приложение:

```csharp
using System.Data.SqlServerCe;
using Microsoft.Data.Sqlite;

// См. файл PaymProdNet9/Tools/DataMigrationTool.cs
```

---

## 📊 Структура таблиц SQLite

### Таблицы для миграции:

| Таблица | Описание | Примерное кол-во |
|---------|----------|------------------|
| `Menus` | Меню банкетов | 10-100 |
| `Delicates` | Справочник блюд | 50-500 |
| `Producrs` | Справочник продуктов | 100-1000 |
| `Components` | Состав блюд | 200-2000 |
| `Menu_Delicates` | Связь меню-блюда | 100-1000 |
| `Type_Del` | Типы блюд | 5-20 |
| `Produkt_Type` | Типы продуктов | 10-30 |
| `Mera` | Единицы измерения | 5-15 |

### SQL скрипт создания таблиц:

```sql
-- Меню
CREATE TABLE Menus (
    Menu_Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name_menu TEXT,
    Count_Human INTEGER,
    Data_menu TEXT,
    Opis TEXT,
    Data_soz TEXT,
    Data_Red TEXT
);

-- Блюда
CREATE TABLE Delicates (
    Del_id INTEGER PRIMARY KEY AUTOINCREMENT,
    Del_Name TEXT,
    Del_Type INTEGER,
    Del_Ves REAL,
    Del_count REAL,
    Del_opis TEXT,
    Datew TEXT
);

-- Продукты
CREATE TABLE Producrs (
    Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    Type INTEGER,
    Ves INTEGER,
    Fass REAL,
    Izmer INTEGER
);

-- Компоненты блюд
CREATE TABLE Components (
    Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Delic_id INTEGER,
    ProductID INTEGER,
    Ves REAL,
    Detail TEXT
);

-- Связь меню и блюд
CREATE TABLE Menu_Delicates (
    id_row INTEGER PRIMARY KEY AUTOINCREMENT,
    Id_menu INTEGER,
    Id_delic INTEGER,
    Count_por REAL
);

-- Типы блюд
CREATE TABLE Type_Del (
    Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Type_del_opis TEXT
);

-- Типы продуктов
CREATE TABLE Produkt_Type (
    TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
    Type_Opis TEXT
);

-- Единицы измерения
CREATE TABLE Mera (
    Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name_Mera TEXT,
    Fass_Def REAL,
    Fass_Izmer TEXT
);

-- Дубликат компонентов для совместимости
CREATE TABLE Components1 (
    Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Delic_id INTEGER,
    ProductID INTEGER,
    Ves REAL,
    Detail TEXT
);
```

---

## ✅ Проверка миграции

После миграции проверьте, что все данные скопированы:

### Используя DB Browser for SQLite:

1. Откройте MenuCalc.db
2. Вкладка "Browse Data"
3. Проверьте каждую таблицу:
   - Убедитесь, что записи есть
   - Проверьте несколько строк на корректность

### Используя SQL запросы:

```sql
-- Проверка количества записей
SELECT 'Menus' as Таблица, COUNT(*) as Записей FROM Menus
UNION ALL
SELECT 'Delicates', COUNT(*) FROM Delicates
UNION ALL
SELECT 'Producrs', COUNT(*) FROM Producrs
UNION ALL
SELECT 'Components', COUNT(*) FROM Components
UNION ALL
SELECT 'Menu_Delicates', COUNT(*) FROM Menu_Delicates
UNION ALL
SELECT 'Type_Del', COUNT(*) FROM Type_Del
UNION ALL
SELECT 'Produkt_Type', COUNT(*) FROM Produkt_Type
UNION ALL
SELECT 'Mera', COUNT(*) FROM Mera;
```

### Тестирование в приложении:

1. Запустите PaymProdNet9
2. Откройте Справочники → Правка справочников
3. Проверьте:
   - ✅ Видны типы блюд
   - ✅ Видны блюда
   - ✅ Видны продукты
   - ✅ У блюд есть состав
4. Откройте главное окно
5. Проверьте:
   - ✅ Видны сохраненные меню
   - ✅ Можно создать новое меню

---

## 🐛 Устранение проблем

### Ошибка: "SQL Server Compact 4.0 не установлен"

**Решение:**
1. Скачайте: https://www.microsoft.com/en-us/download/details.aspx?id=17876
2. Установите `SSCERuntime_x64-ENU.exe`
3. Перезапустите PowerShell
4. Повторите миграцию

### Ошибка: "Файл MenuCaolc.mdf не найден"

**Решение:**
1. Убедитесь, что файл находится в папке `C:\My\menu\PaymProd\`
2. Скопируйте скрипт миграции в эту же папку
3. Запустите из этой папки

### Ошибка: "Access denied" или "Permission denied"

**Решение:**
1. Закройте все программы, которые могут использовать базу
2. Запустите PowerShell от имени администратора
3. Повторите миграцию

### Ошибка: "Table already exists"

**Решение:**
1. Удалите старую базу SQLite:
   ```powershell
   Remove-Item "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
   ```
2. Повторите миграцию

### После миграции данные не отображаются

**Проверьте:**
1. Путь к базе данных правильный
2. В таблицах есть записи (откройте через DB Browser)
3. У типов блюд и продуктов есть записи (они обязательны)

---

## 📝 Альтернативные инструменты

Если ничего не работает, попробуйте:

### 1. SQLite Expert Personal (Free)
- Скачать: http://www.sqliteexpert.com/download.html
- Поддерживает импорт из различных источников

### 2. DBeaver Community (Free)
- Скачать: https://dbeaver.io/download/
- Универсальный клиент БД с миграцией

### 3. HeidiSQL (Free)
- Скачать: https://www.heidisql.com/download.php
- Поддерживает экспорт/импорт между СУБД

---

## ❓ Часто задаваемые вопросы

**Q: Можно ли использовать старую базу SQL Server CE в новом приложении?**  
A: Нет, новое приложение на .NET 9 использует только SQLite.

**Q: Потеряются ли данные при миграции?**  
A: Нет, скрипт копирует все данные. Оригинальный файл не изменяется.

**Q: Нужно ли удалять старую базу после миграции?**  
A: Нет, можете оставить её как резервную копию.

**Q: Можно ли мигрировать только часть данных?**  
A: Да, можно модифицировать скрипт, чтобы копировать только нужные таблицы.

**Q: Как часто нужно повторять миграцию?**  
A: Только один раз. После миграции работайте с новой базой SQLite.

---

## 📞 Поддержка

Если миграция не работает:

1. Проверьте этот документ на ошибки в шагах
2. Посмотрите логи ошибок
3. Попробуйте альтернативные способы
4. Используйте графические инструменты

---

**Дата:** 5 ноября 2025  
**Версия:** 1.0  
**Для приложения:** PaymProdNet9 v2.0


