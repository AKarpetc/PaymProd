using System;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("\n==========================================");
        Console.WriteLine("  DATABASE MIGRATION: LocalDB -> SQLite");
        Console.WriteLine("==========================================\n");

        var sourceFile = "MenuCaolc.mdf";
        var targetFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9", "MenuCalc.db");

        if (!File.Exists(sourceFile))
        {
            Console.WriteLine($"[ERROR] Source file not found: {sourceFile}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Source: {Path.GetFullPath(sourceFile)}");
        Console.WriteLine($"Target: {targetFile}\n");

        try
        {
            // Ensure target directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            // Delete old database
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
                Console.WriteLine("[OK] Old database removed");
            }

            // Connect to SQL Server LocalDB
            Console.WriteLine("Connecting to LocalDB...");
            var fullPath = Path.GetFullPath(sourceFile);
            var connStr = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={fullPath};Integrated Security=True;Connect Timeout=30";
            using var sourceConn = new SqlConnection(connStr);
            sourceConn.Open();
            Console.WriteLine("[OK] Connected to LocalDB");

            // Create SQLite database
            Console.WriteLine("Creating SQLite database...");
            using var targetConn = new SqliteConnection($"Data Source={targetFile}");
            targetConn.Open();
            Console.WriteLine("[OK] SQLite database created");

            // Create tables
            Console.WriteLine("Creating tables...");
            using (var cmd = targetConn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Menus (Menu_Id INTEGER PRIMARY KEY AUTOINCREMENT, Name_menu TEXT, Count_Human INTEGER, Data_menu TEXT, Opis TEXT, Data_soz TEXT, Data_Red TEXT);
                    CREATE TABLE Delicates (Del_id INTEGER PRIMARY KEY AUTOINCREMENT, Del_Name TEXT, Del_Type INTEGER, Del_Ves REAL, Del_count REAL, Del_opis TEXT, Datew TEXT);
                    CREATE TABLE Producrs (Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Type INTEGER, Ves INTEGER, Fass REAL, Izmer INTEGER);
                    CREATE TABLE Components (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
                    CREATE TABLE Menu_Delicates (id_row INTEGER PRIMARY KEY AUTOINCREMENT, Id_menu INTEGER, Id_delic INTEGER, Count_por REAL);
                    CREATE TABLE Type_Del (Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT, Type_del_opis TEXT);
                    CREATE TABLE Produkt_Type (TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT, Type_Opis TEXT);
                    CREATE TABLE Mera (Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name_Mera TEXT, Fass_Def REAL, Fass_Izmer TEXT);
                    CREATE TABLE Components1 (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
                ";
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("[OK] Tables created\n");

            // Migrate data
            Console.WriteLine("Migrating reference tables...");
            MigrateTable(sourceConn, targetConn, "Type_Del", "Type_Del_ID, Type_del_opis");
            MigrateTable(sourceConn, targetConn, "Produkt_Type", "TypeProdId, Type_Opis");
            MigrateTable(sourceConn, targetConn, "Mera", "Mera_ID, Name_Mera, Fass_Def, Fass_Izmer");

            Console.WriteLine("\nMigrating products and dishes...");
            MigrateTable(sourceConn, targetConn, "Producrs", "Prod_ID, Name, Type, Ves, Fass, Izmer");
            MigrateTable(sourceConn, targetConn, "Delicates", "Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew");
            MigrateTable(sourceConn, targetConn, "Components", "Comp_Id, Delic_id, ProductID, Ves, Detail");

            Console.WriteLine("\nMigrating menus...");
            MigrateTable(sourceConn, targetConn, "Menus", "Menu_Id, Name_menu, Count_Human, Data_menu, Opis, Data_soz, Data_Red");
            MigrateTable(sourceConn, targetConn, "Menu_Delicates", "id_row, Id_menu, Id_delic, Count_por");

            // Copy Components to Components1
            Console.WriteLine("\nCopying to Components1...");
            using (var cmd = targetConn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Components1 SELECT * FROM Components";
                var count = cmd.ExecuteNonQuery();
                Console.WriteLine($"  [OK] Components1: {count} records");
            }

            Console.WriteLine("\n==========================================");
            Console.WriteLine("  MIGRATION COMPLETED SUCCESSFULLY!");
            Console.WriteLine("==========================================\n");
            Console.WriteLine($"Database: {targetFile}");
            Console.WriteLine("\nYou can now run the application!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Migration failed: {ex.Message}");
            Console.WriteLine($"{ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void MigrateTable(SqlConnection source, SqliteConnection target, string tableName, string columns)
    {
        try
        {
            using var sourceCmd = source.CreateCommand();
            sourceCmd.CommandText = $"SELECT {columns} FROM [{tableName}]";
            using var reader = sourceCmd.ExecuteReader();

            var columnArray = columns.Split(',').Select(c => c.Trim()).ToArray();
            var placeholders = string.Join(", ", Enumerable.Range(0, columnArray.Length).Select(i => $"@p{i}"));
            var insertSQL = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders})";

            int count = 0;
            while (reader.Read())
            {
                using var cmd = target.CreateCommand();
                cmd.CommandText = insertSQL;

                for (int i = 0; i < columnArray.Length; i++)
                {
                    var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    cmd.Parameters.AddWithValue($"@p{i}", value);
                }

                cmd.ExecuteNonQuery();
                count++;
            }

            Console.WriteLine($"  [OK] {tableName}: {count} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [WARN] {tableName}: {ex.Message}");
        }
    }
}

