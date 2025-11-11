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
        {
            System.IO.Directory.CreateDirectory(directory);
        }
            
        Data.DatabaseHelper.InitializeDatabase(dbPath);
    }
}

