================================================================================
  PAYMPROD DATABASE MIGRATION TOOL
================================================================================

A standalone console application for migrating PaymProd database from
SQL Server LocalDB to SQLite.

================================================================================
  QUICK START
================================================================================

OPTION 1: Run directly (requires .NET 9 SDK)
---------------------------------------------
  From project root:
  
    dotnet run --project MigrationTool
    
  Or simply:
  
    .\MIGRATE-DATABASE.bat


OPTION 2: Build standalone executable
--------------------------------------
  1. Run the build script:
  
       .\build-migration-tool.bat
       
  2. This creates: PaymProdMigrate.exe
  
  3. Run the migration:
  
       .\PaymProdMigrate.exe


================================================================================
  WHAT IT DOES
================================================================================

  1. Connects to your SQL Server LocalDB database (MenuCaolc.mdf)
  2. Creates a new SQLite database
  3. Migrates all your data:
     - Products (104 records)
     - Dishes (32 records)
     - Recipes (164 records)
     - Reference tables (types, units, etc.)
  4. Shows detailed statistics
  5. Ready to use with PaymProdNet9 application!


================================================================================
  LOCATION
================================================================================

  Source Code:
    MigrationTool/
    
  Documentation:
    MIGRATION_TOOL_GUIDE.md   (Complete guide)
    MigrationTool/README.md   (Technical details)
    
  Build Scripts:
    build-migration-tool.bat  (Build standalone exe)
    MIGRATE-DATABASE.bat      (Quick run)


================================================================================
  REQUIREMENTS
================================================================================

  To RUN the tool:
    - Windows 10/11 x64
    - SQL Server LocalDB (or Express)
    - MenuCaolc.mdf file
    
  To BUILD the tool:
    - .NET 9.0 SDK


================================================================================
  FEATURES
================================================================================

  ✓ Single-file executable (~70-80 MB)
  ✓ Self-contained (includes .NET runtime)
  ✓ No installation required
  ✓ Color-coded console output
  ✓ Detailed statistics and error messages
  ✓ Command-line arguments support
  ✓ Confirmation before overwriting existing database


================================================================================
  USAGE EXAMPLES
================================================================================

  Basic usage:
    PaymProdMigrate.exe
    
  Custom source file:
    PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf"
    
  Custom source and target:
    PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf" "C:\Output\MenuCalc.db"


================================================================================
  TROUBLESHOOTING
================================================================================

  Problem: "Source database file not found"
  Solution: Ensure MenuCaolc.mdf is in the same folder, or provide full path
  
  Problem: "Failed to connect to LocalDB"
  Solution: Install SQL Server LocalDB or Express, then run:
            sqllocaldb start MSSQLLocalDB
  
  Problem: "Menu tables not migrated"
  Solution: This is expected - products, dishes, and recipes are migrated.
            You can create new menus in the application.


================================================================================
  OUTPUT LOCATION
================================================================================

  The migrated SQLite database is created at:
  
    C:\Users\<YourName>\AppData\Local\PaymProdNet9\MenuCalc.db
    
  This is automatically used by the PaymProdNet9 application.


================================================================================
  MORE INFORMATION
================================================================================

  For detailed documentation, see:
  
    MIGRATION_TOOL_GUIDE.md
    
  This includes:
    - Complete usage guide
    - Building from source
    - Advanced configuration
    - Troubleshooting
    - Distribution options


================================================================================
  VERSION INFORMATION
================================================================================

  Tool Version: 1.0.0
  .NET Version: 9.0
  Platform: Windows x64
  
  Dependencies:
    - Microsoft.Data.SqlClient 6.1.2
    - Microsoft.Data.Sqlite 9.0.10


================================================================================

For questions or issues, refer to the comprehensive documentation in
MIGRATION_TOOL_GUIDE.md

================================================================================

