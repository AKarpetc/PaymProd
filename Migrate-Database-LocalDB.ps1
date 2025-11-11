# Migration script from SQL Server LocalDB to SQLite
# Usage: .\Migrate-Database-LocalDB.ps1

$ErrorActionPreference = "Stop"

Write-Host "`n===========================================================" -ForegroundColor Cyan
Write-Host "    DATABASE MIGRATION: SQL Server LocalDB -> SQLite" -ForegroundColor Green
Write-Host "===========================================================`n" -ForegroundColor Cyan

# File paths
$sourceFile = "MenuCaolc.mdf"
$targetFile = "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
$targetDir = Split-Path $targetFile

# Check source file
Write-Host "Checking source file..." -ForegroundColor Yellow
if (-not (Test-Path $sourceFile)) {
    Write-Host "[ERROR] File $sourceFile not found!" -ForegroundColor Red
    Write-Host "Make sure the file is in the current directory.`n"
    pause
    exit 1
}
Write-Host "[OK] Source file found: $sourceFile" -ForegroundColor Green
$fullPath = (Resolve-Path $sourceFile).Path

# Create target directory
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
    Write-Host "[OK] Directory created: $targetDir" -ForegroundColor Green
}

# Remove old database if exists
if (Test-Path $targetFile) {
    Remove-Item $targetFile
    Write-Host "[OK] Old database removed" -ForegroundColor Yellow
}

Write-Host "`nSTARTING MIGRATION...`n" -ForegroundColor Yellow

try {
    # Start LocalDB instance
    Write-Host "Starting SQL Server LocalDB..." -ForegroundColor Yellow
    & sqllocaldb start MSSQLLocalDB 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "[OK] LocalDB started" -ForegroundColor Green
    
    # Load assemblies
    Write-Host "Loading assemblies..." -ForegroundColor Yellow
    Add-Type -AssemblyName "System.Data"
    
    # Connect to SQL Server LocalDB
    Write-Host "Connecting to SQL Server LocalDB..." -ForegroundColor Yellow
    $sourceConnectionString = "Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=$fullPath;Integrated Security=True;Connect Timeout=30"
    $sourceConn = New-Object System.Data.SqlClient.SqlConnection
    $sourceConn.ConnectionString = $sourceConnectionString
    
    try {
        $sourceConn.Open()
        Write-Host "[OK] Connected to SQL Server LocalDB" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] Cannot connect to SQL Server LocalDB" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Yellow
        Write-Host "`nВозможные причины:" -ForegroundColor Cyan
        Write-Host "1. База данных повреждена" -ForegroundColor Yellow
        Write-Host "2. Файл используется другим процессом" -ForegroundColor Yellow
        Write-Host "3. Неверный формат базы данных`n" -ForegroundColor Yellow
        pause
        exit 1
    }

    # Load SQLite
    Write-Host "Loading SQLite..." -ForegroundColor Yellow
    
    # Try to load System.Data.SQLite
    $sqliteLoaded = $false
    
    # Method 1: Try loading from NuGet package (if exists)
    $possiblePaths = @(
        "$PSScriptRoot\PaymProdNet9\bin\Debug\net9.0-windows\System.Data.SQLite.dll",
        "$PSScriptRoot\PaymProdNet9\bin\Release\net9.0-windows\System.Data.SQLite.dll"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            try {
                [System.Reflection.Assembly]::LoadFrom($path) | Out-Null
                $sqliteLoaded = $true
                Write-Host "[OK] SQLite loaded from: $path" -ForegroundColor Green
                break
            }
            catch {
                # Try next path
            }
        }
    }
    
    # Method 2: Try loading from GAC
    if (-not $sqliteLoaded) {
        try {
            [Reflection.Assembly]::LoadWithPartialName("System.Data.SQLite") | Out-Null
            $sqliteLoaded = $true
            Write-Host "[OK] SQLite loaded from GAC" -ForegroundColor Green
        }
        catch {
            Write-Host "[ERROR] Cannot load System.Data.SQLite" -ForegroundColor Red
            Write-Host "`nРешение: Сначала соберите проект" -ForegroundColor Yellow
            Write-Host "cd PaymProdNet9" -ForegroundColor Cyan
            Write-Host "dotnet build" -ForegroundColor Cyan
            Write-Host "cd .." -ForegroundColor Cyan
            Write-Host ".\Migrate-Database-LocalDB.ps1`n" -ForegroundColor Cyan
            pause
            exit 1
        }
    }

    # Create SQLite database
    Write-Host "Creating SQLite database..." -ForegroundColor Yellow
    $targetConn = New-Object System.Data.SQLite.SQLiteConnection
    $targetConn.ConnectionString = "Data Source=$targetFile;Version=3;"
    $targetConn.Open()
    
    Write-Host "[OK] SQLite database created" -ForegroundColor Green

    # Create tables
    Write-Host "Creating table structure..." -ForegroundColor Yellow
    $createScript = @"
