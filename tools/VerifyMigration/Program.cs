using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;

var dbPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.CurrentDirectory, "lastDb", "MenuCalc.db");

dbPath = Path.GetFullPath(dbPath);

if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"[ERROR] Database file not found: {dbPath}");
    return 1;
}

Console.WriteLine($"Verifying migrated data in: {dbPath}");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

int ExecuteInt(string sql)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    var result = cmd.ExecuteScalar();
    return result switch
    {
        null => 0,
        DBNull => 0,
        _ => Convert.ToInt32(result, CultureInfo.InvariantCulture)
    };
}

var tableChecks = new (string Label, string Sql)[]
{
    ("Mera", "SELECT COUNT(*) FROM Mera"),
    ("Produkt_Type", "SELECT COUNT(*) FROM Produkt_Type"),
    ("Producrs", "SELECT COUNT(*) FROM Producrs"),
    ("Delicates", "SELECT COUNT(*) FROM Delicates"),
    ("Components", "SELECT COUNT(*) FROM Components"),
    ("Components1", "SELECT COUNT(*) FROM Components1"),
    ("Menus", "SELECT COUNT(*) FROM Menus"),
    ("Menu_Delicates", "SELECT COUNT(*) FROM Menu_Delicates")
};

Console.WriteLine("\nRow counts:");
foreach (var check in tableChecks)
{
    Console.WriteLine($"  {check.Label,-15} {ExecuteInt(check.Sql),6}");
}

var productMenus = ExecuteInt("SELECT COUNT(*) FROM Menu_Delicates WHERE Id_delic < 0");
Console.WriteLine($"  {"Menu products",-15} {productMenus,6}");

var components = ExecuteInt("SELECT COUNT(*) FROM Components");
var components1 = ExecuteInt("SELECT COUNT(*) FROM Components1");
var componentsMatch = components == components1;
var componentsNote = componentsMatch ? "" : " (Components1 stores per-menu overrides)";
Console.WriteLine($"\nComponents copy matches: {(componentsMatch ? "YES" : "NO")}{componentsNote} ({components} vs {components1})");

var missingProductDefaults = ExecuteInt("""
    SELECT COUNT(*) FROM Producrs
    WHERE Priz_menu IS NULL OR Count IS NULL OR Avtomat IS NULL OR Chel IS NULL OR Isdiap IS NULL
    """);

var missingDelicateDefaults = ExecuteInt("""
    SELECT COUNT(*) FROM Delicates
    WHERE Del_Cost IS NULL
    """);

Console.WriteLine("\nDefault-column sanity:");
Console.WriteLine($"  Producrs nullable defaults : {missingProductDefaults} rows missing values");
Console.WriteLine($"  Delicates cost defaults    : {missingDelicateDefaults} rows missing values");

var negativeWeights = ExecuteInt("SELECT COUNT(*) FROM Components WHERE Ves IS NULL");
Console.WriteLine($"  Components weight nulls    : {negativeWeights} rows");

Console.WriteLine("\nVerification complete.");
return 0;
