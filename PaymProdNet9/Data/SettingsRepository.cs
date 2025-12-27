using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;

namespace PaymProdNet9.Data;

public class SettingsRepository
{
    /// <summary>
    /// Получить текущие настройки. Если их нет, создаются дефолтные.
    /// </summary>
    public AppGlobalSettings GetSettings()
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT ServicePercent, DefaultMarkup FROM Settings WHERE Id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new AppGlobalSettings
            {
                Id = 1,
                ServicePercent = reader.GetDecimal(0),
                DefaultMarkup = reader.GetDecimal(1)
            };
        }

        // Если настроек нет (например, только создали таблицу), возвращаем дефолтные
        return new AppGlobalSettings(); 
    }

    /// <summary>
    /// Сохранить настройки.
    /// </summary>
    public void SaveSettings(AppGlobalSettings settings)
    {
        using var connection = DatabaseHelper.GetConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Settings (Id, ServicePercent, DefaultMarkup) 
            VALUES (1, @servicePercent, @defaultMarkup)
            ON CONFLICT(Id) DO UPDATE SET 
                ServicePercent = excluded.ServicePercent,
                DefaultMarkup = excluded.DefaultMarkup";

        command.Parameters.AddWithValue("@servicePercent", settings.ServicePercent);
        command.Parameters.AddWithValue("@defaultMarkup", settings.DefaultMarkup);

        command.ExecuteNonQuery();
    }
}
