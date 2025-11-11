@echo off
echo.
echo ============================================================
echo   Building PaymProd Migration Tool (Standalone Executable)
echo ============================================================
echo.

echo Cleaning previous builds...
rd /s /q bin\Release 2>nul
rd /s /q obj 2>nul

echo.
echo Building single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   BUILD SUCCESSFUL!
echo ============================================================
echo.
echo Executable location:
echo   bin\Release\net9.0\win-x64\publish\PaymProdMigrate.exe
echo.
echo File size: 
dir bin\Release\net9.0\win-x64\publish\PaymProdMigrate.exe | find "PaymProdMigrate.exe"
echo.
echo You can now copy PaymProdMigrate.exe to any location.
echo.

pause

