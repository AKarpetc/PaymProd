using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using PaymProdNet9.Data;
using PaymProdNet9.Windows;

namespace PaymProdNet9.Services;

/// <summary>
///     Сервис проверки и загрузки обновлений приложения
/// </summary>
public static class UpdateService
{
    /// <summary>
    ///     URL JSON-файла с информацией об обновлении (на Google Drive).
    ///     Замените ID на свой при публикации.
    /// </summary>
    private const string UpdateInfoUrl =
        "https://drive.usercontent.google.com/download?id=1hYG95uNWXmRveYMkROKTUA3c-ytYtgMR&export=download&authuser=0&confirm=t&uuid=b4a6a6f8-d4ee-4744-b3ff-2ca6ed16e4d2&at=ALWLOp7xc-Qwoq6XLX7sx1eiI4_7:1763457785182";

    private static readonly HttpClient HttpClient = new();
    private static UpdateInfo? _cachedUpdateInfo;

    /// <summary>
    ///     Проверяет наличие новой версии и при необходимости запускает установщик.
    /// </summary>
    public static async Task CheckForUpdatesAsync(Window? owner = null)
    {
        try
        {
            var info = await GetUpdateInfoAsync();
            if (info == null ||
                string.IsNullOrWhiteSpace(info.Version) ||
                string.IsNullOrWhiteSpace(info.InstallerUrl))
            {
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (!Version.TryParse(info.Version, out var remoteVersion) || remoteVersion <= currentVersion)
            {
                return; // уже последняя версия
            }

            var message = $"Доступна новая версия {remoteVersion} (у вас {currentVersion}).";
            if (!string.IsNullOrWhiteSpace(info.ReleaseNotes))
            {
                message += $"\n\nИзменения:\n{info.ReleaseNotes}";
            }

            message += "\n\nСкачать и установить обновление сейчас?";

            var result = MessageBox.Show(owner ?? Application.Current.MainWindow,
                message,
                "Обновление доступно",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var tempInstallerPath = GetTempInstallerPath(info.InstallerUrl, remoteVersion);
            await DownloadInstallerAsync(info.InstallerUrl, tempInstallerPath, owner ?? Application.Current.MainWindow, remoteVersion);

            Process.Start(new ProcessStartInfo(tempInstallerPath)
            {
                UseShellExecute = true
            });

            Application.Current.Shutdown(); // завершаем текущую версию перед обновлением
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            // Ошибку намеренно не показываем пользователю, чтобы не мешать работе приложения.
        }
    }

    public static async Task<UpdateInfo?> GetUpdateInfoAsync()
    {
        if (_cachedUpdateInfo != null)
        {
            return _cachedUpdateInfo;
        }

        try
        {
            var json = await HttpClient.GetStringAsync(UpdateInfoUrl);
            var info = JsonSerializer.Deserialize<UpdateInfo>(json);
            if (info == null)
            {
                return null;
            }

            _cachedUpdateInfo = info;
            return info;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch update info: {ex.Message}");
            return null;
        }
    }

    private static async Task DownloadInstallerAsync(string url, string destinationPath, Window? owner, Version remoteVersion)
    {
        await DownloadFileAsync(url, destinationPath, owner, "Скачивание обновления", $"Версия: {remoteVersion}");
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, Window? owner, string title, string? subtitle)
    {
        UpdateDownloadWindow? progressWindow = null;
        try
        {
            if (owner != null)
            {
                await owner.Dispatcher.InvokeAsync(() =>
                {
                    progressWindow = new UpdateDownloadWindow
                    {
                        Owner = owner
                    };

                    progressWindow.SetTitle(title);
                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        progressWindow.SetSubtitle(subtitle);
                    }
                    progressWindow.Show();
                });
            }

            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloaded = 0;
            var buffer = new byte[81920];

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(destinationPath);

            while (true)
            {
                var read = await input.ReadAsync(buffer);
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;

                if (progressWindow != null)
                {
                    var fraction = totalBytes > 0 ? (double)downloaded / totalBytes : (double?)null;
                    progressWindow.UpdateProgress(fraction, downloaded, totalBytes);
                }
            }
        }
        finally
        {
            if (progressWindow != null)
            {
                await progressWindow.Dispatcher.InvokeAsync(progressWindow.Close);
            }
        }
    }

    public static async Task<bool> TryDownloadStartDatabaseAsync(
        string targetPath,
        Window? owner = null,
        bool replaceExisting = false,
        bool silentSuccess = false)
    {
        Window? messageOwner = owner ?? Application.Current.MainWindow;

        try
        {
            var info = await GetUpdateInfoAsync();
            if (info == null || string.IsNullOrWhiteSpace(info.StartDb))
            {
                return false;
            }

            var hasExistingDb = File.Exists(targetPath);
            var willBackup = replaceExisting && hasExistingDb;
            var message = hasExistingDb
                ? willBackup
                    ? "Заменить текущую базу данных подготовленным набором?\n\nПеред заменой будет создана резервная копия."
                    : "Заменить текущую базу данных подготовленным набором?"
                : "Скачать подготовленные данные и начать работу с ними?";

            var dialogResult = MessageBox.Show(messageOwner ?? Application.Current.MainWindow,
                message,
                "Стартовая база данных",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (dialogResult != MessageBoxResult.Yes)
            {
                return false;
            }

            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string? backupPath = null;
            if (willBackup)
            {
                try
                {
                    backupPath = DatabaseBackupHelper.CreateAutoBackup();
                }
                catch (Exception backupEx)
                {
                    var continueResult = MessageBox.Show(messageOwner ?? Application.Current.MainWindow,
                        $"Не удалось создать резервную копию:\n{backupEx.Message}\n\nПродолжить без резервного копирования?",
                        "Внимание",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (continueResult != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                }
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"PaymProdStartDb_{Guid.NewGuid():N}.db");
            try
            {
                await DownloadFileAsync(info.StartDb, tempFile, messageOwner, "Скачивание стартовой базы данных", "Подготовленные данные");

                SqliteConnection.ClearAllPools();
                File.Copy(tempFile, targetPath, true);

                DatabaseHelper.InitializeDatabase(targetPath);

                if (!silentSuccess)
                {
                    var successMessage = "Стартовая база данных готова к использованию.\nДля применения изменений рекомендуется перезапустить приложение.";
                    if (!string.IsNullOrWhiteSpace(backupPath))
                    {
                        successMessage += $"\n\nРезервная копия сохранена в:\n{backupPath}";
                    }

                    MessageBox.Show(messageOwner ?? Application.Current.MainWindow,
                        successMessage,
                        "Готово",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return true;
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch
                    {
                        // Игнорируем ошибки очистки
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(messageOwner ?? Application.Current.MainWindow,
                $"Не удалось скачать стартовую базу данных:\n{ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private static string GetTempInstallerPath(string installerUrl, Version version)
    {
        var extension = Path.GetExtension(installerUrl);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            extension = ".exe";
        }

        var fileName = $"PaymProdNet9_{version}{extension}".Replace(" ", "_");
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    public sealed class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonPropertyName("installerUrl")]
        public string InstallerUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("releaseNotes")]
        public string? ReleaseNotes { get; set; }

        [JsonPropertyName("startDb")]
        public string? StartDb { get; set; }
    }
}

