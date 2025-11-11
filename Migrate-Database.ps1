# Migration script from SQL Server CE to SQLite
# Usage: .\Migrate-Database.ps1

$ErrorActionPreference = "Stop"

Write-Host "`n===========================================================" -ForegroundColor Cyan
Write-Host "    DATABASE MIGRATION: SQL Server CE -> SQLite" -ForegroundColor Green
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
    # Load assemblies
    Write-Host "Loading assemblies..." -ForegroundColor Yellow
    Add-Type -AssemblyName "System.Data"
    
    # Try to load SQL Server CE assembly
    Write-Host "Loading SQL Server Compact..." -ForegroundColor Yellow
    $sqlCeAssembly = $null
    
    # Try loading from GAC
    try {
        Add-Type -AssemblyName "System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"
        $sqlCeAssembly = [System.Reflection.Assembly]::Load("System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91")
        Write-Host "[OK] SQL Server Compact loaded from GAC" -ForegroundColor Green
    }
    catch {
        # Try loading from Program Files
        $sqlCePath = "${env:ProgramFiles}\Microsoft SQL Server Compact Edition\v4.0\Desktop\System.Data.SqlServerCe.dll"
        if (Test-Path $sqlCePath) {
            [System.Reflection.Assembly]::LoadFrom($sqlCePath) | Out-Null
            Write-Host "[OK] SQL Server Compact loaded from: $sqlCePath" -ForegroundColor Green
        }
        else {
            Write-Host "[ERROR] SQL Server Compact 4.0 not found!" -ForegroundColor Red
            Write-Host "`nPlease install SQL Server Compact 4.0:" -ForegroundColor Yellow
            Write-Host "Download: https://www.microsoft.com/download/details.aspx?id=17876" -ForegroundColor Cyan
            Write-Host "File: SSCERuntime_x64-ENU.exe`n" -ForegroundColor Cyan
            pause
            exit 1
        }
    }
    
    # Connect to SQL Server CE
    Write-Host "Connecting to SQL Server CE..." -ForegroundColor Yellow
    $sourceConn = New-Object System.Data.SqlServerCe.SqlCeConnection
    $sourceConn.ConnectionString = "Data Source=$sourceFile"
    
    try {
        $sourceConn.Open()
        Write-Host "[OK] Connected to SQL Server CE" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] Cannot connect to SQL Server CE" -ForegroundColor Red
        Write-Host "SQL Server Compact 4.0 is not installed!" -ForegroundColor Yellow
        Write-Host "`nSolution: Install SQL Server Compact 4.0" -ForegroundColor Cyan
        Write-Host "Download: https://www.microsoft.com/download/details.aspx?id=17876`n" -ForegroundColor Cyan
        pause
        exit 1
    }

    # Create SQLite database
    Write-Host "Creating SQLite database..." -ForegroundColor Yellow
    [Reflection.Assembly]::LoadWithPartialName("System.Data.SQLite") | Out-Null
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
            $placeholders = ($columnArray | ForEach-Object { $i = 0 } { "@p$i"; $i++ }) -join ', '
            $insertSql = "INSERT INTO $tableName ($columns) VALUES ($placeholders)"
            
            while ($reader.Read()) {
                $cmd = $targetConn.CreateCommand()
                $cmd.CommandText = $insertSql
                
                for ($i = 0; $i -lt $columnArray.Length; $i++) {
                    if ($reader.IsDBNull($i)) {
                        $cmd.Parameters.AddWithValue("@p$i", [DBNull]::Value) | Out-Null
                    } else {
                        $cmd.Parameters.AddWithValue("@p$i", $reader.GetValue($i)) | Out-Null
                    }
                }
                
                $cmd.ExecuteNonQuery() | Out-Null
                $count++
            }
            
            $reader.Close()
            Write-Host "  [OK] $tableName : $count records" -ForegroundColor Green
        }
        catch {
            Write-Host "  [ERROR] $tableName : $_" -ForegroundColor Red
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
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[OK] Components1 copied" -ForegroundColor Green

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
    Write-Host $_.Exception.StackTrace -ForegroundColor Red
    pause
    exit 1
}

Write-Host "Press any key to exit..."
pause
