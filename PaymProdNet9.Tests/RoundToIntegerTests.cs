using PaymProdNet9.Data;
using PaymProdNet9.Enums;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace PaymProdNet9.Tests
{
    [Collection("Database Tests")]
    public class RoundToIntegerTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _dbPath;
        private readonly MenuRepository _menuRepo;
        private readonly ProductRepository _prodRepo;

        public RoundToIntegerTests(ITestOutputHelper output)
        {
            _output = output;
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Round_{Guid.NewGuid()}.db");
            // We do NOT initialize DB here automatically for all tests, 
            // because strict migration test needs manual setup.
            // But for calculation tests we need it.
            // We will initialize in individual tests or setup if needed.
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { }
            }
        }

        [Fact]
        public void Migration_ShouldAddColumn_And_SetTrueForHouseholdGoods()
        {
            // 1. Manually create Old Schema (without RoundToInteger)
            // We use a fresh connection to create the table manually.
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                
                // Create minimal tables
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE Produkt_Type (
                        TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type_Opis TEXT,
                        SortOrder INTEGER DEFAULT 0
                    );
                    CREATE TABLE Mera (
                        Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name_Mera TEXT,
                        Fass_Def DECIMAL,
                        Fass_Izmer TEXT,
                        MenuRoundingPrecision INTEGER DEFAULT 2
                    );
                    CREATE TABLE Producrs (
                        Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Ves INTEGER,
                        Type INTEGER,
                        Fass DECIMAL,
                        Izmer INTEGER,
                        Priz_menu INTEGER DEFAULT 0,
                        Count DECIMAL,
                        Avtomat INTEGER DEFAULT 0,
                        Chel INTEGER,
                        Isdiap INTEGER,
                        Price REAL,
                        HideInMenu INTEGER DEFAULT 0,
                        IsDeleted INTEGER DEFAULT 0,
                        DoNotConvertToPackInMenu INTEGER DEFAULT 0,
                        FOREIGN KEY (Type) REFERENCES Produkt_Type(TypeProdId),
                        FOREIGN KEY (Ves) REFERENCES Mera(Mera_ID),
                        FOREIGN KEY (Izmer) REFERENCES Mera(Mera_ID)
                    );
                ";
                // Note: RoundToInteger is MISSING
                cmd.ExecuteNonQuery();

                // Insert Test Data
                // Measure
                cmd.CommandText = "INSERT INTO Mera (Name_Mera) VALUES ('pcs'); SELECT last_insert_rowid();";
                var meraId = (long)cmd.ExecuteScalar();

                // Types
                cmd.CommandText = "INSERT INTO Produkt_Type (Type_Opis) VALUES ('Хозтовары'); SELECT last_insert_rowid();";
                var householdTypeId = (long)cmd.ExecuteScalar();

                cmd.CommandText = "INSERT INTO Produkt_Type (Type_Opis) VALUES ('Продукты'); SELECT last_insert_rowid();";
                var foodTypeId = (long)cmd.ExecuteScalar();

                // Products
                // 1. Household item -> Should become RoundToInteger=1
                cmd.CommandText = $"INSERT INTO Producrs (Name, Type, Ves, Izmer) VALUES ('Sponge', {householdTypeId}, {meraId}, {meraId})";
                cmd.ExecuteNonQuery();

                // 2. Food item -> Should stay RoundToInteger=0 (Default)
                cmd.CommandText = $"INSERT INTO Producrs (Name, Type, Ves, Izmer) VALUES ('Apple', {foodTypeId}, {meraId}, {meraId})";
                cmd.ExecuteNonQuery();
            }

            // 2. Run InitializeDatabase (Should trigger Migration)
            DatabaseHelper.InitializeDatabase(_dbPath);

            // 3. Verify Data
            var repo = new ProductRepository(_dbPath);
            var products = repo.GetAllProducts();

            var sponge = products.Find(p => p.Name == "Sponge");
            var apple = products.Find(p => p.Name == "Apple");

            Assert.NotNull(sponge);
            Assert.True(sponge.RoundToInteger, "Household goods should have RoundToInteger = true after migration");

            Assert.NotNull(apple);
            Assert.False(apple.RoundToInteger, "Other goods should have RoundToInteger = false (default)");
        }

        [Fact]
        public void Calculation_ShouldRespectRoundToIntegerFlag()
        {
            // Setup DB
            DatabaseHelper.InitializeDatabase(_dbPath);
            var menuRepo = new MenuRepository(_dbPath);
            var prodRepo = new ProductRepository(_dbPath);
            var service = new MenuPriceService(_dbPath); // Assuming it accepts dbPath via constructor or we mock logic

            // NOTE: MenuPriceService usually uses DatabaseHelper.GetConnection() or takes DbPath.
            // If it has default constructor only, it uses DatabaseHelper's default path.
            // We need to inject path. 
            // Checking MenuPriceService... It has a constructor `public MenuPriceService(string dbPath = null)`.
            // So we can pass _dbPath.

            // 1. Create Data
            int pcsId = prodRepo.AddMeasure("pcs", 1, "pcs");
            int typeId = prodRepo.AddProductType("General", 1);

            // Product A: Rounding TRUE. Price 100 per piece.
            int prodA = prodRepo.AddProduct("RoundItem", pcsId, typeId, 1.0, pcsId, roundToInteger: true);
            prodRepo.UpdateProductPrice(prodA, 100);

            // Product B: Rounding FALSE. Price 100 per piece.
            int prodB = prodRepo.AddProduct("ExactItem", pcsId, typeId, 1.0, pcsId, roundToInteger: false);
            prodRepo.UpdateProductPrice(prodB, 100);

            // Create Menu
            int menuId = menuRepo.CreateMenu("Calc Menu", 10, "", "");
            
            // Save Menu Prices
            prodRepo.SaveMenuProductPrice(menuId, prodA, 100);
            prodRepo.SaveMenuProductPrice(menuId, prodB, 100);

            // Create components in a collection (Simulate logic)
            // Or use service methods.
            // We can test GetComponentPriceInfo directly.
            
            // Case 1: RoundItem. Usage 1.2
            var compA = new Components
            {
                Prodid = prodA,
                Ves = 1.2m, // 1.2 pieces (if Fass=0 or 1)
                Mera = "pcs",
                Fass = 1.0m,
                RoundToInteger = true // Important: Model must have this TRUE
            };

            // Calculate for 1 portion (so total usage is 1.2)
            var priceInfoA = service.GetComponentPriceInfo(menuId, compA, 1);
            
            // Expected: 1.2 rounds to 2 (Ceiling). Price = 2 * 100 = 200.
            Assert.Equal(200m, priceInfoA.TotalPrice);

            // Case 2: RoundItem. Usage 0.1
            compA.Ves = 0.1m; 
            var priceInfoA2 = service.GetComponentPriceInfo(menuId, compA, 1);
            // Expected: 0.1 rounds to 1 (Ceiling). Price = 1 * 100 = 100.
            Assert.Equal(100m, priceInfoA2.TotalPrice);

            // Case 3: ExactItem. Usage 1.2
            var compB = new Components
            {
                Prodid = prodB,
                Ves = 1.2m,
                Mera = "pcs",
                Fass = 1.0m,
                RoundToInteger = false
            };
            var priceInfoB = service.GetComponentPriceInfo(menuId, compB, 1);
            // Expected: 1.2 * 100 = 120.
            Assert.Equal(120m, priceInfoB.TotalPrice);
        }
    }
}
