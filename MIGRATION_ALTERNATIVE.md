# Альтернативный способ миграции БД (без SQL Server CE)

## Если не хотите устанавливать SQL Server CE

---

## 🎯 Способ 1: Использование DB Browser for SQLite (Самый простой!)

### Шаг 1: Скачайте инструменты

**DB Browser for SQLite** (бесплатно):
- Сайт: https://sqlitebrowser.org/
- Скачать: DB Browser for SQLite (Windows 64-bit)

**SQLite ODBC Driver** (бесплатно):
- Сайт: http://www.ch-werner.de/sqliteodbc/
- Скачать: sqliteodbc_w64.exe

### Шаг 2: Установите оба инструмента

1. Установите DB Browser for SQLite
2. Установите SQLite ODBC Driver

### Шаг 3: Создайте пустую SQLite базу

1. Запустите DB Browser for SQLite
2. File → New Database
3. Сохраните как: `MenuCalc.db` в `C:\Users\<Ваше имя>\AppData\Local\PaymProdNet9\`

### Шаг 4: Создайте структуру таблиц

1. В DB Browser выберите вкладку **Execute SQL**
2. Скопируйте и выполните следующий SQL:

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

-- Компоненты (копия для совместимости)
CREATE TABLE Components1 (
    Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Delic_id INTEGER,
    ProductID INTEGER,
    Ves REAL,
    Detail TEXT
);
```

3. Нажмите **Execute** (F5)
4. File → Write Changes (Ctrl+S)

### Шаг 5: Готово!

Теперь запустите приложение PaymProdNet9 - оно будет работать с пустой базой.

---

## 🎯 Способ 2: Начать с чистой базы данных

### Самый простой вариант - не мигрировать старые данные!

Если у вас не так много данных в старой базе, проще начать с нуля:

1. Запустите PaymProdNet9 - база создастся автоматически
2. Заполните справочники:
   - **Типы блюд** (Закуски, Горячее, Салаты, Десерты...)
   - **Типы продуктов** (Овощи, Мясо, Рыба...)
   - **Единицы измерения** (кг, л, шт, г, мл...)
3. Добавьте продукты
4. Создайте блюда с составом
5. Формируйте новые меню

**Преимущества:**
- ✅ Нет проблем с миграцией
- ✅ Чистая база без мусора
- ✅ Работает сразу
- ✅ Можно параллельно использовать старое приложение

---

## 🎯 Способ 3: Ручное копирование самых важных данных

Если в старой базе есть ценные данные, можно вручную перенести самое важное:

### Что перенести в первую очередь:

1. **Справочники** (Types и т.д.) - вручную создать в новом приложении
2. **Продукты** - экспортировать в Excel, импортировать
3. **Блюда** - пересоздать самые используемые

### Инструменты для экспорта из SQL Server CE:

**ExportSQLCE** (бесплатно):
- https://exportsqlce.codeplex.com/
- Экспорт в SQL скрипты или CSV

**SQL Server Management Studio** (бесплатно):
- Если у вас установлен
- Можно подключиться к .mdf и экспортировать данные

---

## ⚠️ Если все равно нужна автоматическая миграция

Тогда необходимо установить SQL Server Compact 4.0:

### Где скачать:
https://www.microsoft.com/download/details.aspx?id=17876

### Что скачать:
- **SSCERuntime_x64-ENU.exe** (для 64-бит системы)
- Или **SSCERuntime_x86-ENU.exe** (для 32-бит системы)

### После установки:
```powershell
.\Migrate-Database.ps1
```

---

## 💡 Рекомендация

**Для новых пользователей:** Способ 2 (начать с чистой базы)

**Для продолжающих работу:** Способ 1 (создать пустую базу) + Способ 3 (перенести только важное)

**Для полной миграции:** Установить SQL Server CE + запустить скрипт

---

## ✅ Проверка

После любого способа проверьте:

1. Запустите PaymProdNet9
2. Откройте Справочники
3. Проверьте, что можете:
   - ✅ Создать тип блюда
   - ✅ Создать продукт
   - ✅ Создать блюдо
   - ✅ Создать меню

Если все работает - миграция не обязательна!

---

**Удачи! 🚀**


