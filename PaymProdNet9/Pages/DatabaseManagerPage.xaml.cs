using PaymProdNet9.Data;
using PaymProdNet9.Services;
using PaymProdNet9.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PaymProdNet9.Pages;

public partial class DatabaseManagerPage : Page
{
    public DatabaseManagerPage()
    {
        InitializeComponent();
        LoadCurrentDatabaseInfo();
        LoadBackups();
    }

    private void LoadCurrentDatabaseInfo()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaymProdNet9", "MenuCalc.db");

            var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");

            var currentPath = File.Exists(appDataPath) ? appDataPath : binPath;

            if (File.Exists(currentPath))
            {
                var fileInfo = new FileInfo(currentPath);
                CurrentDbPathText.Text = $"{currentPath}\n" +
                                         $"Размер: {fileInfo.Length / 1024:N0} KB, " +
                                         $"Изменена: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss}";
            }
            else
            {
                CurrentDbPathText.Text = "База данных не найдена";
            }
        }
        catch (Exception ex)
        {
            CurrentDbPathText.Text = $"Ошибка: {ex.Message}";
        }
    }

    private void LoadBackups()
    {
        try
        {
            var backups = DatabaseBackupHelper.GetAvailableBackups();
            BackupsDataGrid.ItemsSource = backups;

            if (backups.Count == 0)
                MessageBox.Show("Резервные копии не найдены.\n\n" +
                                "Создайте первую резервную копию нажав кнопку 'Создать резервную копию'.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке списка резервных копий:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var savedPath = DatabaseBackupHelper.ExportDatabaseWithDialog();

            if (!string.IsNullOrEmpty(savedPath))
            {
                MessageBox.Show($"База данных успешно экспортирована:\n{savedPath}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadBackups();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте базы данных:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Импорт базы данных заменит текущую базу.\n\n" +
            "Текущая база будет сохранена как резервная копия.\n\n" +
            "Продолжить?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            try
            {
                if (DatabaseBackupHelper.ImportDatabaseWithDialog())
                {
                    MessageBox.Show(
                        "База данных успешно импортирована!\n\n" +
                        "Рекомендуется перезапустить приложение для применения изменений.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoadCurrentDatabaseInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте базы данных:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    private void AutoBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = DatabaseBackupHelper.CreateAutoBackup();

            MessageBox.Show($"Резервная копия успешно создана:\n{backupPath}",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadBackups();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании резервной копии:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsDataGrid.SelectedItem is BackupInfo backup)
        {
            var result = MessageBox.Show(
                $"Восстановить базу данных из резервной копии?\n\n" +
                $"Файл: {backup.FileName}\n" +
                $"Дата: {backup.FormattedDate}\n\n" +
                $"Текущая база данных будет заменена!",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                try
                {
                    if (DatabaseBackupHelper.LoadDatabaseFromFile(backup.FilePath))
                    {
                        MessageBox.Show(
                            "База данных успешно восстановлена!\n\n" +
                            "Рекомендуется перезапустить приложение.",
                            "Успех",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        LoadCurrentDatabaseInfo();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при восстановлении базы данных:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
        }
        else
        {
            MessageBox.Show("Выберите резервную копию для восстановления.",
                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadBackups();
        LoadCurrentDatabaseInfo();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PaymProd", "Backups");

            if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

            Process.Start("explorer.exe", backupFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии папки:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ShareToGoogleDriveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Получаем путь к текущей базе данных
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaymProdNet9", "MenuCalc.db");

            var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");
            var currentDbPath = File.Exists(appDataPath) ? appDataPath : binPath;

            if (!File.Exists(currentDbPath))
            {
                MessageBox.Show("База данных не найдена!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Отключаем кнопку на время загрузки
            ShareToGoogleDriveButton.IsEnabled = false;
            ShareToGoogleDriveButton.Content = "⏳ Подготовка...";

            // Загружаем на Google Drive
            var window = Window.GetWindow(this);
            await GoogleDriveService.UploadDatabaseToGoogleDriveAsync(currentDbPath, window);

            // Восстанавливаем кнопку
            ShareToGoogleDriveButton.IsEnabled = true;
            ShareToGoogleDriveButton.Content = "☁️ Поделиться БД (Google Drive)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке на Google Drive:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            ShareToGoogleDriveButton.IsEnabled = true;
            ShareToGoogleDriveButton.Content = "☁️ Поделиться БД (Google Drive)";
        }
    }

    private void ConfigureGoogleDriveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = Window.GetWindow(this);
            var authWindow = new GoogleDriveAuthWindow
            {
                Owner = window
            };

            if (authWindow.ShowDialog() == true)
            {
                MessageBox.Show("Настройки сохранены!\n\n" +
                               "Теперь загрузка на Google Drive будет автоматической.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при настройке:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}