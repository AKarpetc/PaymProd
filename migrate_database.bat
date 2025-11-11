@echo off
chcp 65001 > nul
echo ═══════════════════════════════════════════════════════════
echo     МИГРАЦИЯ БАЗЫ ДАННЫХ SQL SERVER CE → SQLite
echo ═══════════════════════════════════════════════════════════
echo.

echo Проверка файла источника...
if not exist "MenuCaolc.mdf" (
    echo ❌ Файл MenuCaolc.mdf не найден!
    echo    Убедитесь, что файл находится в текущей папке.
    pause
    exit /b 1
)

echo ✓ Файл MenuCaolc.mdf найден
echo.

echo Создание утилиты миграции...
cd PaymProdNet9

REM Добавляем пакет для SQL Server CE
dotnet add package System.Data.SqlServerCe --version 4.0.0.1

echo.
echo Запуск миграции...
dotnet run --project . -- migrate

cd ..

echo.
echo ═══════════════════════════════════════════════════════════
echo            МИГРАЦИЯ ЗАВЕРШЕНА!
echo ═══════════════════════════════════════════════════════════
echo.
pause


