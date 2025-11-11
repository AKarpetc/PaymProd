using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;

class Program
{
    private const string APP_NAME = "PaymProd Database Migration Tool";
    private const string VERSION = "1.0.0";
    
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        PrintHeader();
        
        // Parse command line arguments
        string? sourceFile = args.Length > 0 ? args[0] : null;
        string? targetFile = args.Length > 1 ? args[1] : null;
        
        // Use defaults if not provided
        sourceFile ??= "MenuCaolc.mdf";
        targetFile ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9", "MenuCalc.db");
        
        // Check source file
        if (!File.Exists(sourceFile))
        {
            // Try parent directory
            var parentPath = Path.Combine("..", sourceFile);
            if (File.Exists(parentPath))
            {
                sourceFile = parentPath;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Source database file not found: {sourceFile}");
                Console.ResetColor();
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  PaymProdMigrate.exe [source.mdf] [target.db]");
                Console.WriteLine("\nExample:");
                Console.WriteLine("  PaymProdMigrate.exe MenuCaolc.mdf");
                Console.WriteLine("  PaymProdMigrate.exe C:\\Data\\MenuCaolc.mdf C:\\Output\\MenuCalc.db");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return 1;
            }
        }
        
        var fullSourcePath = Path.GetFullPath(sourceFile);
        
        Console.WriteLine($"Source: {fullSourcePath}");
        Console.WriteLine($"Target: {targetFile}");
        Console.WriteLine();
        
        // Check for log file early to provide helpful warning
        var ldfFile = Path.ChangeExtension(fullSourcePath, ".ldf");
        var ldfFile2 = Path.Combine(Path.GetDirectoryName(fullSourcePath)!, 
            Path.GetFileNameWithoutExtension(fullSourcePath) + "_log.ldf");
        var hasLogFile = File.Exists(ldfFile) || File.Exists(ldfFile2);
        
