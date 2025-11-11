# Migration script from SQL Server LocalDB to SQLite
$ErrorActionPreference = "Stop"

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "  DATABASE MIGRATION: LocalDB -> SQLite" -ForegroundColor Green
Write-Host "==========================================`n" -ForegroundColor Cyan

# File paths
$sourceFile = "MenuCaolc.mdf"
$targetFile = "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
$targetDir = Split-Path $targetFile

# Check source file
Write-Host "Checking source file..." -ForegroundColor Yellow
if (-not (Test-Path $sourceFile)) {
    Write-Host "[ERROR] File not found: $sourceFile" -ForegroundColor Red
    pause
    exit 1
}
Write-Host "[OK] Source file found" -ForegroundColor Green
$fullPath = (Resolve-Path $sourceFile).Path

# Create target directory
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
}

# Remove old database
if (Test-Path $targetFile) {
    Remove-Item $targetFile
}

Write-Host "`nStarting migration...`n" -ForegroundColor Yellow

try {
    # Start LocalDB
    Write-Host "Starting LocalDB..." -ForegroundColor Yellow
    sqllocaldb start MSSQLLocalDB 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "[OK] LocalDB started" -ForegroundColor Green
    
    # Connect to SQL Server LocalDB
    Write-Host "Connecting to LocalDB..." -ForegroundColor Yellow
    $connStr = "Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=$fullPath;Integrated Security=True;Connect Timeout=30"
    $sourceConn = New-Object System.Data.SqlClient.SqlConnection
    $sourceConn.ConnectionString = $connStr
    
    try {
        $sourceConn.Open()
        Write-Host "[OK] Connected to LocalDB" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] Cannot connect: $_" -ForegroundColor Red
        pause
        exit 1
    }

    # Load SQLite
    Write-Host "Loading SQLite..." -ForegroundColor Yellow
    $binPath = ".\PaymProdNet9\bin\Debug\net9.0-windows"
    $sqliteDLLs = @(
        "$binPath\SQLitePCLRaw.core.dll",
        "$binPath\SQLitePCLRaw.provider.e_sqlite3.dll",
        "$binPath\SQLitePCLRaw.batteries_v2.dll",
        "$binPath\Microsoft.Data.Sqlite.dll"
    )
    
    $allLoaded = $true
    foreach ($dll in $sqliteDLLs) {
        if (Test-Path $dll) {
            [System.Reflection.Assembly]::LoadFrom((Resolve-Path $dll).Path) | Out-Null
        } else {
            $allLoaded = $false
            break
        }
    }
    
    if ($allLoaded) {
        Write-Host "[OK] SQLite loaded" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] SQLite not found. Run: cd PaymProdNet9; dotnet build; cd .." -ForegroundColor Red
        pause
        exit 1
    }

    # Create SQLite database
    Write-Host "Creating SQLite database..." -ForegroundColor Yellow
    $targetConn = New-Object Microsoft.Data.Sqlite.SqliteConnection
    $targetConn.ConnectionString = "Data Source=$targetFile"
    $targetConn.Open()
    Write-Host "[OK] SQLite database created" -ForegroundColor Green

    # Create tables
    Write-Host "Creating tables..." -ForegroundColor Yellow
    $createSQL = @"
CREATE TABLE Menus (Menu_Id INTEGER PRIMARY KEY AUTOINCREMENT, Name_menu TEXT, Count_Human INTEGER, Data_menu TEXT, Opis TEXT, Data_soz TEXT, Data_Red TEXT);
CREATE TABLE Delicates (Del_id INTEGER PRIMARY KEY AUTOINCREMENT, Del_Name TEXT, Del_Type INTEGER, Del_Ves REAL, Del_count REAL, Del_opis TEXT, Datew TEXT);
CREATE TABLE Producrs (Prod_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Type INTEGER, Ves INTEGER, Fass REAL, Izmer INTEGER);
CREATE TABLE Components (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
CREATE TABLE Menu_Delicates (id_row INTEGER PRIMARY KEY AUTOINCREMENT, Id_menu INTEGER, Id_delic INTEGER, Count_por REAL);
CREATE TABLE Type_Del (Type_Del_ID INTEGER PRIMARY KEY AUTOINCREMENT, Type_del_opis TEXT);
CREATE TABLE Produkt_Type (TypeProdId INTEGER PRIMARY KEY AUTOINCREMENT, Type_Opis TEXT);
CREATE TABLE Mera (Mera_ID INTEGER PRIMARY KEY AUTOINCREMENT, Name_Mera TEXT, Fass_Def REAL, Fass_Izmer TEXT);
CREATE TABLE Components1 (Comp_Id INTEGER PRIMARY KEY AUTOINCREMENT, Delic_id INTEGER, ProductID INTEGER, Ves REAL, Detail TEXT);
"@

    $cmd = $targetConn.CreateCommand()
    $cmd.CommandText = $createSQL
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[OK] Tables created`n" -ForegroundColor Green

    # Migration function
    function Migrate-Table($tableName, $columns) {
        try {
            $sourceCmd = $sourceConn.CreateCommand()
            $sourceCmd.CommandText = "SELECT $columns FROM [$tableName]"
            $reader = $sourceCmd.ExecuteReader()
            
            $columnArray = $columns -split ',' | ForEach-Object { $_.Trim() }
            $paramList = @()
            for ($i = 0; $i -lt $columnArray.Length; $i++) {
                $paramList += "?"
            }
            $params = $paramList -join ", "
            
            $insertSQL = "INSERT INTO $tableName ($columns) VALUES ($params)"
            
            $count = 0
            while ($reader.Read()) {
                $cmd = $targetConn.CreateCommand()
                $cmd.CommandText = $insertSQL
                
                for ($i = 0; $i -lt $columnArray.Length; $i++) {
                    if ($reader.IsDBNull($i)) {
                        $cmd.Parameters.AddWithValue("p$i", [DBNull]::Value) | Out-Null
                    } else {
                        $cmd.Parameters.AddWithValue("p$i", $reader.GetValue($i)) | Out-Null
                    }
                }
                
                $cmd.ExecuteNonQuery() | Out-Null
                $count++
            }
            
            $reader.Close()
            Write-Host "  [OK] $tableName : $count records" -ForegroundColor Green
        }
        catch {
            Write-Host "  [WARN] $tableName : $_" -ForegroundColor Yellow
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
    Write-Host "`nCopying to Components1..." -ForegroundColor Yellow
    $cmd = $targetConn.CreateCommand()
    $cmd.CommandText = "INSERT INTO Components1 SELECT * FROM Components"
    $copied = $cmd.ExecuteNonQuery()
    Write-Host "[OK] Components1: $copied records" -ForegroundColor Green

    $sourceConn.Close()
    $targetConn.Close()

    Write-Host "`n==========================================" -ForegroundColor Cyan
    Write-Host "  MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "==========================================`n" -ForegroundColor Cyan
    Write-Host "Database: $targetFile" -ForegroundColor Cyan
    Write-Host "`nYou can now run the application!" -ForegroundColor Yellow

} 
catch {
    Write-Host "`n[ERROR] Migration failed: $_" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "`nPress any key to exit..."
pause

