using System;
using System.Windows;

namespace PaymProdNet9.Windows;

public partial class UpdateDownloadWindow : Window
{
    public UpdateDownloadWindow()
    {
        InitializeComponent();
    }

    public void SetVersion(string version)
    {
        Dispatcher.Invoke(() =>
        {
            VersionTextBlock.Text = $"Версия: {version}";
        });
    }

    public void UpdateProgress(double? fraction, long downloadedBytes, long totalBytes)
    {
        Dispatcher.Invoke(() =>
        {
            if (fraction.HasValue)
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = Math.Clamp(fraction.Value * 100, 0, 100);
                DetailsTextBlock.Text = $"Скачано {FormatBytes(downloadedBytes)} из {FormatBytes(totalBytes)}";
            }
            else
            {
                DownloadProgressBar.IsIndeterminate = true;
                DetailsTextBlock.Text = $"Скачано {FormatBytes(downloadedBytes)}";
            }
        });
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 МБ";
        }

        var mb = bytes / 1024d / 1024d;
        return $"{mb:F2} МБ";
    }
}

