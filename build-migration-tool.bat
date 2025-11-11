@echo off
echo.
echo ============================================================
echo   Building PaymProd Migration Tool
echo ============================================================
echo.

cd MigrationTool

echo Cleaning previous builds...
rd /s /q bin\Release 2>nul
rd /s /q obj 2>nul

echo.
echo Building standalone executable...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed!
    cd ..
    pause
    exit /b 1
)

echo.
echo Copying executable to project root...
copy /Y bin\Release\net9.0\win-x64\publish\PaymProdMigrate.exe ..\PaymProdMigrate.exe >nul

cd ..

echo.
echo ============================================================
echo   BUILD SUCCESSFUL!
echo ============================================================
echo.
echo Executable created: PaymProdMigrate.exe
echo.

dir PaymProdMigrate.exe | find "PaymProdMigrate.exe"

echo.
echo To run the migration:
echo   PaymProdMigrate.exe
echo.
echo Or double-click PaymProdMigrate.exe
echo.

pause

