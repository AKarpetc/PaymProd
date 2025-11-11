# 🔧 PaymProd Migration Tool - Complete Guide

## Overview

The **PaymProd Migration Tool** is a standalone console application that migrates your database from SQL Server LocalDB to SQLite.

## 📁 Location

```
MigrationTool/
├── Program.cs              # Main application code
├── MigrationTool.csproj    # Project file
├── README.md               # Detailed documentation
├── build-standalone.bat    # Build standalone executable
└── run-migration.bat       # Quick run script
```

---

## 🚀 Quick Start

### Option 1: Run Directly (Requires .NET SDK)

From project root:

```powershell
dotnet run --project MigrationTool
```

Or use the provided batch file:

```powershell
.\MIGRATE-DATABASE.bat
```

### Option 2: Build Standalone Executable

Build a single `.exe` file that can run without .NET SDK:

```powershell
.\build-migration-tool.bat
```

This creates `PaymProdMigrate.exe` in the project root.

Then run:

```powershell
.\PaymProdMigrate.exe
```

---

## 📋 Features

### ✅ What It Does

- Connects to SQL Server LocalDB
- Reads data from `MenuCaolc.mdf`
- Creates new SQLite database
- Migrates all tables and data
- Shows detailed statistics
- Provides colored console output
- Handles errors gracefully

### 📊 What Gets Migrated

- ✅ **5** Dish Types (Type_Del)
- ✅ **14** Product Types (Produkt_Type)
- ✅ **7** Units of Measurement (Mera)
- ✅ **104** Products (Producrs)
- ✅ **32** Dishes (Delicates)
- ✅ **164** Recipe Components (Components)
- ⚠️ Menus (if column names match)

---

## 💻 Usage Examples

### Basic Usage

Place `PaymProdMigrate.exe` in the same folder as `MenuCaolc.mdf` and double-click it.

Or from command line:

```powershell
PaymProdMigrate.exe
```

### Custom Source File

```powershell
PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf"
```

### Custom Source and Target

```powershell
PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf" "C:\Output\MenuCalc.db"
```

### From Development Environment

```powershell
cd MigrationTool
dotnet run
```

---

## 🏗️ Building from Source

### Prerequisites

- .NET 9.0 SDK
- Windows 10/11 x64

### Build for Development

```powershell
cd MigrationTool
dotnet build
```

### Build Standalone Executable

From project root:

```powershell
.\build-migration-tool.bat
```

Or manually:

```powershell
cd MigrationTool
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The executable will be in:
```
MigrationTool\bin\Release\net9.0\win-x64\publish\PaymProdMigrate.exe
```

### Build Options

The tool is configured to build as:
- ✅ Single-file executable
- ✅ Self-contained (includes .NET runtime)
- ✅ Windows x64 only
- ✅ ~70-80 MB file size

---

## 📸 Example Output

```
============================================================
  PaymProd Database Migration Tool v1.0.0
============================================================

Source: C:\My\menu\PaymProd\MenuCaolc.mdf
Target: C:\Users\User\AppData\Local\PaymProdNet9\MenuCalc.db

  → Connecting to SQL Server LocalDB...
  ✓ Connected to LocalDB
  → Creating SQLite database...
  ✓ SQLite database created
  → Creating table structure...
  ✓ Table structure created

Migrating reference tables...
  ✓ Type_Del: 5 records
  ✓ Produkt_Type: 14 records
  ✓ Mera: 7 records

Migrating products and dishes...
  ✓ Producrs: 104 records
  ✓ Delicates: 32 records
  ✓ Components: 164 records

Migrating menus...
  ✓ Menus: 0 records
  ✓ Menu_Delicates: 0 records

Copying to Components1...
  ✓ Components1: 164 records

============================================================
  MIGRATION COMPLETED SUCCESSFULLY!
============================================================

📊 Migration Statistics:
  • Dish Types:            5 records
  • Product Types:        14 records
  • Units:                 7 records
  • Products:            104 records
  • Dishes:               32 records
  • Components:          164 records
  • Components1:         164 records
  • Menus:                 0 records
  • Menu-Dishes:           0 records

📁 Database Location:
  C:\Users\User\AppData\Local\PaymProdNet9\MenuCalc.db

⚠️  Note: Menu tables were not migrated due to column name mismatches.
   All products, dishes, and recipes were successfully migrated.
   You can create new menus using the existing dishes.

✅ You can now run the PaymProdNet9 application!

