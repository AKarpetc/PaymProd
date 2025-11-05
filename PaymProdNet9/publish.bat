@echo off
echo ====================================
echo Publishing PaymProdNet9
echo ====================================
echo.
echo Select publication type:
echo 1. Framework-dependent (requires .NET 9 installed)
echo 2. Self-contained (includes .NET 9 runtime)
echo.

choice /c 12 /n /m "Enter your choice (1 or 2): "
set choice=%errorlevel%

if %choice%==1 (
    echo.
    echo Publishing framework-dependent version...
    dotnet publish -c Release -r win-x64 --self-contained false -o publish\framework-dependent
    echo.
    echo Published to: publish\framework-dependent
) else (
    echo.
    echo Publishing self-contained version...
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\self-contained
    echo.
    echo Published to: publish\self-contained
)

echo.
echo ====================================
echo Publishing completed!
echo ====================================
echo.
pause

