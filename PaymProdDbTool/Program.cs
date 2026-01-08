using Microsoft.Data.Sqlite;
using System;
using System.IO;

var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaymProdNet9");
var dbPath = Path.Combine(appDataDir, "MenuCalc.db");
var connectionString = $"Data Source={dbPath}";

Console.WriteLine($"Connecting to DB: {dbPath}");

using var connection = new SqliteConnection(connectionString);
connection.Open();

var command = connection.CreateCommand();
command.CommandText = @"
    SELECT p.Prod_ID, p.Name, p.Fass, p.Izmer, p.Ves, 
           m.Name_Mera as IzmerName, m.Fass_Def as IzmerFassDef,
           mVes.Name_Mera as VesName, mVes.Fass_Def as VesFassDef
    FROM Producrs p
    LEFT JOIN Mera m ON p.Izmer = m.Mera_ID
    LEFT JOIN Mera mVes ON p.Ves = mVes.Mera_ID
    WHERE p.Name LIKE '%крахмал%'";

using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"ID: {reader["Prod_ID"]}");
    Console.WriteLine($"Name: {reader["Name"]}");
    Console.WriteLine($"Fass (DB): {reader["Fass"]}");
    Console.WriteLine($"IzmerID: {reader["Izmer"]}");
    Console.WriteLine($"IzmerName: {reader["IzmerName"]}");
    Console.WriteLine($"IzmerFassDef: {reader["IzmerFassDef"]}");
    Console.WriteLine($"VesName: {reader["VesName"]}");
    Console.WriteLine($"VesFassDef: {reader["VesFassDef"]}");
}
