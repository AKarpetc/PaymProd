# Установщик PaymProdNet9

Этот проект содержит установщик для приложения PaymProdNet9, созданный с использованием WiX Toolset.

## Требования

### Для сборки установщика:

1. **WiX Toolset v3.11 или новее**
   - Скачать: https://wixtoolset.org/releases/
   - Установить WiX Toolset Build Tools

2. **.NET 9.0 SDK**
   - Должен быть установлен для публикации приложения

### Для установки приложения:

1. **.NET 9.0 Runtime** (если приложение не self-contained)
   - Скачать: https://dotnet.microsoft.com/download/dotnet/9.0
   - Или установщик автоматически проверит наличие

## Сборка установщика

### Автоматическая сборка (рекомендуется):

```batch
cd Installer
build-installer.bat
```

### Ручная сборка:

1. Опубликуйте приложение:
```batch
cd PaymProdNet9
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

2. Соберите установщик:
```batch
cd Installer
candle.exe Product.wxs -out obj\Product.wixobj
light.exe obj\Product.wixobj -out bin\PaymProdNet9_Setup.msi
```

## Структура файлов

```
Installer/
├── Product.wxs              # Основной файл WiX
├── PaymProdInstaller.wixproj # Файл проекта WiX
├── build-installer.bat      # Скрипт автоматической сборки
├── License.rtf              # Лицензионное соглашение (опционально)
├── Banner.bmp               # Баннер установщика (опционально)
└── Dialog.bmp               # Фон диалогов (опционально)
```

## Что устанавливает установщик

- Приложение PaymProdNet9 в папку `Program Files\PaymProdNet9`
- Ярлык в меню Пуск
- Ярлык на рабочем столе
- Проверку наличия .NET 9.0 Runtime

## Альтернативные варианты установщиков

Если WiX Toolset слишком сложен, можно использовать:

1. **Inno Setup** - простой и популярный установщик
2. **NSIS** - Nullsoft Scriptable Install System
3. **MSIX** - современный формат для Windows 10/11

## Примечания

- Установщик требует прав администратора
- Приложение устанавливается для всех пользователей
- При обновлении старая версия будет удалена автоматически

