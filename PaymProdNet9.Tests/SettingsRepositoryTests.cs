using PaymProdNet9.Data;
using PaymProdNet9.Models;
using System;
using System.IO;
using Xunit;

namespace PaymProdNet9.Tests.Data;

[Collection("Database Tests")]
public class SettingsRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SettingsRepository _repository;

    public SettingsRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_Sett_{Guid.NewGuid()}.db");
        DatabaseHelper.InitializeDatabase(_dbPath);
        _repository = new SettingsRepository();
    }

    public void Dispose()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
        }
    }

    [Fact]
    public void GetSettings_Defaults_ShouldReturnEmptyOrDefaults()
    {
        // Act
        var settings = _repository.GetSettings();

        // Assert
        Assert.NotNull(settings);
        // Default expected logic: 
        // - If table empty, returns new AppGlobalSettings() -> properties are 0 by default for decimal
        // - But let's verify what "new AppGlobalSettings()" gives.
        // - AppGlobalSettings defaults: ServicePercent=10, DefaultMarkup=200
        Assert.Equal(10, settings.ServicePercent);
        Assert.Equal(200, settings.DefaultMarkup);
    }

    [Fact]
    public void SaveSettings_ShouldInsert_IfNoneExists()
    {
        // Arrange
        var newSettings = new AppGlobalSettings
        {
            ServicePercent = 10,
            DefaultMarkup = 200
        };

        // Act
        _repository.SaveSettings(newSettings);

        // Assert
        var saved = _repository.GetSettings();
        Assert.Equal(10, saved.ServicePercent);
        Assert.Equal(200, saved.DefaultMarkup);
    }

    [Fact]
    public void SaveSettings_ShouldUpdate_IfExists()
    {
        // Arrange - Insert first
        _repository.SaveSettings(new AppGlobalSettings { ServicePercent = 10, DefaultMarkup = 200 });

        // Act
        _repository.SaveSettings(new AppGlobalSettings { ServicePercent = 15, DefaultMarkup = 300 });

        // Assert
        var saved = _repository.GetSettings();
        Assert.Equal(15, saved.ServicePercent);
        Assert.Equal(300, saved.DefaultMarkup);
    }
}