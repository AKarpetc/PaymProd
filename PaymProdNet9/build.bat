@echo off
echo ====================================
echo Building PaymProdNet9
echo ====================================
echo.

echo Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Building project in Release mode...
dotnet build -c Release
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ====================================
echo Build completed successfully!
echo ====================================
echo.
pause

