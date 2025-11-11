@echo off
echo.
echo ==========================================
echo   DATABASE MIGRATION: LocalDB -^> SQLite
echo ==========================================
echo.

rem Build the project first to get dependencies
echo Building project...
cd PaymProdNet9
dotnet build -c Release -v quiet
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
cd ..

echo.
echo Copying dependencies...
if not exist TempMigrate mkdir TempMigrate
copy PaymProdNet9\bin\Release\net9.0-windows\*.dll TempMigrate\ >nul 2>&1

echo Creating migration project...
cd TempMigrate

rem Create temporary project file
(
echo ^<Project Sdk="Microsoft.NET.Sdk"^>
echo   ^<PropertyGroup^>
echo     ^<OutputType^>Exe^</OutputType^>
echo     ^<TargetFramework^>net9.0^</TargetFramework^>
echo   ^</PropertyGroup^>
echo   ^<ItemGroup^>
echo     ^<PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.2" /^>
echo     ^<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" /^>
echo   ^</ItemGroup^>
echo ^</Project^>
) > Migrate.csproj

rem Copy migration source
copy ..\MigrationRunner.cs Program.cs >nul

echo Running migration...
echo.
dotnet run --no-build

cd ..
rmdir /s /q TempMigrate 2>nul

echo.
pause