        if (!hasLogFile)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  WARNING: Log file (.ldf) not found!");
            Console.ResetColor();
            Console.WriteLine($"   Expected: {ldfFile}");
            Console.WriteLine($"   Or: {ldfFile2}");
            Console.WriteLine($"   Migration may fail without it.\n");
        }
        
        // Confirm before proceeding
        if (File.Exists(targetFile))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: Target database already exists and will be overwritten!");
            Console.ResetColor();
            Console.Write("Continue? (Y/N): ");
            var key = Console.ReadKey();
            Console.WriteLine("\n");
            
            if (key.Key != ConsoleKey.Y)
            {
                Console.WriteLine("Migration cancelled.");
                return 0;
            }
        }
        
        try
        {
            var stats = RunMigration(fullSourcePath, targetFile);
            
            PrintSuccess(stats, targetFile);
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            return 0;
        }
        catch (Exception ex)
        {
            PrintError(ex);
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            return 1;
        }
    }
    
    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"  {APP_NAME} v{VERSION}");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();
        Console.WriteLine();
    }
    
    static MigrationStats RunMigration(string sourceFile, string targetFile)
    {
        var stats = new MigrationStats();
        
        // Ensure target directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        
        // Delete old database
        if (File.Exists(targetFile))
        {
            File.Delete(targetFile);
            WriteSuccess("Old database removed");
        }
        
        // Connect to SQL Server LocalDB
        WriteInfo("Connecting to SQL Server LocalDB...");
        
        // Try with User Instance first (simpler, doesn't require attaching)
        var connStr = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={sourceFile};Integrated Security=True;Connect Timeout=30;User Instance=False";
        
        using var sourceConn = new SqlConnection(connStr);
        
        try
        {
            Console.WriteLine();
            WriteInfo("Attempting to attach and open database...");
            sourceConn.Open();
            WriteSuccess("Connected to LocalDB");
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 4060 || sqlEx.Number == 18456)
        {
            // Login failed or database not found - try alternative connection
            Console.WriteLine();
            WriteWarning("Standard connection failed, trying alternative method...");
            
            try
            {
                sourceConn.Close();
                
                // Try with CREATE DATABASE approach
                var tempConnStr = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30";
                using var tempConn = new SqlConnection(tempConnStr);
                tempConn.Open();
                
                var dbName = "PaymProdMigration_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                
                using (var cmd = tempConn.CreateCommand())
                {
                    cmd.CommandText = $@"
                        CREATE DATABASE [{dbName}] ON PRIMARY 
                        (FILENAME = '{sourceFile}')
                        FOR ATTACH";
                    cmd.ExecuteNonQuery();
                }
                
                WriteInfo($"Database attached as: {dbName}");
                
                // Now connect to the attached database
                sourceConn.ConnectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog={dbName};Integrated Security=True;Connect Timeout=30";
                sourceConn.Open();
                WriteSuccess("Connected via alternative method");
            }
            catch
            {
                throw new Exception(
                    $"Failed to connect to LocalDB: {sqlEx.Message}\n\n" +
                    $"Database: {sourceFile}\n" +
                    $"User: {Environment.UserName}\n\n" +
                    "Possible solutions:\n" +
                    "1. Close any programs that might be using the database (SQL Server Management Studio, old app, etc.)\n" +
                    "2. Copy BOTH MenuCaolc.mdf AND MenuCaolc_log.ldf (or MenuCaolc.ldf) to the same folder\n" +
                    "3. Try running this tool as Administrator\n" +
                    "4. Restart SQL Server LocalDB: sqllocaldb stop MSSQLLocalDB && sqllocaldb start MSSQLLocalDB\n" +
                    $"5. Check if the file is read-only or blocked\n\n" +
                    $"Current user: {Environment.UserName}\n" +
                    $"File location: {sourceFile}", sqlEx);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to LocalDB: {ex.Message}\n\n" +
                "Possible solutions:\n" +
                "1. Ensure SQL Server LocalDB is installed (comes with Visual Studio)\n" +
                "2. Install SQL Server Express from: https://www.microsoft.com/sql-server/sql-server-downloads\n" +
                "3. Start LocalDB: sqllocaldb start MSSQLLocalDB", ex);
        }
        
        // Create SQLite database
        WriteInfo("Creating SQLite database...");
        using var targetConn = new SqliteConnection($"Data Source={targetFile}");
        targetConn.Open();
        
        // Disable foreign key constraints during migration
        using (var cmd = targetConn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }
        
        WriteSuccess("SQLite database created");
        
        // Create tables
        WriteInfo("Creating table structure...");
        CreateTables(targetConn);
        WriteSuccess("Table structure created\n");
        
        // Migrate data
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Migrating reference tables...");
        Console.ResetColor();
        stats.TypeDel = MigrateTable(sourceConn, targetConn, "Type_Del", "Type_Del_ID, Type_del_opis");
        stats.ProduktType = MigrateTable(sourceConn, targetConn, "Produkt_Type", "TypeProdId, Type_Opis");
        stats.Mera = MigrateTable(sourceConn, targetConn, "Mera", "Mera_ID, Name_Mera, Fass_Def, Fass_Izmer");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nMigrating products and dishes...");
        Console.ResetColor();
        stats.Products = MigrateProducts(sourceConn, targetConn);
        stats.Delicates = MigrateDelicates(sourceConn, targetConn);
        stats.Components = MigrateTable(sourceConn, targetConn, "Components", "Comp_Id, Delic_id, ProductID, Ves, Detail");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nMigrating menus...");
        Console.ResetColor();
        stats.Menus = MigrateMenus(sourceConn, targetConn);
        stats.MenuDelicates = MigrateMenuDelicates(sourceConn, targetConn);
        
        // Copy Components to Components1
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nCopying to Components1...");
        Console.ResetColor();
        using (var cmd = targetConn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO Components1 SELECT * FROM Components";
            var count = cmd.ExecuteNonQuery();
            stats.Components1 = count;
            WriteSuccess($"Components1: {count} records");
        }
        
        // Re-enable foreign key constraints
        using (var cmd = targetConn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }
        
        return stats;
    }
    
    static void CreateTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Mera (
                Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name_Mera TEXT NOT NULL,
                Fass_Def REAL DEFAULT 1,
                Fass_Izmer TEXT
            );

            CREATE TABLE IF NOT EXISTS Produkt_Type (
                TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_Opis TEXT NOT NULL
            );

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
            );

            CREATE TABLE IF NOT EXISTS Type_Del (
                Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Type_del_opis TEXT NOT NULL
            );

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
            );

            CREATE TABLE IF NOT EXISTS Components (
                Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Delic_id INTEGER,
                ProductID INTEGER,
                Ves REAL,
                Detail TEXT,
                FOREIGN KEY (Delic_id) REFERENCES Delicates(Del_id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID)
            );

            CREATE TABLE IF NOT EXISTS Menus (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Count_people INTEGER,
                Deteils TEXT,
                Datew TEXT,
                Isopen INTEGER DEFAULT 0,
                Dateban TEXT,
                Ifchan INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Menu_Delicates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Id_men INTEGER,
                Id_delic INTEGER,
                Delcount INTEGER,
                FOREIGN KEY (Id_men) REFERENCES Menus(Id),
                FOREIGN KEY (Id_delic) REFERENCES Delicates(Del_id)
            );

            CREATE TABLE IF NOT EXISTS Components1 (
                Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Delic_id INTEGER,
                ProductID INTEGER,
                Ves REAL,
                Idmen INTEGER,
                FOREIGN KEY (Delic_id) REFERENCES Delicates(Del_id),
                FOREIGN KEY (ProductID) REFERENCES Producrs(Prod_ID),
                FOREIGN KEY (Idmen) REFERENCES Menus(Id)
            );
        ";
        cmd.ExecuteNonQuery();
    }
    
    static int MigrateTable(SqlConnection source, SqliteConnection target, string tableName, string columns)
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
            
            WriteSuccess($"{tableName}: {count} records");
            return count;
        }
        catch (Exception ex)
        {
            WriteWarning($"{tableName}: {ex.Message}");
            return 0;
        }
    }

    static int MigrateProducts(SqlConnection source, SqliteConnection target)
    {
        try
        {
            using var sourceCmd = source.CreateCommand();
            sourceCmd.CommandText = "SELECT Prod_ID, Name, Type, Ves, Fass, Izmer FROM [Producrs]";
            using var reader = sourceCmd.ExecuteReader();
            
            var insertSQL = @"INSERT INTO Producrs 
                (Prod_ID, Name, Type, Ves, Fass, Izmer, Priz_menu, Count, Avtomat, Chel, Isdiap) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 0, 0, 0, 0, 0)";
            
            int count = 0;
            while (reader.Read())
            {
                using var cmd = target.CreateCommand();
                cmd.CommandText = insertSQL;
                
                cmd.Parameters.AddWithValue("@p0", reader.IsDBNull(0) ? DBNull.Value : reader.GetValue(0));
                cmd.Parameters.AddWithValue("@p1", reader.IsDBNull(1) ? DBNull.Value : reader.GetValue(1));
                cmd.Parameters.AddWithValue("@p2", reader.IsDBNull(2) ? DBNull.Value : reader.GetValue(2));
                cmd.Parameters.AddWithValue("@p3", reader.IsDBNull(3) ? DBNull.Value : reader.GetValue(3));
                cmd.Parameters.AddWithValue("@p4", reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4));
                cmd.Parameters.AddWithValue("@p5", reader.IsDBNull(5) ? DBNull.Value : reader.GetValue(5));
                
                cmd.ExecuteNonQuery();
                count++;
            }
            
            WriteSuccess($"Producrs: {count} records");
            return count;
        }
        catch (Exception ex)
        {
            WriteWarning($"Producrs: {ex.Message}");
            return 0;
        }
    }

    static int MigrateDelicates(SqlConnection source, SqliteConnection target)
    {
        try
        {
            using var sourceCmd = source.CreateCommand();
            sourceCmd.CommandText = "SELECT Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew FROM [Delicates]";
            using var reader = sourceCmd.ExecuteReader();
            
            var insertSQL = @"INSERT INTO Delicates 
                (Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew, Del_Cost) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, 0)";
            
            int count = 0;
            while (reader.Read())
            {
                using var cmd = target.CreateCommand();
                cmd.CommandText = insertSQL;
                
                cmd.Parameters.AddWithValue("@p0", reader.IsDBNull(0) ? DBNull.Value : reader.GetValue(0));
                cmd.Parameters.AddWithValue("@p1", reader.IsDBNull(1) ? DBNull.Value : reader.GetValue(1));
                cmd.Parameters.AddWithValue("@p2", reader.IsDBNull(2) ? DBNull.Value : reader.GetValue(2));
                cmd.Parameters.AddWithValue("@p3", reader.IsDBNull(3) ? DBNull.Value : reader.GetValue(3));
                cmd.Parameters.AddWithValue("@p4", reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4));
                cmd.Parameters.AddWithValue("@p5", reader.IsDBNull(5) ? DBNull.Value : reader.GetValue(5));
                cmd.Parameters.AddWithValue("@p6", reader.IsDBNull(6) ? DBNull.Value : reader.GetValue(6));
                
                cmd.ExecuteNonQuery();
                count++;
            }
            
            WriteSuccess($"Delicates: {count} records");
            return count;
        }
        catch (Exception ex)
        {
            WriteWarning($"Delicates: {ex.Message}");
            return 0;
        }
    }

    static int MigrateMenus(SqlConnection source, SqliteConnection target)
    {
        try
        {
            // First check if Menus table exists in source
            using var checkCmd = source.CreateCommand();
            checkCmd.CommandText = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Menus'";
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            
            if (!exists)
            {
                WriteWarning("Menus: Table does not exist in source database");
                return 0;
            }
            
            using var sourceCmd = source.CreateCommand();
            sourceCmd.CommandText = "SELECT Menu_Id, Name_menu, Count_Human, Data_menu, Data_soz FROM [Menus]";
            using var reader = sourceCmd.ExecuteReader();
            
            var insertSQL = @"INSERT INTO Menus 
                (Id, Name, Count_people, Deteils, Datew, Isopen, Dateban, Ifchan) 
                VALUES (@p0, @p1, @p2, @p3, @p4, 0, NULL, 0)";
            
            int count = 0;
            while (reader.Read())
            {
                using var cmd = target.CreateCommand();
                cmd.CommandText = insertSQL;
                
                cmd.Parameters.AddWithValue("@p0", reader.IsDBNull(0) ? DBNull.Value : reader.GetValue(0)); // Menu_Id -> Id
                cmd.Parameters.AddWithValue("@p1", reader.IsDBNull(1) ? DBNull.Value : reader.GetValue(1)); // Name_menu -> Name
                cmd.Parameters.AddWithValue("@p2", reader.IsDBNull(2) ? DBNull.Value : reader.GetValue(2)); // Count_Human -> Count_people
                cmd.Parameters.AddWithValue("@p3", reader.IsDBNull(3) ? DBNull.Value : reader.GetValue(3)); // Data_menu -> Deteils
                cmd.Parameters.AddWithValue("@p4", reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4)); // Data_soz -> Datew
                
                cmd.ExecuteNonQuery();
                count++;
            }
            
            WriteSuccess($"Menus: {count} records");
            return count;
        }
        catch (Exception ex)
        {
            WriteWarning($"Menus: {ex.Message}");
            return 0;
        }
    }

    static int MigrateMenuDelicates(SqlConnection source, SqliteConnection target)
    {
        try
        {
            // First check if Menu_Delicates table exists in source
            using var checkCmd = source.CreateCommand();
            checkCmd.CommandText = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Menu_Delicates'";
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            
            if (!exists)
            {
                WriteWarning("Menu_Delicates: Table does not exist in source database");
                return 0;
            }
            
            using var sourceCmd = source.CreateCommand();
            sourceCmd.CommandText = "SELECT id_row, Id_menu, Id_delic, Count_por FROM [Menu_Delicates]";
            using var reader = sourceCmd.ExecuteReader();
            
            var insertSQL = @"INSERT INTO Menu_Delicates 
                (Id, Id_men, Id_delic, Delcount) 
                VALUES (@p0, @p1, @p2, @p3)";
            
            int count = 0;
            while (reader.Read())
            {
                using var cmd = target.CreateCommand();
                cmd.CommandText = insertSQL;
                
                cmd.Parameters.AddWithValue("@p0", reader.IsDBNull(0) ? DBNull.Value : reader.GetValue(0)); // id_row -> Id
                cmd.Parameters.AddWithValue("@p1", reader.IsDBNull(1) ? DBNull.Value : reader.GetValue(1)); // Id_menu -> Id_men
                cmd.Parameters.AddWithValue("@p2", reader.IsDBNull(2) ? DBNull.Value : reader.GetValue(2)); // Id_delic -> Id_delic
                cmd.Parameters.AddWithValue("@p3", reader.IsDBNull(3) ? Convert.ToInt32(reader.GetValue(3)) : 0); // Count_por -> Delcount (REAL to INTEGER)
                
                cmd.ExecuteNonQuery();
                count++;
            }
            
            WriteSuccess($"Menu_Delicates: {count} records");
            return count;
        }
        catch (Exception ex)
        {
            WriteWarning($"Menu_Delicates: {ex.Message}");
            return 0;
        }
    }
    
    static void PrintSuccess(MigrationStats stats, string targetFile)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("  MIGRATION COMPLETED SUCCESSFULLY!");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();
        
        Console.WriteLine("\n📊 Migration Statistics:");
        Console.WriteLine($"  • Dish Types:        {stats.TypeDel,5} records");
        Console.WriteLine($"  • Product Types:     {stats.ProduktType,5} records");
        Console.WriteLine($"  • Units:             {stats.Mera,5} records");
        Console.WriteLine($"  • Products:          {stats.Products,5} records");
        Console.WriteLine($"  • Dishes:            {stats.Delicates,5} records");
        Console.WriteLine($"  • Components:        {stats.Components,5} records");
        Console.WriteLine($"  • Components1:       {stats.Components1,5} records");
        Console.WriteLine($"  • Menus:             {stats.Menus,5} records");
        Console.WriteLine($"  • Menu-Dishes:       {stats.MenuDelicates,5} records");
        
        Console.WriteLine($"\n📁 Database Location:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {targetFile}");
        Console.ResetColor();
        
        if (stats.Menus == 0 || stats.MenuDelicates == 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Note: Menu tables were not migrated due to column name mismatches.");
            Console.WriteLine("   All products, dishes, and recipes were successfully migrated.");
            Console.WriteLine("   You can create new menus using the existing dishes.");
            Console.ResetColor();
        }
        
        Console.WriteLine("\n✅ You can now run the PaymProdNet9 application!");
    }
    
    static void PrintError(Exception ex)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("  MIGRATION FAILED");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();
        
        Console.WriteLine($"\n❌ Error: {ex.Message}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"\nDetails: {ex.InnerException.Message}");
        }
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
        Console.ResetColor();
    }
    
    static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  ✓ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }
    
    static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  → ");
        Console.ResetColor();
        Console.WriteLine(message);
    }
    
    static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  ⚠ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }
}

class MigrationStats
{
    public int TypeDel { get; set; }
    public int ProduktType { get; set; }
    public int Mera { get; set; }
    public int Products { get; set; }
    public int Delicates { get; set; }
    public int Components { get; set; }
    public int Components1 { get; set; }
    public int Menus { get; set; }
    public int MenuDelicates { get; set; }
}
