@echo off
echo ====================================
echo Running PaymProdNet9
echo ====================================
echo.

dotnet run
if errorlevel 1 (
    echo Failed to run application!
    pause
    exit /b 1
)

