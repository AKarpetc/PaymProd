# PaymProd Database Migration Tool

A standalone console application to migrate PaymProd database from SQL Server LocalDB to SQLite.

## Features

- ✅ Migrates all data from SQL Server LocalDB (.mdf) to SQLite (.db)
- ✅ Command-line interface with color-coded output
- ✅ Detailed migration statistics
- ✅ Error handling with helpful messages
- ✅ Can be compiled to a single executable file
- ✅ Supports custom source and target paths

## Requirements

- .NET 9.0 SDK (for building)
- SQL Server LocalDB (for reading source database)
- Windows x64

## Usage

### Basic Usage

Run from the same directory as `MenuCaolc.mdf`:

```bash
PaymProdMigrate.exe
```

### Custom Source File

```bash
PaymProdMigrate.exe C:\Data\MenuCaolc.mdf
```

### Custom Source and Target

```bash
PaymProdMigrate.exe C:\Data\MenuCaolc.mdf C:\Output\MenuCalc.db
```

## Building from Source

### Development Build

```bash
dotnet build
```

### Run without building executable

```bash
dotnet run
```

### Publish as Single Executable

Create a single standalone .exe file:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The executable will be in:
```
bin\Release\net9.0\win-x64\publish\PaymProdMigrate.exe
```

Or use the provided batch file:

```bash
build-standalone.bat
```

## Migration Process

The tool performs the following steps:

1. **Connect** to SQL Server LocalDB and attach the .mdf file
2. **Create** a new SQLite database
3. **Create** table structure in SQLite
4. **Migrate** data from all tables:
   - Reference tables (types, units)
   - Products
   - Dishes and recipes
   - Menus
5. **Display** migration statistics

## Output Example

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

📁 Database Location:
  C:\Users\User\AppData\Local\PaymProdNet9\MenuCalc.db

✅ You can now run the PaymProdNet9 application!
```

## Troubleshooting

### Error: "Source database file not found"

**Solution:** Make sure `MenuCaolc.mdf` is in the same directory as the executable, or provide the full path as a command-line argument.

### Error: "Failed to connect to LocalDB"

**Cause:** SQL Server LocalDB is not installed or not running.

**Solutions:**
1. Install Visual Studio (includes LocalDB)
2. Install SQL Server Express from: https://www.microsoft.com/sql-server/sql-server-downloads
3. Ensure LocalDB is started: `sqllocaldb start MSSQLLocalDB`

### Warning: "Menu tables were not migrated"

**Cause:** The source database uses different column names for the Menus tables.

**Impact:** Products, dishes, and recipes are fully migrated. You can create new menus in the application.

## Project Structure

```
MigrationTool/
├── Program.cs              # Main application code
├── MigrationTool.csproj    # Project configuration
├── README.md               # This file
├── build-standalone.bat    # Build script for standalone executable
└── run-migration.bat       # Quick run script
```

## Technical Details

**Language:** C# 11  
**Framework:** .NET 9.0  
**Dependencies:**
- Microsoft.Data.SqlClient 6.1.2
- Microsoft.Data.Sqlite 9.0.10

**Database:**
- Source: SQL Server LocalDB (attached .mdf file)
- Target: SQLite 3

## License

Copyright © 2025 PaymProd Team

## Support

For issues or questions, please refer to the main PaymProd documentation.

