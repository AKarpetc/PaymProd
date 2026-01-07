using PaymProdNet9.Data;
using PaymProdNet9.Models;
using PaymProdNet9.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PaymProdNet9.Pages;

public partial class ParametersPage : Page
{
    private readonly SettingsRepository _settingsRepository;

    public ParametersPage()
    {
        InitializeComponent();
        _settingsRepository = new SettingsRepository();

        InitializeDeletedItemsSettings();
        LoadCurrentDatabaseInfo();
        LoadBackups();
        LoadSettings();
    }

    private void InitializeDeletedItemsSettings()
    {
        try
        {
            ShowDeletedItemsCheckBox.IsChecked = DeletedItemsViewSettings.ShowDeletedItems;
        }
        catch
        {
            // Если по какой-то причине чекбокс недоступен — просто игнорируем.
        }
    }

    // --- Settings Logic ---

    private void LoadSettings()
    {
        try
        {
            var settings = _settingsRepository.GetSettings();
            ServicePercentTextBox.Text = settings.ServicePercent.ToString("G");
            DefaultMarkupTextBox.Text = settings.DefaultMarkup.ToString("G");
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при загрузке настроек", ex);
            MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplyMarkupToAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(DefaultMarkupTextBox.Text, out var markup))
            {
                MessageBox.Show("Введите корректное значение надбавки.", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите установить надбавку {markup}% для ВСЕХ блюд?\n\n" +
                "Это действие обновит:\n" +
                "1. Все блюда в справочнике.\n" +
                "2. Все блюда в текущем открытом меню.\n\n" +
                "Продолжить?",
                "Подтверждение массового обновления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // 1. Обновляем справочник
            var delicateRepo = new DelicateRepository();
            var updatedCatalog = delicateRepo.UpdateAllDefaultMarkups(markup);

            // 2. Обновляем текущее меню
            var menuRepo = new MenuRepository();
            var openMenu = menuRepo.GetOpenMenu();
            var updatedMenu = 0;

            if (openMenu != null) updatedMenu = menuRepo.UpdateMarkupForMenu(openMenu.Id, markup);

            MessageBox.Show(
                $"Обновление завершено!\n\n" +
                $"Обновлено блюд в справочнике: {updatedCatalog}\n" +
                $"Обновлено блюд в текущем меню: {updatedMenu}",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при массовом обновлении надбавки", ex);
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(ServicePercentTextBox.Text, out var servicePercent))
            {
                MessageBox.Show("Некорректное значение процента за обслуживание", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(DefaultMarkupTextBox.Text, out var defaultMarkup))
            {
                MessageBox.Show("Некорректное значение надбавки по умолчанию", "Ошибка", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var settings = new AppGlobalSettings
            {
                Id = 1,
                ServicePercent = servicePercent,
                DefaultMarkup = defaultMarkup
            };

            _settingsRepository.SaveSettings(settings);
            MessageBox.Show("Настройки успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при сохранении настроек", ex);
            MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // --- Database Logic (From DatabaseManagerPage) ---

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

            // Optional: if (backups.Count == 0) ...
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

    private async void ShareLogsToS3Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaymProdNet9", "Logs");

            if (!Directory.Exists(logsDir))
            {
                MessageBox.Show("Папка с логами не найдена.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var files = Directory.GetFiles(logsDir, "PaymProd_*.log");
            if (files.Length == 0)
            {
                MessageBox.Show("Лог-файлы не найдены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var latestLog = files
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .First();

            var key = await S3UploadService.UploadFileAsync(latestLog, "logs");

            MessageBox.Show($"Журнал ошибок отправлен.\n\nОбъект: {key}",
                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке журнала ошибок:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ShareDbToS3Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Сначала создаем резервную копию (это снимает блокировки и копирует актуальную БД)
            var backupPath = DatabaseBackupHelper.CreateAutoBackup();
            if (!File.Exists(backupPath))
            {
                MessageBox.Show("Не удалось создать резервную копию базы данных.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var key = await S3UploadService.UploadFileAsync(backupPath, "database");

            MessageBox.Show($"База данных отправлена.\n\nОбъект: {key}",
                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке базы данных:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetDbButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Создать новую пустую базу данных?\n\nТекущая база будет сохранена как резервная копия.",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        string? backupPath = null;
        try
        {
            var currentPath = DatabaseBackupHelper.GetCurrentDatabasePath();
            if (File.Exists(currentPath)) backupPath = DatabaseBackupHelper.CreateAutoBackup();
        }
        catch (Exception backupEx)
        {
            var continueResult = MessageBox.Show(
                $"Не удалось создать резервную копию:\n{backupEx.Message}\n\nПродолжить без резервной копии?",
                "Внимание",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (continueResult != MessageBoxResult.Yes) return;
        }

        try
        {
            DatabaseBackupHelper.CreateFreshDatabase();
            var message = "Создана новая база данных.";
            if (!string.IsNullOrWhiteSpace(backupPath)) message += $"\nРезервная копия: {backupPath}";

            message += "\n\nПерезапустите приложение, чтобы начать работу с чистой базой.";

            MessageBox.Show(message,
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            LoadCurrentDatabaseInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании новой базы данных:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DownloadStartDbButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadStartDbButton.IsEnabled = false;
        try
        {
            var currentPath = DatabaseBackupHelper.GetCurrentDatabasePath();
            var window = Window.GetWindow(this);
            var downloaded = await UpdateService.TryDownloadStartDatabaseAsync(
                currentPath,
                window,
                true,
                false);

            if (downloaded)
                LoadCurrentDatabaseInfo();
            else
                MessageBox.Show(
                    "Загрузка стартовой базы данных отменена или недоступна.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }
        finally
        {
            DownloadStartDbButton.IsEnabled = true;
        }
    }

    private void ShowDeletedItemsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        DeletedItemsViewSettings.ShowDeletedItems = ShowDeletedItemsCheckBox.IsChecked == true;
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        InputValidationHelper.NumericOnly_PreviewTextInput(sender, e);
    }

    private void ValidateNumeric_LostFocus(object sender, RoutedEventArgs e)
    {
        InputValidationHelper.ValidateNumericField_LostFocus(sender, e);
    }
}