using Microsoft.Data.Sqlite;
using PaymProdNet9.Services;
using System.Collections.Generic;
using System.Diagnostics;

namespace PaymProdNet9.Data;

/// <summary>
/// Расширения для логирования SQL-запросов
/// </summary>
public static class DatabaseCommandExtensions
{
    /// <summary>
    /// Выполняет ExecuteNonQuery с логированием (только в Debug режиме)
    /// </summary>
    public static int ExecuteNonQueryWithLog(this SqliteCommand command)
    {
#if DEBUG
        LogCommand(command);
#endif
        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// Выполняет ExecuteScalar с логированием (только в Debug режиме)
    /// </summary>
    public static object? ExecuteScalarWithLog(this SqliteCommand command)
    {
#if DEBUG
        LogCommand(command);
#endif
        return command.ExecuteScalar();
    }

    /// <summary>
    /// Выполняет ExecuteReader с логированием (только в Debug режиме)
    /// </summary>
    public static SqliteDataReader ExecuteReaderWithLog(this SqliteCommand command)
    {
#if DEBUG
        LogCommand(command);
#endif
        return command.ExecuteReader();
    }

    /// <summary>
    /// Логирует SQL-запрос и его параметры
    /// </summary>
    [Conditional("DEBUG")]
    internal static void LogCommand(SqliteCommand command)
    {
        try
        {
            var sql = command.CommandText;
            var parameters = new Dictionary<string, object?>();

            foreach (SqliteParameter param in command.Parameters)
            {
                var value = param.Value;
                // Ограничиваем длину значения для логирования
                var displayValue = value?.ToString();
                if (displayValue != null && displayValue.Length > 200)
                    displayValue = displayValue.Substring(0, 200) + "...";
                parameters[param.ParameterName] = displayValue ?? (object?)DBNull.Value;
            }

            Logger.Sql(sql, parameters);
        }
        catch
        {
            // Игнорируем ошибки логирования, чтобы не нарушить основную логику
        }
    }
}