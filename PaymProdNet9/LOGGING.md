# Система логирования

## Глобальный обработчик ошибок

Приложение автоматически перехватывает и логирует все необработанные исключения:

- **AppDomain.UnhandledException** - критические ошибки, которые приводят к завершению приложения
- **DispatcherUnhandledException** - ошибки в UI потоке (предотвращает краш приложения)
- **TaskScheduler.UnobservedTaskException** - ошибки в асинхронных задачах

Все ошибки записываются в логи с полной информацией (тип исключения, сообщение, стек вызовов).

## Логирование SQL-запросов

Для логирования SQL-запросов (только в Debug режиме) используйте extension методы:

```csharp
using PaymProdNet9.Data; // Для доступа к extension методам

// Вместо:
command.ExecuteNonQuery();

// Используйте:
command.ExecuteNonQueryWithLog();

// Вместо:
command.ExecuteScalar();

// Используйте:
command.ExecuteScalarWithLog();

// Вместо:
command.ExecuteReader();

// Используйте:
command.ExecuteReaderWithLog();
```

### Пример

```csharp
var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM Menus WHERE Id = @id";
command.Parameters.AddWithValue("@id", menuId);

// Автоматически логирует SQL и параметры (только в Debug)
using var reader = command.ExecuteReaderWithLog();
```

### Что логируется

- SQL-запрос (CommandText)
- Все параметры с их значениями
- Значения длиннее 200 символов обрезаются

### Где найти логи

Логи сохраняются в:

- Windows: `%LocalAppData%\PaymProdNet9\Logs\PaymProd_YYYYMMDD.log`
- Консоль (с цветовой подсветкой)

Старые логи (старше 7 дней) автоматически удаляются.

## Уровни логирования

- **Debug** - отладочная информация (только в Debug сборке)
- **Info** - информационные сообщения
- **Warning** - предупреждения
- **Error** - ошибки с полным стеком вызовов

## Использование Logger

```csharp
using PaymProdNet9.Services;

Logger.Info("Операция выполнена успешно");
Logger.Warning("Потенциальная проблема");
Logger.Error("Ошибка при выполнении операции", exception);
Logger.Debug("Отладочная информация"); // Только в Debug
Logger.Sql("SELECT * FROM Menus", parameters); // Только в Debug
```

