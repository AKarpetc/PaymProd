using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace PaymProdNet9.Services;

/// <summary>
/// Сервис для загрузки файлов в S3-совместимое хранилище.
/// Настройки берутся из переменных окружения:
///   PAYMPROD_S3_ENDPOINT   - URL S3 совместимого сервиса (например, https://storage.yandexcloud.kz)
///   PAYMPROD_S3_ACCESS_KEY - идентификатор ключа
///   PAYMPROD_S3_SECRET_KEY - секретный ключ
/// Бакет задается в коде (menu-db).
/// </summary>
public static class S3UploadService
{
    private const string DefaultBucketName = "menu-db";

    private static IAmazonS3 CreateClient()
    {
        // 1. Пытаемся прочитать из переменных окружения
        var endpoint = Environment.GetEnvironmentVariable("PAYMPROD_S3_ENDPOINT");
        var accessKey = Environment.GetEnvironmentVariable("PAYMPROD_S3_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("PAYMPROD_S3_SECRET_KEY");

        // 2. Если ключей нет в окружении – пробуем локальный конфиг s3settings.local.json
        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            TryLoadFromLocalConfig(out var cfgEndpoint, out var cfgAccess, out var cfgSecret);

            if (!string.IsNullOrWhiteSpace(cfgEndpoint))
                endpoint = cfgEndpoint;
            if (!string.IsNullOrWhiteSpace(cfgAccess))
                accessKey = cfgAccess;
            if (!string.IsNullOrWhiteSpace(cfgSecret))
                secretKey = cfgSecret;
        }

        // Значение по умолчанию для endpoint, если так и не нашли
        endpoint ??= "https://storage.yandexcloud.kz";

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Не заданы ключи доступа для S3. " +
                "Установите переменные окружения PAYMPROD_S3_ACCESS_KEY / PAYMPROD_S3_SECRET_KEY " +
                "или создайте файл s3settings.local.json рядом с исполняемым файлом.");
        }

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            UseHttp = false
        };

        return new AmazonS3Client(credentials, config);
    }

    /// <summary>
    /// Попытка загрузить настройки S3 из локального файла s3settings.local.json
    /// (лежит рядом с PaymProdNet9.exe, НЕ коммитится в репозиторий).
    /// Формат:
    /// {
    ///   "Endpoint": "https://storage.yandexcloud.kz",
    ///   "AccessKey": "XXX",
    ///   "SecretKey": "YYY"
    /// }
    /// </summary>
    private static void TryLoadFromLocalConfig(out string? endpoint, out string? accessKey, out string? secretKey)
    {
        endpoint = null;
        accessKey = null;
        secretKey = null;

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, "s3settings.local.json");
            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Endpoint", out var epProp) && epProp.ValueKind == JsonValueKind.String)
                endpoint = epProp.GetString();
            if (root.TryGetProperty("AccessKey", out var akProp) && akProp.ValueKind == JsonValueKind.String)
                accessKey = akProp.GetString();
            if (root.TryGetProperty("SecretKey", out var skProp) && skProp.ValueKind == JsonValueKind.String)
                secretKey = skProp.GetString();
        }
        catch (Exception ex)
        {
            Logger.Warning($"Не удалось прочитать s3settings.local.json: {ex.Message}");
        }
    }

    /// <summary>
    /// Загружает файл в S3 в указанную "папку" (prefix/username/filename).
    /// </summary>
    public static async Task<string> UploadFileAsync(string localPath, string prefix)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
            throw new FileNotFoundException("Файл для загрузки не найден", localPath);

        if (string.IsNullOrWhiteSpace(prefix))
            prefix = "uploads";

        var userName = Environment.UserName;
        var fileName = Path.GetFileName(localPath);

        // Ключ объекта: prefix/username/filename
        var key = $"{prefix.TrimEnd('/')}/{userName}/{fileName}";

        using var client = CreateClient();

        await using var stream = File.OpenRead(localPath);

        var request = new PutObjectRequest
        {
            BucketName = DefaultBucketName,
            Key = key,
            InputStream = stream
        };

        var response = await client.PutObjectAsync(request);

        Logger.Info($"Файл '{localPath}' загружен в S3: bucket={DefaultBucketName}, key={key}, HTTP {response.HttpStatusCode}");

        return key;
    }
}


