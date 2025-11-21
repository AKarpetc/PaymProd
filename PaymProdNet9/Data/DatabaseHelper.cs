using Microsoft.Data.Sqlite;
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
                // First try user AppData location (where migration tool puts it)
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PaymProdNet9", "MenuCalc.db");

                // Then try application directory
                var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");

                // Use AppData if it exists, otherwise use bin directory
                var dbPath = File.Exists(appDataPath) ? appDataPath : binPath;

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
        _connectionString = $"Data Source={dbPath}";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        // Создание таблицы мер
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Mera (
                Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name_Mera TEXT NOT NULL,
                Fass_Def REAL DEFAULT 1,
                Fass_Izmer TEXT,
                RoundingPrecision INTEGER DEFAULT 2,
                MenuRoundingPrecision INTEGER DEFAULT 2
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

        // Создание таблицы типов продуктов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Produkt_Type (
                TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_Opis TEXT NOT NULL,
                SortOrder INTEGER DEFAULT 0
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
                SortOrder INTEGER DEFAULT 0
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
                FOREIGN KEY (Del_Type) REFERENCES Type_Del(Type_Del_ID)
            );";
        command.ExecuteNonQuery();

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

        // Инициализация базовых данных
        InitializeDefaultData(connection);
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