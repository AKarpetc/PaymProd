# 📁 Руководство по резервному копированию базы данных

## ✨ Новые функции добавлены!

Теперь в приложении PaymProdNet9 есть полноценная система управления базой данных с возможностью экспорта и импорта.

---

## 🎯 Как использовать

### Открыть менеджер базы данных

В главном окне приложения:
1. Откройте меню **Справочники**
2. Выберите **Управление базой данных**

---

## 💾 Экспорт базы данных (Сохранение)

### Способ 1: Экспорт в выбранную папку

```csharp
// В коде
var savedPath = DatabaseBackupHelper.SaveDatabaseToFolder(
    @"C:\Backups", 
    "MyBackup.db"
);
```

**Через интерфейс:**
1. Нажмите кнопку **"💾 Экспорт базы данных"**
2. Выберите папку и имя файла
3. Нажмите "Сохранить"

### Способ 2: Автоматическая резервная копия

```csharp
// В коде
var backupPath = DatabaseBackupHelper.CreateAutoBackup();
```

**Через интерфейс:**
1. Нажмите кнопку **"🔄 Создать резервную копию"**
2. Копия автоматически сохранится в:
   ```
   C:\Users\<Ваше имя>\Documents\PaymProd\Backups\
   ```

---

## 📂 Импорт базы данных (Загрузка)

### Загрузка из файла

```csharp
// В коде
bool success = DatabaseBackupHelper.LoadDatabaseFromFile(
    @"C:\Backups\MyBackup.db",
    replaceExisting: true
);
```

**Через интерфейс:**
1. Нажмите кнопку **"📂 Импорт базы данных"**
2. Подтвердите замену текущей базы
3. Выберите файл базы данных (.db)
4. Перезапустите приложение

---

## 🔄 Восстановление из резервной копии

**Через интерфейс:**
1. В списке "Доступные резервные копии" выберите нужную копию
2. Нажмите **"↩️ Восстановить выбранную"**
3. Подтвердите восстановление
4. Перезапустите приложение

---

## 📊 Что включает резервная копия

Резервная копия содержит **ВСЕ данные**:
- ✅ Все продукты
- ✅ Все блюда с рецептами
- ✅ Все меню банкетов
- ✅ Все справочники (типы, единицы измерения)
- ✅ Все связи между данными

---

## 💻 Использование в коде

### Пример: Автоматическое резервное копирование при закрытии

```csharp
// В MainWindow.xaml.cs
protected override void OnClosing(CancelEventArgs e)
{
    try
    {
        // Создаем резервную копию при закрытии
        DatabaseBackupHelper.CreateAutoBackup();
    }
    catch (Exception ex)
    {
        // Логируем ошибку, но не прерываем закрытие
        Debug.WriteLine($"Backup failed: {ex.Message}");
    }
    
    base.OnClosing(e);
}
```

### Пример: Экспорт с пользовательским именем

```csharp
// Сохранить с датой и временем
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var path = DatabaseBackupHelper.SaveDatabaseToFolder(
    @"D:\MyBackups",
    $"PaymProd_{timestamp}.db"
);

MessageBox.Show($"Сохранено: {path}");
```

### Пример: Проверка и восстановление

```csharp
// Получить список резервных копий
var backups = DatabaseBackupHelper.GetAvailableBackups();

if (backups.Count > 0)
{
    // Показать последнюю копию
    var latest = backups.First();
    Console.WriteLine($"Последняя копия: {latest.FileName}");
    Console.WriteLine($"Дата: {latest.FormattedDate}");
    Console.WriteLine($"Размер: {latest.FormattedSize}");
    
    // Восстановить
    bool success = DatabaseBackupHelper.LoadDatabaseFromFile(
        latest.FilePath,
        replaceExisting: true
    );
}
```

---

## 🛡️ Безопасность

### Автоматическая резервная копия при импорте

Когда вы импортируете базу данных, **текущая база автоматически сохраняется** как резервная копия:

```
MenuCalc.db.backup
```

Если импорт не удался, старая база **автоматически восстанавливается**.

### Проверка валидности

Перед импортом файл проверяется:
- ✅ Является ли файл базой данных SQLite
- ✅ Можно ли прочитать таблицы
- ✅ Не поврежден ли файл

---

## 📁 Расположение файлов

### Текущая база данных

```
C:\Users\<Ваше имя>\AppData\Local\PaymProdNet9\MenuCalc.db
```

### Автоматические резервные копии

