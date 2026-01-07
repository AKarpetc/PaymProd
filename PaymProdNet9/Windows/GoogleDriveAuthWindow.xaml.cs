using PaymProdNet9.Services;
using System.Diagnostics;
using System.Windows;

namespace PaymProdNet9.Windows;

public partial class GoogleDriveAuthWindow : Window
{
    // OAuth 2.0 Playground для получения токена
    // Пользователь может использовать этот инструмент для получения токена
    private const string OAuthPlaygroundUrl = "https://developers.google.com/oauthplayground/";

    public GoogleDriveAuthWindow()
    {
        InitializeComponent();
    }

    private void OpenAuthLinkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Открываем OAuth Playground для получения токена
            Process.Start(new ProcessStartInfo
            {
                FileName = OAuthPlaygroundUrl,
                UseShellExecute = true
            });

            // Показываем инструкцию
            MessageBox.Show(
                "В открывшемся браузере:\n\n" +
                "1. В левой панели найдите 'Drive API v3'\n" +
                "2. Выберите 'https://www.googleapis.com/auth/drive.file'\n" +
                "3. Нажмите 'Authorize APIs'\n" +
                "4. Войдите в Google аккаунт\n" +
                "5. Нажмите 'Exchange authorization code for tokens'\n" +
                "6. Скопируйте 'Access token' из правой панели\n" +
                "7. Вставьте токен в поле выше",
                "Инструкция",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии браузера:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveTokenButton_Click(object sender, RoutedEventArgs e)
    {
        var token = TokenTextBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show("Введите OAuth токен!",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            GoogleDriveService.SaveToken(token);
            MessageBox.Show("Токен успешно сохранен!\n\n" +
                            "Теперь загрузка на Google Drive будет автоматической.",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении токена:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}