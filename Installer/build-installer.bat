@echo off
chcp 65001 >nul
echo ========================================
echo Сборка установщика PaymProdNet9
echo ========================================
echo.

REM Проверка наличия WiX Toolset
where candle.exe >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ОШИБКА] WiX Toolset не установлен!
    echo.
    echo Пожалуйста, установите WiX Toolset v3.11 или новее:
    echo https://wixtoolset.org/releases/
    echo.
    pause
    exit /b 1
)

REM Переход в папку проекта
cd /d "%~dp0.."

REM Публикация приложения
echo [1/4] Публикация приложения...
cd PaymProdNet9
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if %ERRORLEVEL% NEQ 0 (
    echo [ОШИБКА] Ошибка при публикации приложения!
    pause
    exit /b 1
)
cd ..

REM Переход в папку установщика
cd Installer

REM Компиляция WiX
echo [2/4] Компиляция установщика...
candle.exe -ext WixUtilExtension Product.wxs -out obj\Product.wixobj
if %ERRORLEVEL% NEQ 0 (
    echo [ОШИБКА] Ошибка при компиляции WiX!
    pause
    exit /b 1
)

REM Линковка
echo [3/4] Создание MSI файла...
light.exe -ext WixUtilExtension -ext WixUIExtension obj\Product.wixobj -out bin\PaymProdNet9_Setup.msi -cultures:ru-RU
if %ERRORLEVEL% NEQ 0 (
    echo [ОШИБКА] Ошибка при создании MSI!
    pause
    exit /b 1
)

echo [4/4] Готово!
echo.
echo Установщик создан: Installer\bin\PaymProdNet9_Setup.msi
echo.
pause

