@echo off
chcp 65001 >nul
echo ========================================
echo Сборка установщика PaymProdNet9 (Inno Setup)
echo ========================================
echo.

REM Проверка наличия Inno Setup
set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\iscc.exe"
if exist "%ISCC_PATH%" goto HAS_INNO
echo [ОШИБКА] Inno Setup не найден по пути:
echo %ISCC_PATH%
echo.
echo Измените переменную ISCC_PATH в build-inno-setup.bat, если Inno Setup установлен в другом месте.
echo.
pause
exit /b 1
:HAS_INNO

REM Переход в папку проекта
cd /d "%~dp0.."

REM Публикация приложения
echo [1/3] Публикация приложения...
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

REM Компиляция установщика
echo [2/3] Компиляция установщика Inno Setup...
"%ISCC_PATH%" PaymProdNet9.iss
if %ERRORLEVEL% NEQ 0 (
    echo [ОШИБКА] Ошибка при компиляции установщика!
    pause
    exit /b 1
)

echo [3/3] Готово!
echo.
echo Установщик создан: Installer\bin\PaymProdNet9_Setup.exe
echo.
pause

