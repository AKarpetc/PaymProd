# Руководство по созданию установщика PaymProdNet9

Это руководство описывает два способа создания установщика для приложения PaymProdNet9.

## Вариант 1: Inno Setup (Рекомендуется) ⭐

### Преимущества:
- ✅ Простой в использовании
- ✅ Красивый интерфейс установщика
- ✅ Не требует сложной настройки
- ✅ Бесплатный и открытый исходный код

### Установка Inno Setup:

1. Скачайте Inno Setup 6.0 или новее:
   - https://jrsoftware.org/isdl.php
2. Установите Inno Setup

### Сборка установщика:

```batch
cd Installer
build-inno-setup.bat
```

Или вручную:

1. Опубликуйте приложение:
```batch
cd PaymProdNet9
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

2. Откройте `Installer\PaymProdNet9.iss` в Inno Setup Compiler
3. Нажмите "Build" → "Compile"

### Результат:
- `Installer\bin\PaymProdNet9_Setup.exe` - готовый установщик

---

## Вариант 2: WiX Toolset

### Преимущества:
- ✅ Профессиональный MSI установщик
- ✅ Стандарт для корпоративных приложений
- ✅ Поддержка групповых политик

### Установка WiX Toolset:

1. Скачайте WiX Toolset v3.11 или новее:
   - https://wixtoolset.org/releases/
2. Установите WiX Toolset Build Tools

### Сборка установщика:

```batch
cd Installer
build-installer.bat
```

Или вручную:

1. Опубликуйте приложение (см. выше)
2. Компилируйте WiX:
```batch
cd Installer
candle.exe Product.wxs -out obj\Product.wixobj
light.exe obj\Product.wixobj -out bin\PaymProdNet9_Setup.msi
```

### Результат:
- `Installer\bin\PaymProdNet9_Setup.msi` - готовый установщик

---

## Что делает установщик:

1. ✅ Устанавливает приложение в `Program Files\PaymProdNet9`
2. ✅ Создает ярлык в меню Пуск
3. ✅ Создает ярлык на рабочем столе (опционально)
4. ✅ Добавляет запись в "Установка и удаление программ"
5. ✅ Поддерживает обновление существующей установки
6. ✅ Полное удаление при деинсталляции

## Требования для установки:

- **Windows 10/11** (x64)
- **.NET 9.0 Runtime** (если приложение не self-contained)
  - Скачать: https://dotnet.microsoft.com/download/dotnet/9.0

## Настройка установщика:

### Изменение версии:

В файле `PaymProdNet9.iss` (Inno Setup) или `Product.wxs` (WiX):
```ini
#define MyAppVersion "2.0.0"  ; Измените версию здесь
```

### Добавление лицензии:

1. Создайте файл `License.rtf` в папке `Installer`
2. Добавьте текст лицензии
3. В `Product.wxs` раскомментируйте:
```xml
<WixVariable Id="WixUILicenseRtf" Value="$(var.ProjectDir)License.rtf" />
```

### Изменение иконки:

Иконка уже настроена: `PaymProdNet9\Resources\Restaurant_Blue_2.ico`

## Рекомендации:

- **Для простых проектов**: Используйте Inno Setup
- **Для корпоративных проектов**: Используйте WiX Toolset
- **Для Windows Store**: Используйте MSIX

## Устранение проблем:

### Ошибка "WiX Toolset не установлен":
- Установите WiX Toolset Build Tools
- Проверьте, что `candle.exe` и `light.exe` в PATH

### Ошибка "Inno Setup не установлен":
- Установите Inno Setup 6.0 или новее
- Проверьте, что `iscc.exe` в PATH

### Ошибка при публикации:
- Убедитесь, что .NET 9.0 SDK установлен
- Проверьте, что проект собирается без ошибок

## Дополнительная информация:

- [Документация Inno Setup](https://jrsoftware.org/ishelp/)
- [Документация WiX Toolset](https://wixtoolset.org/documentation/)
- [.NET Publishing Guide](https://docs.microsoft.com/dotnet/core/deploying/)

