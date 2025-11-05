using System.Windows;

namespace PaymProdNet9;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Инициализация базы данных при старте приложения
        var dbPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "MenuCalc.db");
            
        Data.DatabaseHelper.InitializeDatabase(dbPath);
    }
}

