using Microsoft.Data.Sqlite;
using PaymProdNet9.Migrations;
using System.IO;

namespace PaymProdNet9.Data;

/// <summary>
/// Помощник для работы с базой данных SQLite
/// </summary>
public static class DatabaseHelper
{
    private static string? _connectionString;

    public static string ConnectionString
    {
        get
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                // Всегда используем AppData для базы данных (там есть права на запись)
                // Это важно для установленных приложений в Program Files
                var appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PaymProdNet9");
                
                // Убедимся что директория существует
                if (!Directory.Exists(appDataDir))
                {
                    try
                    {
                        Directory.CreateDirectory(appDataDir);
                    }
                    catch (Exception ex)
                    {
                        Services.Logger.Error($"Не удалось создать директорию для базы данных: {appDataDir}", ex);
                        throw;
                    }
                }

                var dbPath = Path.Combine(appDataDir, "MenuCalc.db");
                _connectionString = $"Data Source={dbPath}";
            }

            return _connectionString;
        }
    }

    /// <summary>
    /// Инициализация базы данных и создание таблиц
    /// </summary>
    public static void InitializeDatabase(string dbPath)
    {
        try
        {
            Services.Logger.Info($"Инициализация базы данных: {dbPath}");
            _connectionString = $"Data Source={dbPath}";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Включаем более быстрый режим работы SQLite для локальной базы:
            // - WAL улучшает параллелизм и уменьшает блокировки
            // - synchronous=NORMAL уменьшает количество fsync при сохранении
            try
            {
                var pragmaJournal = connection.CreateCommand();
                pragmaJournal.CommandText = "PRAGMA journal_mode=WAL";
                pragmaJournal.ExecuteNonQuery();

                var pragmaSync = connection.CreateCommand();
                pragmaSync.CommandText = "PRAGMA synchronous=NORMAL";
                pragmaSync.ExecuteNonQuery();
            }
            catch
            {
                // Если по какой-то причине PRAGMA не применились, просто продолжаем с настройками по умолчанию
            }

        var command = connection.CreateCommand();

        // Создание таблицы мер
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Mera (
                Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name_Mera TEXT NOT NULL,
                Fass_Def REAL DEFAULT 1,
                Fass_Izmer TEXT,
                RoundingPrecision INTEGER DEFAULT 2,
                MenuRoundingPrecision INTEGER DEFAULT 2,
                IsDeleted INTEGER DEFAULT 0
            );";
        command.ExecuteNonQuery();

        // Миграция: добавляем RoundingPrecision, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Mera ADD COLUMN RoundingPrecision INTEGER DEFAULT 2";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Миграция: добавляем MenuRoundingPrecision, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Mera ADD COLUMN MenuRoundingPrecision INTEGER DEFAULT 2";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Миграция: добавляем IsDeleted для мер, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Mera ADD COLUMN IsDeleted INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Создание таблицы типов продуктов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Produkt_Type (
                TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_Opis TEXT NOT NULL,
                SortOrder INTEGER DEFAULT 0,
                HideInMenu INTEGER DEFAULT 0,
                IsDeleted INTEGER DEFAULT 0
            );";
        command.ExecuteNonQuery();

        // Миграция: добавляем SortOrder, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Produkt_Type ADD COLUMN SortOrder INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Миграция: добавляем IsDeleted для типов продуктов, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Produkt_Type ADD COLUMN IsDeleted INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Создание таблицы продуктов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Producrs (
                Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Type INTEGER,
                Ves INTEGER NULL,
                Fass REAL DEFAULT 1,
                Izmer INTEGER NOT NULL,
                Priz_menu INTEGER DEFAULT 0,
                Count REAL DEFAULT 0,
                Avtomat INTEGER DEFAULT 0,
                Chel INTEGER DEFAULT 0,
                Isdiap INTEGER DEFAULT 0,
                Price REAL DEFAULT 0,
                HideInMenu INTEGER DEFAULT 0,
                IsDeleted INTEGER DEFAULT 0,
                FOREIGN KEY (Type) REFERENCES Produkt_Type(TypeProdId),
                FOREIGN KEY (Ves) REFERENCES Mera(Mera_ID),
                FOREIGN KEY (Izmer) REFERENCES Mera(Mera_ID)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы типов блюд
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Type_Del (
                Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_del_opis TEXT NOT NULL,
                SortOrder INTEGER DEFAULT 0,
                LinkedProductTypeId INTEGER,
                IsDeleted INTEGER DEFAULT 0
            );";
        command.ExecuteNonQuery();

        // Миграция: добавляем SortOrder, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Type_Del ADD COLUMN SortOrder INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Миграция: добавляем LinkedProductTypeId, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Type_Del ADD COLUMN LinkedProductTypeId INTEGER";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует
        }

        // Миграция: добавляем IsDeleted для типов блюд, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Type_Del ADD COLUMN IsDeleted INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Миграция: добавляем Price, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Producrs ADD COLUMN Price REAL DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем
        }

        // Миграция: флаг "не переводить в фасованные в меню" (на уровне продукта)
        try
        {
            command.CommandText = "ALTER TABLE Producrs ADD COLUMN DoNotConvertToPackInMenu INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует
        }

        // Миграция: добавляем IsDeleted для продуктов, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Producrs ADD COLUMN IsDeleted INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Создание таблицы блюд
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Delicates (
                Del_id INTEGER PRIMARY KEY AUTOINCREMENT,
                Del_Type INTEGER,
                Del_Name TEXT NOT NULL,
                Del_opis TEXT,
                Del_Cost REAL DEFAULT 0,
                Del_Ves REAL DEFAULT 0,
                Del_count REAL DEFAULT 0,
                Datew TEXT,
                LinkedProductId INTEGER,
                AutoAdd INTEGER DEFAULT 0,
                HideInMenu INTEGER DEFAULT 0,
                IsDeleted INTEGER DEFAULT 0,
                FOREIGN KEY (Del_Type) REFERENCES Type_Del(Type_Del_ID)
            );";
        command.ExecuteNonQuery();

        // Миграция: добавляем LinkedProductId, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Delicates ADD COLUMN LinkedProductId INTEGER";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует
        }

        // Миграция: добавляем IsDeleted для блюд, если его нет
        try
        {
            command.CommandText = "ALTER TABLE Delicates ADD COLUMN IsDeleted INTEGER DEFAULT 0";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Колонка уже существует, игнорируем ошибку
        }

        // Создание таблицы компонентов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Components (
                Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Delic_id INTEGER,
                ProductID INTEGER,
                Ves REAL,
                Detail TEXT,
                FOREIGN KEY (Delic_id) REFERENCES Delicates(Del_id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы меню
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Menus (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Count_people INTEGER,
                Deteils TEXT,
                Datew TEXT,
                Isopen INTEGER DEFAULT 0,
                Dateban TEXT,
                Ifchan INTEGER DEFAULT 0
            );";
        command.ExecuteNonQuery();

        // Создание таблицы связи меню и блюд
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Menu_Delicates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Id_men INTEGER,
                Id_delic INTEGER,
                Delcount INTEGER,
                FOREIGN KEY (Id_men) REFERENCES Menus(Id),
                FOREIGN KEY (Id_delic) REFERENCES Delicates(Del_id)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы для измененных компонентов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Components1 (
                Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Delic_id INTEGER,
                ProductID INTEGER,
                Ves REAL,
                Idmen INTEGER,
                FOREIGN KEY (Delic_id) REFERENCES Delicates(Del_id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID),
                FOREIGN KEY (Idmen) REFERENCES Menus(Id)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы для цен продуктов в меню
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Menu_Product_Prices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Id_men INTEGER,
                ProductID INTEGER,
                Price REAL DEFAULT 0,
                FOREIGN KEY (Id_men) REFERENCES Menus(Id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID),
                UNIQUE(Id_men, ProductID)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы для отключения авто-добавления продуктов в конкретных меню
        // Если пользователь вручную удалил продукт с флагом AutoAdd из меню,
        // запись в этой таблице предотвращает его последующее автоматическое добавление
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Menu_AutoProduct_Ignore (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Id_men INTEGER NOT NULL,
                ProductID INTEGER NOT NULL,
                FOREIGN KEY (Id_men) REFERENCES Menus(Id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID),
                UNIQUE(Id_men, ProductID)
            );";
        command.ExecuteNonQuery();

        // Индексы для ускорения выборок и фильтрации
        var indexStatements = new[]
        {
            // Блюда
            @"CREATE INDEX IF NOT EXISTS idx_delicates_type_hide_deleted ON Delicates(Del_Type, HideInMenu, IsDeleted);",
            @"CREATE INDEX IF NOT EXISTS idx_delicates_name ON Delicates(Del_Name);",
            @"CREATE INDEX IF NOT EXISTS idx_delicates_linked_product ON Delicates(LinkedProductId);",

            // Компоненты блюд
            @"CREATE INDEX IF NOT EXISTS idx_components_delic ON Components(Delic_id);",
            @"CREATE INDEX IF NOT EXISTS idx_components_product ON Components(ProductID);",

            // Продукты
            @"CREATE INDEX IF NOT EXISTS idx_products_type ON Producrs(Type);",
            @"CREATE INDEX IF NOT EXISTS idx_products_name ON Producrs(Name);",
            @"CREATE INDEX IF NOT EXISTS idx_products_hide_deleted ON Producrs(HideInMenu, IsDeleted);",

            // Типы блюд
            @"CREATE INDEX IF NOT EXISTS idx_type_del_sort_deleted ON Type_Del(SortOrder, IsDeleted);",

            // Связь меню и блюд
            @"CREATE INDEX IF NOT EXISTS idx_menu_delicates_menu ON Menu_Delicates(Id_men);",
            @"CREATE INDEX IF NOT EXISTS idx_menu_delicates_delic ON Menu_Delicates(Id_delic);",
            @"CREATE INDEX IF NOT EXISTS idx_menu_delicates_menu_delic ON Menu_Delicates(Id_men, Id_delic);",

            // Цены продуктов в меню
            @"CREATE INDEX IF NOT EXISTS idx_menu_product_prices_menu ON Menu_Product_Prices(Id_men);",

            // Игнор авто-добавления продуктов
            @"CREATE INDEX IF NOT EXISTS idx_menu_auto_ignore_menu ON Menu_AutoProduct_Ignore(Id_men);",

            // Меню
            @"CREATE INDEX IF NOT EXISTS idx_menus_isopen ON Menus(Isopen);"
        };

        foreach (var sql in indexStatements)
        {
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        // Миграция: добавляем Menu_Product_Prices, если её нет
        try
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Menu_Product_Prices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Id_men INTEGER,
                    ProductID INTEGER,
                    Price REAL DEFAULT 0,
                    FOREIGN KEY (Id_men) REFERENCES Menus(Id),
                    FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID),
                    UNIQUE(Id_men, ProductID)
                );";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Таблица уже существует, игнорируем ошибку
        }

        // Запускаем миграции (поддержка старых форматов базы данных)
        MigrationRunner.RunAllMigrations(connection);

        // Инициализация базовых данных
        InitializeDefaultData(connection);

        Services.Logger.Debug("База данных успешно инициализирована");
        }
        catch (Exception ex)
        {
            Services.Logger.Error("Критическая ошибка при инициализации базы данных", ex);
            throw;
        }
    }

    /// <summary>
    /// Инициализация базовых справочных данных
    /// </summary>
    private static void InitializeDefaultData(SqliteConnection connection)
    {
        var command = connection.CreateCommand();

        // Проверка и добавление базовых мер
        command.CommandText = "SELECT COUNT(*) FROM Mera";
        var count = Convert.ToInt32(command.ExecuteScalar());

        if (count == 0)
        {
            command.CommandText = @"
                INSERT INTO Mera (Name_Mera, Fass_Def, Fass_Izmer) VALUES 
                ('г', 1, 'г'),
                ('кг', 1000, 'кг'),
                ('л', 1000, 'л'),
                ('мл', 1, 'мл'),
                ('шт', 1, 'шт'),
                ('порция', 1, 'порция');";
            command.ExecuteNonQuery();
        }

        // Проверка и добавление базовых типов продуктов
        command.CommandText = "SELECT COUNT(*) FROM Produkt_Type";
        count = Convert.ToInt32(command.ExecuteScalar());

        if (count == 0)
        {
            command.CommandText = @"
                INSERT INTO Produkt_Type (Type_Opis) VALUES 
                ('Овощи'),
                ('Мясо'),
                ('Рыба'),
                ('Молочные продукты'),
                ('Крупы'),
                ('Напитки'),
                ('Специи'),
                ('Фрукты');";
            command.ExecuteNonQuery();
        }

        // Проверка и добавление базовых типов блюд
        command.CommandText = "SELECT COUNT(*) FROM Type_Del";
        count = Convert.ToInt32(command.ExecuteScalar());

        if (count == 0)
        {
            command.CommandText = @"
                INSERT INTO Type_Del (Type_del_opis) VALUES 
                ('Холодные закуски'),
                ('Горячие закуски'),
                ('Салаты'),
                ('Супы'),
                ('Горячие блюда'),
                ('Гарниры'),
                ('Десерты'),
                ('Напитки');";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Получение соединения с базой данных
    /// </summary>
    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }
}