```
C:\Users\<Ваше имя>\Documents\PaymProd\Backups\
```

Имя файла: `MenuCalc_backup_YYYYMMDD_HHMMSS.db`

Пример: `MenuCalc_backup_20251111_180930.db`

---

## 🎯 Практические сценарии

### Сценарий 1: Ежедневное резервное копирование

```csharp
// Создавайте копию каждый день
public void DailyBackup()
{
    var backupPath = DatabaseBackupHelper.CreateAutoBackup();
    Console.WriteLine($"Ежедневная копия создана: {backupPath}");
}
```

### Сценарий 2: Перенос на другой компьютер

**На старом компьютере:**
1. Справочники → Управление базой данных
2. Экспорт базы данных → сохранить на флешку

**На новом компьютере:**
1. Установить PaymProdNet9
2. Справочники → Управление базой данных
3. Импорт базы данных → выбрать файл с флешки

### Сценарий 3: Откат изменений

Если вы случайно удалили данные:
1. Справочники → Управление базой данных
2. Выбрать последнюю резервную копию
3. Восстановить выбранную
4. Перезапустить приложение

### Сценарий 4: Работа с несколькими базами

```csharp
// База для ресторана A
DatabaseBackupHelper.LoadDatabaseFromFile(@"C:\Data\RestaurantA.db");

// База для ресторана B
DatabaseBackupHelper.LoadDatabaseFromFile(@"C:\Data\RestaurantB.db");
```

---

## 🔧 Дополнительные функции

### Получить путь к текущей базе

```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PaymProdNet9", "MenuCalc.db");
```

### Открыть папку с резервными копиями

**Через интерфейс:**
- Нажмите **"📁 Открыть папку с копиями"**

**В коде:**
```csharp
var backupFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "PaymProd", "Backups");

Process.Start("explorer.exe", backupFolder);
```

---

## ⚙️ API Reference

### DatabaseBackupHelper

```csharp
// Сохранить в папку
string SaveDatabaseToFolder(string targetFolder, string? fileName = null)

// Загрузить из файла
bool LoadDatabaseFromFile(string sourceFilePath, bool replaceExisting = true)

// Экспорт через диалог
string? ExportDatabaseWithDialog()

// Импорт через диалог
bool ImportDatabaseWithDialog()

// Автоматическая резервная копия
string CreateAutoBackup()

// Список резервных копий
List<BackupInfo> GetAvailableBackups()
```

### BackupInfo

```csharp
public class BackupInfo
{
    public string FilePath { get; set; }        // Полный путь
    public string FileName { get; set; }        // Имя файла
    public DateTime CreatedDate { get; set; }   // Дата создания
    public long Size { get; set; }              // Размер в байтах
    public string FormattedSize { get; }        // Размер (KB/MB)
    public string FormattedDate { get; }        // Дата (dd.MM.yyyy HH:mm:ss)
}
```

---

## 📝 Рекомендации

### ✅ DO (Рекомендуется)

- ✅ Создавайте резервные копии **перед важными операциями**
- ✅ Храните резервные копии **в нескольких местах** (флешка, облако)
- ✅ **Проверяйте резервные копии** периодически
- ✅ Используйте **понятные имена файлов** с датами
- ✅ **Удаляйте старые копии** чтобы освободить место

### ❌ DON'T (Не рекомендуется)

- ❌ Не редактируйте файлы базы данных вручную
- ❌ Не храните только одну копию
- ❌ Не импортируйте неизвестные базы данных
- ❌ Не забывайте перезапускать приложение после импорта

---

## 🆘 Устранение проблем

### Проблема: "Файл используется другим процессом"

**Решение:**
1. Закройте все экземпляры PaymProdNet9
2. Подождите 10 секунд
3. Попробуйте снова

### Проблема: "Неверная база данных"

**Решение:**
- Убедитесь что файл не поврежден
- Проверьте расширение (.db)
- Попробуйте другую резервную копию

### Проблема: "Нет доступа к папке"

**Решение:**
- Проверьте права доступа
- Попробуйте сохранить в другую папку
- Запустите приложение от администратора

---

## 🎉 Готово!

Теперь у вас есть полный контроль над базой данных:
- 💾 Экспорт в любую папку
- 📂 Импорт из файла
- 🔄 Автоматические резервные копии
- ↩️ Восстановление из копий
- 📁 Удобный менеджер

**Ваши данные в безопасности!** 🛡️

---

*Документация обновлена: 11.11.2025*

