using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

namespace PaymProdNet9.Services;

/// <summary>
/// Сервис логирования для записи логов в консоль и файл
/// </summary>
public static class Logger
{
    private static readonly object _lockObject = new object();
    private static string? _logDirectory;
    private static string? _logFileName;
    private static bool _isInitialized = false;
    private static LogLevel _minLogLevel = LogLevel.Debug;

    /// <summary>
    /// Инициализация логгера
    /// </summary>
    /// <param name="logDirectory">Директория для хранения логов (если null, используется AppData)</param>
    public static void Initialize(string? logDirectory = null)
    {
        if (_isInitialized) return;

        lock (_lockObject)
        {
            if (_isInitialized) return;

            // Загружаем уровень логирования из конфигурации
            LoadLogLevelFromConfig();

            // Определяем директорию для логов
            if (string.IsNullOrEmpty(logDirectory))
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PaymProdNet9", "Logs");
                _logDirectory = appDataPath;
            }
            else
            {
                _logDirectory = logDirectory;
            }

            // Создаем директорию, если её нет
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch
            {
                // Fallback: если AppData недоступна (редкий случай) — пишем в TEMP
                _logDirectory = Path.Combine(Path.GetTempPath(), "PaymProdNet9", "Logs");
                Directory.CreateDirectory(_logDirectory);
            }

            // Формируем имя файла с текущей датой
            _logFileName = Path.Combine(_logDirectory, $"PaymProd_{DateTime.Now:yyyyMMdd}.log");

            // Очищаем старые логи (старше 7 дней)
            CleanOldLogs();

            _isInitialized = true;
            Info($"Logger initialized with log level: {_minLogLevel}");
        }
    }

    /// <summary>
    /// Загружает уровень логирования из app.config
    /// </summary>
    private static void LoadLogLevelFromConfig()
    {
        try
        {
            var logLevelConfig = ConfigurationManager.AppSettings["LogLevel"];
            if (!string.IsNullOrEmpty(logLevelConfig))
            {
                if (Enum.TryParse<LogLevel>(logLevelConfig, ignoreCase: true, out var parsedLevel))
                {
                    _minLogLevel = parsedLevel;
                }
                else
                {
                    // Если значение не распознано, используем по умолчанию Info
                    _minLogLevel = LogLevel.Info;
                    Console.WriteLine($"[Logger] Неизвестный уровень логирования '{logLevelConfig}', используется по умолчанию: Info");
                }
            }
        }
        catch (Exception ex)
        {
            // Если не удалось загрузить конфигурацию, используем по умолчанию
            _minLogLevel = LogLevel.Info;
            Console.WriteLine($"[Logger] Ошибка при загрузке уровня логирования из конфигурации: {ex.Message}. Используется по умолчанию: Info");
        }
    }

    /// <summary>
    /// Очистка лог-файлов старше 7 дней
    /// </summary>
    private static void CleanOldLogs()
    {
        try
        {
            if (string.IsNullOrEmpty(_logDirectory) || !Directory.Exists(_logDirectory))
                return;

            var cutoffDate = DateTime.Now.AddDays(-7);
            var logFiles = Directory.GetFiles(_logDirectory, "PaymProd_*.log");

            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        Console.WriteLine($"[Logger] Удален старый лог-файл: {fileInfo.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Logger] Ошибка при удалении лог-файла {logFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Logger] Ошибка при очистке старых логов: {ex.Message}");
        }
    }

    /// <summary>
    /// Запись информационного сообщения
    /// </summary>
    public static void Info(string message)
    {
        WriteLog(LogLevel.Info, message);
    }

    /// <summary>
    /// Запись предупреждения
    /// </summary>
    public static void Warning(string message)
    {
        WriteLog(LogLevel.Warning, message);
    }

    /// <summary>
    /// Запись ошибки
    /// </summary>
    public static void Error(string message, Exception? exception = null)
    {
        var fullMessage = message;
        if (exception != null)
        {
            fullMessage += $"\nИсключение: {exception.GetType().Name}\nСообщение: {exception.Message}\nСтек вызовов: {exception.StackTrace}";
            if (exception.InnerException != null)
            {
                fullMessage += $"\nВнутреннее исключение: {exception.InnerException.Message}";
            }
        }
        WriteLog(LogLevel.Error, fullMessage);
    }

    /// <summary>
    /// Запись отладочного сообщения (только в Debug сборке)
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Debug(string message)
    {
        WriteLog(LogLevel.Debug, message);
    }

    /// <summary>
    /// Запись SQL-запроса (только в Debug сборке)
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Sql(string sql, System.Collections.Generic.Dictionary<string, object?>? parameters = null)
    {
        var message = $"SQL: {sql}";
        if (parameters != null && parameters.Count > 0)
        {
            var paramString = string.Join(", ", parameters.Select(p => $"{p.Key}={FormatParameterValue(p.Value)}"));
            message += $"\nПараметры: {paramString}";
        }
        WriteLog(LogLevel.Debug, message);
    }

    /// <summary>
    /// Форматирует значение параметра для логирования
    /// </summary>
    private static string FormatParameterValue(object? value)
    {
        if (value == null || value == DBNull.Value)
            return "NULL";
        
        var str = value.ToString() ?? "null";
        // Ограничиваем длину для читаемости
        if (str.Length > 100)
            return str.Substring(0, 100) + "...";
        return str;
    }

    /// <summary>
    /// Запись лога
    /// </summary>
    private static void WriteLog(LogLevel level, string message)
    {
        if (!_isInitialized)
        {
            // Если логгер не инициализирован, инициализируем с настройками по умолчанию
            Initialize();
        }

        // Важно: ошибки пишем всегда (даже если в конфиге высокий уровень)
        // Пользователь должен иметь возможность поделиться логом в Release.
        if (level != LogLevel.Error)
        {
            // Фильтруем логи по минимальному уровню
            if (level < _minLogLevel)
            {
                return;
            }
        }

        // В Release сборке пропускаем Debug логи (если они не включены через конфигурацию)
#if !DEBUG
        if (level == LogLevel.Debug && _minLogLevel > LogLevel.Debug)
        {
            return;
        }
#endif

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{level}] {message}";

        // Запись в консоль (если доступна)
        try
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = level switch
                {
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Debug => ConsoleColor.Gray,
                    _ => ConsoleColor.White
                };
                Console.WriteLine(logMessage);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
        catch
        {
            // Консоль может быть недоступна в WPF приложении
        }

        // Запись в Debug Output (видно в Visual Studio / Rider)
        System.Diagnostics.Debug.WriteLine(logMessage);

        // Запись в файл
        lock (_lockObject)
        {
            try
            {
                // Если по какой-то причине директория не готова (Release/инсталлятор/права) — используем fallback в TEMP
                if (string.IsNullOrEmpty(_logDirectory))
                {
                    _logDirectory = Path.Combine(Path.GetTempPath(), "PaymProdNet9", "Logs");
                }
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Проверяем, нужно ли создать новый файл (если дата изменилась)
                var currentLogFileName = Path.Combine(_logDirectory!, $"PaymProd_{DateTime.Now:yyyyMMdd}.log");
                if (currentLogFileName != _logFileName)
                {
                    _logFileName = currentLogFileName;
                }

                // Записываем в файл с UTF-8 кодировкой
                // Если файл новый, добавляем BOM для правильного отображения в Windows
                var fileExists = File.Exists(_logFileName!);
                if (!fileExists)
                {
                    // Создаем файл с UTF-8 BOM
                    using (var writer = new StreamWriter(_logFileName!, false, new UTF8Encoding(true)))
                    {
                        writer.Write(logMessage + Environment.NewLine);
                    }
                }
                else
                {
                    // Добавляем в существующий файл
                    File.AppendAllText(_logFileName!, logMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // Если не удалось записать в файл, выводим только в консоль
                Console.WriteLine($"[Logger] Ошибка при записи в файл: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Уровни логирования
    /// </summary>
    private enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}

