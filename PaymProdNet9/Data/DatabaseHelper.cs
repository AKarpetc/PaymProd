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
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");
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
                Fass_Izmer TEXT
            );";
        command.ExecuteNonQuery();

        // Создание таблицы типов продуктов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Produkt_Type (
                TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_Opis TEXT NOT NULL
            );";
        command.ExecuteNonQuery();

        // Создание таблицы продуктов
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Producrs (
                Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Type INTEGER,
                Ves INTEGER,
                Fass REAL DEFAULT 1,
                Izmer INTEGER,
                Priz_menu INTEGER DEFAULT 0,
                Count REAL DEFAULT 0,
                Avtomat INTEGER DEFAULT 0,
                Chel INTEGER DEFAULT 0,
                Isdiap INTEGER DEFAULT 0,
                FOREIGN KEY (Type) REFERENCES Produkt_Type(TypeProdId),
                FOREIGN KEY (Ves) REFERENCES Mera(Mera_ID),
                FOREIGN KEY (Izmer) REFERENCES Mera(Mera_ID)
            );";
        command.ExecuteNonQuery();

        // Создание таблицы типов блюд
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Type_Del (
                Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_del_opis TEXT NOT NULL
            );";
        command.ExecuteNonQuery();

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

