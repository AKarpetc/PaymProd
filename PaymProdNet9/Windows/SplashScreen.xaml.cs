using System.Windows;

namespace PaymProdNet9.Windows;

/// <summary>
/// Окно загрузки при старте приложения
/// </summary>
public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Обновить текст статуса загрузки
    /// </summary>
    public void UpdateStatus(string status)
    {
        StatusText.Text = status;
    }
}

