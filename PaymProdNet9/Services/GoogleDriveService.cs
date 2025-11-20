using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PaymProdNet9.Services;

/// <summary>
/// Сервис для загрузки файлов на Google Drive
/// </summary>
public static class GoogleDriveService
{
    // ID папки на Google Drive, куда будут загружаться файлы
    private const string GoogleDriveFolderId = "1m6KPyu7bTWK6EXOxOUrYK9vYbRAJmrZO";
    
    // URL для открытия папки в браузере
    private const string GoogleDriveFolderUrl = "https://drive.google.com/drive/folders/1m6KPyu7bTWK6EXOxOUrYK9vYbRAJmrZO";
    
    // Путь к файлу с сохраненным токеном
    private static string TokenFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PaymProdNet9", "google_drive_token.txt");

    /// <summary>
    /// Открывает папку Google Drive в браузере для ручной загрузки файла
    /// </summary>
    public static void OpenGoogleDriveFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GoogleDriveFolderUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть Google Drive:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Загружает файл на Google Drive автоматически
    /// Если токен не сохранен, открывает браузер для авторизации
    /// </summary>
    public static async Task<bool> UploadDatabaseToGoogleDriveAsync(string databaseFilePath, Window? owner = null)
    {
        try
        {
            if (!File.Exists(databaseFilePath))
            {
                MessageBox.Show(owner ?? Application.Current.MainWindow,
                    "Файл базы данных не найден!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Пытаемся загрузить сохраненный токен
            string? accessToken = LoadSavedToken();
            
            // Если токена нет, используем упрощенный метод через веб-интерфейс
            if (string.IsNullOrEmpty(accessToken))
            {
                return await UploadDatabaseToGoogleDriveManualAsync(databaseFilePath, owner);
            }

            // Пытаемся загрузить через API
            try
            {
                return await UploadDatabaseToGoogleDriveApiAsync(databaseFilePath, accessToken, owner);
            }
            catch
            {
                // Если токен недействителен, удаляем его и используем ручной метод
                DeleteSavedToken();
                return await UploadDatabaseToGoogleDriveManualAsync(databaseFilePath, owner);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner ?? Application.Current.MainWindow,
                $"Ошибка при загрузке на Google Drive:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Ручная загрузка через веб-интерфейс (если API недоступен)
    /// </summary>
    private static async Task<bool> UploadDatabaseToGoogleDriveManualAsync(string databaseFilePath, Window? owner)
    {
        try
        {
            // Создаем временную копию с понятным именем
            var fileName = $"PaymProdNet9_DB_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            
            File.Copy(databaseFilePath, tempPath, true);

            // Открываем папку с файлом в проводнике
            Process.Start("explorer.exe", $"/select,\"{tempPath}\"");

            // Открываем Google Drive в браузере
            await Task.Delay(500);
            OpenGoogleDriveFolder();

            MessageBox.Show(owner ?? Application.Current.MainWindow,
                "Для автоматической загрузки настройте OAuth токен.\n\n" +
                "Сейчас:\n" +
                "1. Перетащите файл из проводника в открывшуюся папку Google Drive\n" +
                "2. Или используйте кнопку 'Создать' → 'Загрузить файлы'\n\n" +
                $"Файл: {fileName}",
                "Загрузка на Google Drive",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner ?? Application.Current.MainWindow,
                $"Ошибка при подготовке загрузки:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Сохраняет OAuth токен для последующих загрузок
    /// </summary>
    public static void SaveToken(string token)
    {
        try
        {
            var directory = Path.GetDirectoryName(TokenFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(TokenFilePath, token);
        }
        catch
        {
            // Игнорируем ошибки сохранения токена
        }
    }

    /// <summary>
    /// Загружает сохраненный OAuth токен
    /// </summary>
    private static string? LoadSavedToken()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                return File.ReadAllText(TokenFilePath).Trim();
            }
        }
        catch
        {
            // Игнорируем ошибки чтения токена
        }
        
        return null;
    }

    /// <summary>
    /// Удаляет сохраненный токен
    /// </summary>
    private static void DeleteSavedToken()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                File.Delete(TokenFilePath);
            }
        }
        catch
        {
            // Игнорируем ошибки удаления токена
        }
    }

    /// <summary>
    /// Загружает файл на Google Drive через API (требует OAuth токен)
    /// Этот метод можно использовать, если настроен OAuth в Google Cloud Console
    /// </summary>
    public static async Task<bool> UploadDatabaseToGoogleDriveApiAsync(
        string databaseFilePath, 
        string accessToken,
        Window? owner = null)
    {
        try
        {
            if (!File.Exists(databaseFilePath))
            {
                MessageBox.Show(owner ?? Application.Current.MainWindow,
                    "Файл базы данных не найден!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var fileName = Path.GetFileName(databaseFilePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"PaymProdNet9_DB_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // Читаем файл
            var fileBytes = await File.ReadAllBytesAsync(databaseFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Метаданные файла
            var metadata = new
            {
                name = fileName,
                parents = new[] { GoogleDriveFolderId }
            };

            var metadataJson = JsonSerializer.Serialize(metadata);
            var metadataContent = new StringContent(metadataJson, Encoding.UTF8, "application/json");

            // Загружаем файл через multipart upload
            using var formData = new MultipartFormDataContent();
            formData.Add(metadataContent, "metadata");
            formData.Add(fileContent, "file", fileName);

            var response = await httpClient.PostAsync(
                "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart",
                formData);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var fileInfo = JsonSerializer.Deserialize<GoogleDriveFileInfo>(responseContent);

                // Сохраняем токен для следующих раз
                SaveToken(accessToken);

                MessageBox.Show(owner ?? Application.Current.MainWindow,
                    $"✅ База данных успешно загружена на Google Drive!\n\n" +
                    $"Имя файла: {fileInfo?.Name}\n" +
                    $"ID файла: {fileInfo?.Id}\n\n" +
                    $"Файл доступен по ссылке:\n" +
                    $"https://drive.google.com/file/d/{fileInfo?.Id}/view",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Если токен недействителен (401), удаляем его
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    DeleteSavedToken();
                }
                
                throw new Exception($"Ошибка загрузки: {response.StatusCode}\n{errorContent}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner ?? Application.Current.MainWindow,
                $"Ошибка при загрузке на Google Drive:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private class GoogleDriveFileInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}

