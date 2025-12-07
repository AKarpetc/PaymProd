using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace PaymProdNet9.Data;

/// <summary>
/// Помощник для резервного копирования и восстановления базы данных
/// </summary>
public static class DatabaseBackupHelper
{
    /// <summary>
    /// Сохранить текущую базу данных в указанную папку
    /// </summary>
    /// <param name="targetFolder">Папка для сохранения</param>
    /// <param name="fileName">Имя файла (необязательно, по умолчанию MenuCalc_backup_дата.db)</param>
    /// <returns>Полный путь к сохраненному файлу</returns>
    public static string SaveDatabaseToFolder(string targetFolder, string? fileName = null)
    {
        try
        {
            // Создаем папку если не существует
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

            // Формируем имя файла если не указано
            if (string.IsNullOrEmpty(fileName))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"MenuCalc_backup_{timestamp}.db";
            }

            var targetPath = Path.Combine(targetFolder, fileName);

            // Получаем путь к текущей базе данных
            var currentDbPath = GetCurrentDatabasePath();

            if (!File.Exists(currentDbPath))
                throw new FileNotFoundException($"База данных не найдена: {currentDbPath}");

            // Закрываем все соединения перед копированием
            SqliteConnection.ClearAllPools();

            // Копируем файл базы данных
            File.Copy(currentDbPath, targetPath, true);

            return targetPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при сохранении базы данных: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Загрузить базу данных из указанной папки
    /// </summary>
    /// <param name="sourceFilePath">Полный путь к файлу базы данных</param>
    /// <param name="replaceExisting">Заменить существующую базу данных</param>
    /// <returns>true если успешно загружено</returns>
    public static bool LoadDatabaseFromFile(string sourceFilePath, bool replaceExisting = true)
    {
        try
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException($"Файл базы данных не найден: {sourceFilePath}");

            // Проверяем что это валидная база данных SQLite
            if (!IsValidSqliteDatabase(sourceFilePath))
                throw new InvalidDataException("Указанный файл не является валидной базой данных SQLite");

            var currentDbPath = GetCurrentDatabasePath();
            var backupPath = currentDbPath + ".backup";

            // Создаем резервную копию существующей базы если она есть
            if (File.Exists(currentDbPath) && replaceExisting)
            {
                // Закрываем все соединения
                SqliteConnection.ClearAllPools();

                // Делаем резервную копию
                File.Copy(currentDbPath, backupPath, true);
            }

            try
            {
                // Закрываем все соединения
                SqliteConnection.ClearAllPools();

                // Копируем новую базу данных
                File.Copy(sourceFilePath, currentDbPath, replaceExisting);

                // Обновляем строку подключения
                DatabaseHelper.InitializeDatabase(currentDbPath);

                // Удаляем резервную копию если все прошло успешно
                if (File.Exists(backupPath)) File.Delete(backupPath);

                return true;
            }
            catch
            {
                // Восстанавливаем из резервной копии при ошибке
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, currentDbPath, true);
                    File.Delete(backupPath);
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при загрузке базы данных: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Экспортировать базу данных с выбором папки через диалог
    /// </summary>
    public static string? ExportDatabaseWithDialog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Сохранить базу данных",
            Filter = "База данных SQLite (*.db)|*.db|Все файлы (*.*)|*.*",
            DefaultExt = ".db",
            FileName = $"MenuCalc_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };

        if (dialog.ShowDialog() == true)
            try
            {
                var folder = Path.GetDirectoryName(dialog.FileName);
                var fileName = Path.GetFileName(dialog.FileName);
                return SaveDatabaseToFolder(folder!, fileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при экспорте базы данных: {ex.Message}", ex);
            }

        return null;
    }

    /// <summary>
    /// Импортировать базу данных с выбором файла через диалог
    /// </summary>
    public static bool ImportDatabaseWithDialog()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Загрузить базу данных",
            Filter = "База данных SQLite (*.db)|*.db|Все файлы (*.*)|*.*",
            DefaultExt = ".db"
        };

        if (dialog.ShowDialog() == true) return LoadDatabaseFromFile(dialog.FileName, true);

        return false;
    }

    /// <summary>
    /// Создать резервную копию в папке по умолчанию
    /// </summary>
    public static string CreateAutoBackup()
    {
        var backupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PaymProd", "Backups");

        return SaveDatabaseToFolder(backupFolder);
    }

    /// <summary>
    /// Получить путь к текущей базе данных
    /// </summary>
    public static string GetCurrentDatabasePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9", "MenuCalc.db");

        var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");

        return File.Exists(appDataPath) ? appDataPath : binPath;
    }

    /// <summary>
    /// Создать новую пустую базу данных, удалив текущую
    /// </summary>
    public static void CreateFreshDatabase()
    {
        var currentDbPath = GetCurrentDatabasePath();
        SqliteConnection.ClearAllPools();

        var directory = Path.GetDirectoryName(currentDbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(currentDbPath))
        {
            File.Delete(currentDbPath);
        }

        DatabaseHelper.InitializeDatabase(currentDbPath);
    }

    /// <summary>
    /// Проверить что файл является валидной базой данных SQLite
    /// </summary>
    private static bool IsValidSqliteDatabase(string filePath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={filePath}");
            connection.Open();

            // Проверяем что можем прочитать таблицы
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = command.ExecuteReader();

            return reader.HasRows;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Получить список доступных резервных копий
    /// </summary>
    public static List<BackupInfo> GetAvailableBackups()
    {
        var backupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PaymProd", "Backups");

        var backups = new List<BackupInfo>();

        if (Directory.Exists(backupFolder))
        {
            var files = Directory.GetFiles(backupFolder, "*.db")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    FilePath = file,
                    FileName = fileInfo.Name,
                    CreatedDate = fileInfo.LastWriteTime,
                    Size = fileInfo.Length
                });
            }
        }

        return backups;
    }
}

/// <summary>
/// Информация о резервной копии
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public long Size { get; set; }

    public string FormattedSize => Size < 1024 * 1024
        ? $"{Size / 1024:N0} KB"
        : $"{Size / 1024.0 / 1024.0:N2} MB";

    public string FormattedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm:ss");
}