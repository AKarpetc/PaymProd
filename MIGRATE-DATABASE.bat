@echo off
echo.
echo ==========================================
echo   DATABASE MIGRATION: LocalDB -^> SQLite
echo ==========================================
echo.
echo This will migrate your data from:
echo   MenuCaolc.mdf
echo.
echo To:
echo   %%LOCALAPPDATA%%\PaymProdNet9\MenuCalc.db
echo.
echo.

dotnet run --project MigrationTool

echo.
pause

