@echo off
echo.
echo ==========================================
echo   Compiling Migration Tool
echo ==========================================
echo.

cd PaymProdNet9
dotnet build -c Release >nul 2>&1
cd ..

echo Building migration executable...
csc /out:Migrate.exe /r:PaymProdNet9\bin\Release\net9.0-windows\Microsoft.Data.SqlClient.dll /r:PaymProdNet9\bin\Release\net9.0-windows\Microsoft.Data.Sqlite.dll /r:PaymProdNet9\bin\Release\net9.0-windows\SQLitePCLRaw.core.dll /r:PaymProdNet9\bin\Release\net9.0-windows\SQLitePCLRaw.provider.e_sqlite3.dll /r:PaymProdNet9\bin\Release\net9.0-windows\SQLitePCLRaw.batteries_v2.dll MigrationRunner.cs 2>nul

if not exist Migrate.exe (
    echo [ERROR] Build failed. Running with dotnet instead...
    echo.
    dotnet script MigrationRunner.cs
) else (
    echo [OK] Built successfully
    echo.
    echo Running migration...
    echo.
    Migrate.exe
    del Migrate.exe >nul 2>&1
)

pause

