using Microsoft.Data.Sqlite;
using PaymProdNet9.Models;

namespace PaymProdNet9.Data;

public class SettingsRepository
{
    private readonly string? _dbPath;

    public SettingsRepository(string? dbPath = null)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// Получить текущие настройки. Если их нет, создаются дефолтные.
    /// </summary>
    public AppGlobalSettings GetSettings()
    {
        using var connection = DatabaseHelper.GetConnection(_dbPath);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT ServicePercent, DefaultMarkup, MenuReportFontSize, ProductReportFontSize FROM Settings WHERE Id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return new AppGlobalSettings
            {
                Id = 1,
                ServicePercent = reader.GetDecimal(0),
                DefaultMarkup = reader.GetDecimal(1),
                MenuReportFontSize = reader.IsDBNull(2) ? 12 : reader.GetInt32(2),
                ProductReportFontSize = reader.IsDBNull(3) ? 12 : reader.GetInt32(3)
            };

        // Если настроек нет (например, только создали таблицу), возвращаем дефолтные
        return new AppGlobalSettings();
    }

    /// <summary>
    /// Сохранить настройки.
    /// </summary>
    public void SaveSettings(AppGlobalSettings settings)
    {
        using var connection = DatabaseHelper.GetConnection(_dbPath);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Settings (Id, ServicePercent, DefaultMarkup, MenuReportFontSize, ProductReportFontSize) 
            VALUES (1, @servicePercent, @defaultMarkup, @menuFontSize, @productFontSize)
            ON CONFLICT(Id) DO UPDATE SET 
                ServicePercent = excluded.ServicePercent,
                DefaultMarkup = excluded.DefaultMarkup,
                MenuReportFontSize = excluded.MenuReportFontSize,
                ProductReportFontSize = excluded.ProductReportFontSize";

        command.Parameters.AddWithValue("@servicePercent", settings.ServicePercent);
        command.Parameters.AddWithValue("@defaultMarkup", settings.DefaultMarkup);
        command.Parameters.AddWithValue("@menuFontSize", settings.MenuReportFontSize);
        command.Parameters.AddWithValue("@productFontSize", settings.ProductReportFontSize);

        command.ExecuteNonQuery();
    }
}