CREATE TABLE IF NOT EXISTS Menus (Menu_Id INTEGER PRIMARY KEY AUTOINCREMENT, Name_menu TEXT, Count_Human INTEGER, Data_menu TEXT, Opis TEXT, Data_soz TEXT, Data_Red TEXT);
CREATE TABLE IF NOT EXISTS Delicates (Del_id INTEGER PRIMARY KEY AUTOINCREMENT, Del_Name TEXT, Del_Type INTEGER, Del_Ves REAL, Del_count REAL, Del_opis TEXT, Datew TEXT);
CREATE TABLE IF NOT EXISTS Producrs (Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Type INTEGER, Ves INTEGER, Fass REAL, Izmer INTEGER);
CREATE TABLE IF NOT EXISTS Components (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
CREATE TABLE IF NOT EXISTS Menu_Delicates (id_row INTEGER PRIMARY KEY AUTOINCREMENT, Id_menu INTEGER, Id_delic INTEGER, Count_por REAL);
CREATE TABLE IF NOT EXISTS Type_Del (Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT, Type_del_opis TEXT);
CREATE TABLE IF NOT EXISTS Produkt_Type (TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT, Type_Opis TEXT);
CREATE TABLE IF NOT EXISTS Mera (Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name_Mera TEXT, Fass_Def REAL, Fass_Izmer TEXT);
CREATE TABLE IF NOT EXISTS Components1 (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
"@

    $cmd = $targetConn.CreateCommand()
    $cmd.CommandText = $createScript
    $cmd.ExecuteNonQuery() | Out-Null
    
    Write-Host "[OK] Table structure created`n" -ForegroundColor Green

    # Migration function
    function Migrate-Table {
        param($tableName, $columns)
        
        try {
            $sourceCmd = $sourceConn.CreateCommand()
            $sourceCmd.CommandText = "SELECT $columns FROM [$tableName]"
            $reader = $sourceCmd.ExecuteReader()
            
            $count = 0
            $columnArray = $columns -split ',' | ForEach-Object { $_.Trim() }
            $placeholderList = @()
            for ($i = 0; $i -lt $columnArray.Length; $i++) {
                $placeholderList += '?'
            }
            $placeholders = $placeholderList -join ', '
            $insertSql = "INSERT INTO $tableName ($columns) VALUES ($placeholders)"
            
            while ($reader.Read()) {
                $cmd = $targetConn.CreateCommand()
                $cmd.CommandText = $insertSql
                
                for ($i = 0; $i -lt $columnArray.Length; $i++) {
                    if ($reader.IsDBNull($i)) {
                        $cmd.Parameters.AddWithValue("p$i", [DBNull]::Value) | Out-Null
                    } else {
                        $value = $reader.GetValue($i)
                        $cmd.Parameters.AddWithValue("p$i", $value) | Out-Null
                    }
                }
                
                $cmd.ExecuteNonQuery() | Out-Null
                $count++
            }
            
            $reader.Close()
            Write-Host "  [OK] $tableName : $count records" -ForegroundColor Green
            return $count
        }
        catch {
            Write-Host "  [WARNING] $tableName : $_" -ForegroundColor Yellow
            return 0
        }
    }

    # Migrate data
    Write-Host "Migrating reference tables..." -ForegroundColor Yellow
    Migrate-Table "Type_Del" "Type_Del_ID, Type_del_opis"
    Migrate-Table "Produkt_Type" "TypeProdId, Type_Opis"
    Migrate-Table "Mera" "Mera_ID, Name_Mera, Fass_Def, Fass_Izmer"

    Write-Host "`nMigrating products and dishes..." -ForegroundColor Yellow
    Migrate-Table "Producrs" "Prod_ID, Name, Type, Ves, Fass, Izmer"
    Migrate-Table "Delicates" "Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew"
    Migrate-Table "Components" "Comp_Id, Delic_id, ProductID, Ves, Detail"

    Write-Host "`nMigrating menus..." -ForegroundColor Yellow
    Migrate-Table "Menus" "Menu_Id, Name_menu, Count_Human, Data_menu, Opis, Data_soz, Data_Red"
    Migrate-Table "Menu_Delicates" "id_row, Id_menu, Id_delic, Count_por"

    # Copy Components to Components1
    Write-Host "`nCopying Components to Components1..." -ForegroundColor Yellow
    $cmd = $targetConn.CreateCommand()
    $cmd.CommandText = "INSERT INTO Components1 SELECT * FROM Components"
    $copied = $cmd.ExecuteNonQuery()
    Write-Host "[OK] Components1 copied ($copied records)" -ForegroundColor Green

    $sourceConn.Close()
    $targetConn.Close()

    Write-Host "`n===========================================================" -ForegroundColor Cyan
    Write-Host "           MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "===========================================================`n" -ForegroundColor Cyan
    Write-Host "Database saved to: $targetFile" -ForegroundColor Cyan
    Write-Host "You can now run PaymProdNet9 application`n" -ForegroundColor Yellow

} 
catch {
    Write-Host "`n[ERROR] MIGRATION FAILED: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Press any key to exit..."
pause

