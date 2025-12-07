using System.Windows;
using PaymProdNet9.Services;

namespace PaymProdNet9;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Настраиваем консоль для правильного отображения UTF-8
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Игнорируем ошибки настройки консоли
        }

        // Инициализируем логгер в самом начале
        Logger.Initialize();
        Logger.Info("Приложение запущено");

        // Обработка необработанных исключений
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Logger.Error("Необработанное исключение в AppDomain", exception);
            
            // Показываем сообщение пользователю только для критических ошибок
            if (args.IsTerminating)
            {
                var errorMessage = exception != null
                    ? $"Критическая ошибка приложения:\n\n{exception.Message}\n\nПодробности в логах."
                    : "Произошла критическая ошибка приложения. Подробности в логах.";
                
                MessageBox.Show(errorMessage, "Критическая ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Logger.Error("Необработанное исключение в UI потоке", args.Exception);
            
            // Показываем сообщение пользователю
            var errorMessage = $"Произошла ошибка:\n\n{args.Exception.Message}\n\nПодробности в логах.";
            MessageBox.Show(errorMessage, "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            
            args.Handled = true; // Предотвращаем краш приложения
        };

        // Обработка необработанных исключений в задачах
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Logger.Error("Необработанное исключение в Task", args.Exception);
            args.SetObserved(); // Помечаем как обработанное
        };

        // Проверяем наличие базы данных в AppData (созданной инструментом миграции)
        var appDataPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9", "MenuCalc.db");

        // Если база существует в AppData, используем её
        // Иначе создаём в директории приложения
        var dbPath = System.IO.File.Exists(appDataPath)
            ? appDataPath
            : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");

        // Убедимся что директория существует
        var directory = System.IO.Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        if (!System.IO.File.Exists(dbPath))
        {
            await UpdateService.TryDownloadStartDatabaseAsync(dbPath, null, replaceExisting: false, silentSuccess: true);
        }

        try
        {
            Logger.Debug($"Инициализация базы данных: {dbPath}");
            Data.DatabaseHelper.InitializeDatabase(dbPath);
            Logger.Info("База данных успешно инициализирована");
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при инициализации базы данных", ex);
            MessageBox.Show($"Критическая ошибка при инициализации базы данных:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        try
        {
            var mainWindow = new MainNavigationWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            Logger.Debug("Главное окно отображено");

            await UpdateService.CheckForUpdatesAsync(mainWindow);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при запуске приложения", ex);
            MessageBox.Show($"Ошибка при запуске приложения:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}