Press any key to exit...
```

---

## 🔍 Troubleshooting

### ❌ Error: "Source database file not found"

**Problem:** The tool can't find `MenuCaolc.mdf`

**Solutions:**
1. Copy `MenuCaolc.mdf` to the same folder as the executable
2. Provide the full path: `PaymProdMigrate.exe "C:\Path\To\MenuCaolc.mdf"`
3. Run from the correct directory

---

### ❌ Error: "Failed to connect to LocalDB"

**Problem:** SQL Server LocalDB is not installed or not running

**Solutions:**

**Option 1:** Start LocalDB
```powershell
sqllocaldb start MSSQLLocalDB
```

**Option 2:** Install SQL Server LocalDB
- Comes with Visual Studio
- Or download SQL Server Express: https://www.microsoft.com/sql-server/sql-server-downloads

**Option 3:** Check if LocalDB is available
```powershell
sqllocaldb info
```

---

### ⚠️ Warning: "Menu tables were not migrated"

**Problem:** Column names in Menus tables don't match expected schema

**Impact:**
- ✅ All products, dishes, and recipes are migrated successfully
- ⚠️ Old menus are not migrated
- ✅ You can create new menus in the application

**Solution (if you need old menus):**
Contact support to investigate the actual column names in your database.

---

### ❌ Error: "Target database already exists"

**Problem:** SQLite database already exists

**What happens:**
- Tool prompts: "Continue? (Y/N)"
- Press `Y` to overwrite
- Press `N` to cancel

**Manual solution:**
Delete the old database:
```
C:\Users\<YourName>\AppData\Local\PaymProdNet9\MenuCalc.db
```

---

## 🔧 Advanced Configuration

### Change Target Location

Edit the default location in code or use command-line:

```powershell
PaymProdMigrate.exe MenuCaolc.mdf "D:\MyData\MenuCalc.db"
```

### Batch Processing

Create a batch file for automated migration:

```batch
@echo off
cd /d "C:\My\menu\PaymProd"
PaymProdMigrate.exe
if errorlevel 1 (
    echo Migration failed!
    pause
    exit /b 1
)
echo Migration successful!
```

---

## 📝 Technical Details

**Language:** C# 11  
**Framework:** .NET 9.0  
**Target:** Windows x64

**Dependencies:**
- Microsoft.Data.SqlClient 6.1.2 (SQL Server connectivity)
- Microsoft.Data.Sqlite 9.0.10 (SQLite connectivity)

**Source Database:**
- Type: SQL Server LocalDB
- Format: .mdf (SQL Server database file)
- Connection: LocalDB instance with file attachment

**Target Database:**
- Type: SQLite 3
- Format: .db file
- Location: `%LOCALAPPDATA%\PaymProdNet9\MenuCalc.db`

---

## 📦 Distribution

### Standalone Executable

The built executable (`PaymProdMigrate.exe`) is:
- ✅ Self-contained (includes .NET runtime)
- ✅ Single file (~70-80 MB)
- ✅ No installation required
- ✅ Can be copied anywhere
- ✅ Runs on Windows x64 without .NET SDK

### Requirements for Running

**On the target machine:**
- Windows 10/11 x64
- SQL Server LocalDB (or SQL Server Express)
- The source `.mdf` file

**NOT required:**
- .NET SDK
- Visual Studio
- NuGet packages (embedded in exe)

---

## 🎯 Use Cases

### 1. One-Time Migration

```powershell
# Build once
.\build-migration-tool.bat

# Run migration
.\PaymProdMigrate.exe

# Delete tool after successful migration
```

### 2. Distributable Tool

```powershell
# Build the tool
.\build-migration-tool.bat

# Copy PaymProdMigrate.exe to a USB drive or network share
# Users can run it on their machines
```

### 3. Development Workflow

```powershell
# Quick test during development
cd MigrationTool
dotnet run

# Modify code, test again
```

---

## 📚 Related Documentation

- **MIGRATION_QUICK_START.md** - Quick migration guide
- **MIGRATION_SUCCESS.md** - Migration completion report
- **DATABASE_MIGRATION_GUIDE.md** - Comprehensive migration guide
- **MigrationTool/README.md** - Tool-specific documentation

---

## ✅ Success Checklist

After running the migration tool:

- [ ] Tool connected to LocalDB successfully
- [ ] SQLite database created
- [ ] All tables migrated (or expected warnings shown)
- [ ] Statistics displayed correctly
- [ ] Database file exists at target location
- [ ] PaymProdNet9 application can open the new database
- [ ] Products and dishes are visible in the application

---

## 🆘 Getting Help

If you encounter issues:

1. Check the console output for specific error messages
2. Review this guide's Troubleshooting section
3. Ensure SQL Server LocalDB is installed and running
4. Verify the source `.mdf` file exists and is accessible
5. Check Windows Event Viewer for SQL Server errors

---

**Built with ❤️ for PaymProd**

*Version 1.0.0*

