using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PaymProdNet9.Services;
using PaymProdNet9.Windows;

namespace PaymProdNet9;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Устанавливаем русскую локализацию для приложения
        try
        {
            var culture = new System.Globalization.CultureInfo("ru-RU");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при установке локализации", ex);
        }

        // Настраиваем консоль для правильного отображения UTF-8
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Игнорируем ошибки настройки консоли
        }

        // Показываем splash screen
        var splashScreen = new PaymProdNet9.Windows.SplashScreen();
        splashScreen.Show();
        splashScreen.UpdateStatus("Инициализация...");
        
        // Обновляем UI для отображения splash screen
        await System.Threading.Tasks.Task.Delay(50);
        
        // Инициализируем логгер в самом начале
        Logger.Initialize();
        Logger.Info("Приложение запущено");

        // Обработка необработанных исключений
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            
            // Выводим подробную информацию в консоль
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("КРИТИЧЕСКАЯ ОШИБКА: Необработанное исключение в AppDomain");
            Console.WriteLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Приложение завершается: {args.IsTerminating}");
            if (exception != null)
            {
                Console.WriteLine($"Тип исключения: {exception.GetType().FullName}");
                Console.WriteLine($"Сообщение: {exception.Message}");
                Console.WriteLine($"Стек вызовов:\n{exception.StackTrace}");
                if (exception.InnerException != null)
                {
                    Console.WriteLine($"Внутреннее исключение: {exception.InnerException.GetType().FullName}");
                    Console.WriteLine($"Внутреннее сообщение: {exception.InnerException.Message}");
                    Console.WriteLine($"Внутренний стек:\n{exception.InnerException.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"Объект исключения: {args.ExceptionObject?.GetType().FullName ?? "null"}");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            Logger.Error("Необработанное исключение в AppDomain", exception);
            
            // Показываем сообщение пользователю только для критических ошибок
            if (args.IsTerminating)
            {
                var errorMessage = exception != null
                    ? $"Критическая ошибка приложения:\n\n{exception.Message}\n\nПодробности в логах и консоли."
                    : "Произошла критическая ошибка приложения. Подробности в логах и консоли.";
                
                MessageBox.Show(errorMessage, "Критическая ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            // Выводим подробную информацию в консоль
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("ОШИБКА: Необработанное исключение в UI потоке");
            Console.WriteLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Тип исключения: {args.Exception.GetType().FullName}");
            Console.WriteLine($"Сообщение: {args.Exception.Message}");
            Console.WriteLine($"Стек вызовов:\n{args.Exception.StackTrace}");
            if (args.Exception.InnerException != null)
            {
                Console.WriteLine($"Внутреннее исключение: {args.Exception.InnerException.GetType().FullName}");
                Console.WriteLine($"Внутреннее сообщение: {args.Exception.InnerException.Message}");
                Console.WriteLine($"Внутренний стек:\n{args.Exception.InnerException.StackTrace}");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            Logger.Error("Необработанное исключение в UI потоке", args.Exception);
            
            // Показываем сообщение пользователю
            var errorMessage = $"Произошла ошибка:\n\n{args.Exception.Message}\n\nПодробности в логах и консоли.";
            MessageBox.Show(errorMessage, "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            
            args.Handled = true; // Предотвращаем краш приложения
        };

        // Обработка необработанных исключений в задачах
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            // Выводим подробную информацию в консоль
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("ОШИБКА: Необработанное исключение в Task");
            Console.WriteLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Тип исключения: {args.Exception.GetType().FullName}");
            Console.WriteLine($"Сообщение: {args.Exception.Message}");
            Console.WriteLine($"Стек вызовов:\n{args.Exception.StackTrace}");
            if (args.Exception.InnerException != null)
            {
                Console.WriteLine($"Внутреннее исключение: {args.Exception.InnerException.GetType().FullName}");
                Console.WriteLine($"Внутреннее сообщение: {args.Exception.InnerException.Message}");
                Console.WriteLine($"Внутренний стек:\n{args.Exception.InnerException.StackTrace}");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            Logger.Error("Необработанное исключение в Task", args.Exception);
            args.SetObserved(); // Помечаем как обработанное
        };

        // Проверяем наличие базы данных в AppData (созданной инструментом миграции)
        splashScreen.UpdateStatus("Проверка базы данных...");
        var appDataDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaymProdNet9");
        var dbPath = System.IO.Path.Combine(appDataDir, "MenuCalc.db");

        // Всегда используем AppData для базы данных (там есть права на запись)
        // Убедимся что директория существует
        if (!System.IO.Directory.Exists(appDataDir))
        {
            try
            {
                System.IO.Directory.CreateDirectory(appDataDir);
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось создать директорию для базы данных: {appDataDir}", ex);
                splashScreen.Close();
                MessageBox.Show($"Не удалось создать директорию для базы данных:\n{appDataDir}\n\nОшибка: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        if (!System.IO.File.Exists(dbPath))
        {
            splashScreen.UpdateStatus("Загрузка базы данных...");
            await UpdateService.TryDownloadStartDatabaseAsync(dbPath, null, replaceExisting: false, silentSuccess: true);
        }

        try
        {
            splashScreen.UpdateStatus("Инициализация базы данных...");
            Logger.Debug($"Инициализация базы данных: {dbPath}");
            Data.DatabaseHelper.InitializeDatabase(dbPath);
            Logger.Info("База данных успешно инициализирована");
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при инициализации базы данных", ex);
            splashScreen.Close();
            MessageBox.Show($"Критическая ошибка при инициализации базы данных:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        try
        {
            splashScreen.UpdateStatus("Загрузка интерфейса...");
            var mainWindow = new MainNavigationWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            Logger.Debug("Главное окно отображено");

            // Закрываем splash screen
            splashScreen.Close();

            await UpdateService.CheckForUpdatesAsync(mainWindow);
        }
        catch (Exception ex)
        {
            Logger.Error("Ошибка при запуске приложения", ex);
            splashScreen.Close();
            MessageBox.Show($"Ошибка при запуске приложения:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}