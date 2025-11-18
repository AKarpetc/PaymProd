# 📦 Создание установщика для PaymProdNet9

## Быстрый старт

### Вариант 1: Inno Setup (Рекомендуется) ⭐

**Самый простой способ:**

1. Установите [Inno Setup 6.0+](https://jrsoftware.org/isdl.php)
2. Запустите:
   ```batch
   cd Installer
   build-inno-setup.bat
   ```
3. Готово! Установщик: `Installer\bin\PaymProdNet9_Setup.exe`

### Вариант 2: WiX Toolset

**Для профессиональных MSI установщиков:**

1. Установите [WiX Toolset v3.11+](https://wixtoolset.org/releases/)
2. Запустите:
   ```batch
   cd Installer
   build-installer.bat
   ```
3. Готово! Установщик: `Installer\bin\PaymProdNet9_Setup.msi`

## Что устанавливает установщик:

✅ Приложение в `Program Files\PaymProdNet9`  
✅ Ярлык в меню Пуск  
✅ Ярлык на рабочем столе (опционально)  
✅ Запись в "Установка и удаление программ"  
✅ Поддержка обновления и удаления  

## Подробная документация:

См. `Installer\INSTALLER_GUIDE.md` для детальной информации.

## Требования:

- Windows 10/11 (x64)
- .NET 9.0 Runtime (если приложение не self-contained)

