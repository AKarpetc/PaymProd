using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
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

    /// <summary>
    ///     Проверяет наличие новой версии и при необходимости запускает установщик.
    /// </summary>
    public static async Task CheckForUpdatesAsync(Window? owner = null)
    {
        try
        {
            var json = await HttpClient.GetStringAsync(UpdateInfoUrl);
            var info = JsonSerializer.Deserialize<UpdateInfo>(json);
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

    private static async Task DownloadInstallerAsync(string url, string destinationPath, Window? owner, Version remoteVersion)
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

                    progressWindow.SetVersion(remoteVersion.ToString());
                    progressWindow.Show();
                });
            }

            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

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

    private sealed class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonPropertyName("installerUrl")]
        public string InstallerUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("releaseNotes")]
        public string? ReleaseNotes { get; set; }
    }
}

