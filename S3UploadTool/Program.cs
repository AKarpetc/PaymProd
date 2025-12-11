using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3UploadTool;

class Program
{
    private const string ConfigFileName = "s3config.json";
    private const string DefaultSourceFolder = "files";
    private const string DefaultBucketName = "menu-db"; // Change this to your bucket name

    static async Task Main(string[] args)
    {
        // Настраиваем кодировку консоли для правильного отображения русского текста
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch
        {
            // Игнорируем ошибки установки кодировки
        }

        Console.WriteLine("=== S3 Upload Tool ===");
        Console.WriteLine();

        try
        {
            // Ask user if they want to build installer
            Console.Write("Do you want to build installer? (Y/N): ");
            var buildResponse = Console.ReadLine()?.Trim().ToUpperInvariant();
            
            if (buildResponse == "Y" || buildResponse == "YES" || buildResponse == "Д" || buildResponse == "ДА")
            {
                // Build installer using Inno Setup script
                await BuildInstaller();
            }
            else
            {
                Console.WriteLine("Skipping installer build. Using existing files.");
                Console.WriteLine();
            }

            // Check and update installer file before upload
            CheckAndUpdateInstaller();

            // Load configuration
            var config = LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("ERROR: Configuration file not found or invalid.");
                Console.WriteLine($"Please create '{ConfigFileName}' with your S3 credentials.");
                return;
            }

            if (!Directory.Exists(DefaultSourceFolder))
            {
                Console.WriteLine($"ERROR: Source folder not found: {DefaultSourceFolder}");
                return;
            }

            var bucketName = !string.IsNullOrWhiteSpace(config.BucketName) 
                ? config.BucketName 
                : DefaultBucketName;

            Console.WriteLine($"Source folder: {DefaultSourceFolder}");
            Console.WriteLine($"S3 Endpoint: {config.Endpoint}");
            Console.WriteLine($"S3 Bucket: {bucketName}");
            Console.WriteLine();

            // Create S3 client
            var s3Client = CreateS3Client(config);

            // Get all files from source folder
            var files = Directory.GetFiles(DefaultSourceFolder, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .ToList();

            Console.WriteLine($"Found {files.Count} file(s) to check:");
            Console.WriteLine();

            var uploadedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(DefaultSourceFolder, file.FullName);
                var s3Key = $"auto_update_files/{relativePath.Replace('\\', '/')}";

                try
                {
                    // Check if file needs to be uploaded
                    var shouldUpload = await ShouldUploadFile(s3Client, bucketName, s3Key, file);

                    if (shouldUpload)
                    {
                        Console.Write($"Uploading: {relativePath}... ");
                        await UploadFile(s3Client, bucketName, s3Key, file.FullName);
                        Console.WriteLine("OK");
                        uploadedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"Skipped (up to date): {relativePath}");
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {relativePath} - {ex.Message}");
                    errorCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Uploaded: {uploadedCount}");
            Console.WriteLine($"Skipped: {skippedCount}");
            Console.WriteLine($"Errors: {errorCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static S3Config? LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        
        if (!File.Exists(configPath))
        {
            // Try to find config in solution root
            var solutionRoot = FindSolutionRoot();
            configPath = Path.Combine(solutionRoot, "S3UploadTool", ConfigFileName);
        }

        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<S3Config>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string FindSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PaymProd.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return currentDir;
    }

    /// <summary>
    /// Запускает скрипт сборки установщика build-inno-setup.bat из папки Installer
    /// </summary>
    private static async Task BuildInstaller()
    {
        const string buildScriptPath = @"Installer\build-inno-setup.bat";

        try
        {
            var solutionRoot = FindSolutionRoot();
            var scriptPath = Path.Combine(solutionRoot, buildScriptPath);

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"INFO: Build script not found: {scriptPath}");
                Console.WriteLine("Skipping installer build step.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Building installer using Inno Setup...");
            Console.WriteLine($"Running script: {buildScriptPath}");
            Console.WriteLine();

            var scriptDirectory = Path.GetDirectoryName(scriptPath);
            if (string.IsNullOrEmpty(scriptDirectory))
            {
                Console.WriteLine("ERROR: Cannot determine script directory.");
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c chcp 65001 >nul && \"{scriptPath}\"",
                WorkingDirectory = scriptDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                Console.WriteLine("ERROR: Failed to start build script.");
                return;
            }

            // Читаем вывод процесса асинхронно
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    Console.WriteLine($"ERROR: {e.Data}");
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Ждем завершения процесса с таймаутом (максимум 15 минут)
            var completed = process.WaitForExit(900000); // 15 минут в миллисекундах

            if (!completed)
            {
                Console.WriteLine();
                Console.WriteLine("WARNING: Build script timeout (15 minutes exceeded).");
                Console.WriteLine("Terminating process...");
                try
                {
                    process.Kill();
                    if (!process.WaitForExit(5000))
                    {
                        Console.WriteLine("WARNING: Process did not terminate within 5 seconds.");
                    }
                }
                catch (Exception killEx)
                {
                    Console.WriteLine($"Failed to terminate process: {killEx.Message}");
                }
                Console.WriteLine();
                return;
            }

            // Даем время на завершение асинхронного чтения вывода
            await Task.Delay(1000); // Даем 1 секунду на завершение асинхронных операций чтения

            if (process.ExitCode == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Installer build completed successfully.");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"WARNING: Build script exited with code {process.ExitCode}.");
                Console.WriteLine("Continuing with upload process...");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to run build script: {ex.Message}");
            Console.WriteLine("Continuing with upload process...");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Проверяет файл установщика в Installer\bin\PaymProdNet9_Setup.exe
    /// и копирует его в files, если он новее
    /// </summary>
    private static void CheckAndUpdateInstaller()
    {
        const string installerFileName = "PaymProdNet9_Setup.exe";
        const string installerSourcePath = @"Installer\bin\PaymProdNet9_Setup.exe";
        const string installerTargetPath = @"files\PaymProdNet9_Setup.exe";

        try
        {
            // Находим корень решения для определения абсолютных путей к исходному файлу
            var solutionRoot = FindSolutionRoot();
            var sourcePath = Path.Combine(solutionRoot, installerSourcePath);
            
            // Путь к files - относительно базовой директории приложения (где запущена программа)
            // Это соответствует DefaultSourceFolder = "files"
            var baseDirectory = AppContext.BaseDirectory;
            var targetPath = Path.Combine(baseDirectory, installerTargetPath);
            
            // Если папки files нет, пробуем найти её относительно корня решения
            var targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                // Пробуем путь в S3UploadTool\files
                var altPath = Path.Combine(solutionRoot, "S3UploadTool", installerTargetPath);
                var altDir = Path.GetDirectoryName(altPath);
                if (altDir != null && Directory.Exists(altDir))
                {
                    targetPath = altPath;
                }
            }

            // Проверяем, существует ли исходный файл
            if (!File.Exists(sourcePath))
            {
                Console.WriteLine($"INFO: Installer source not found: {sourcePath}");
                Console.WriteLine("Skipping installer update check.");
                Console.WriteLine();
                return;
            }

            var sourceFile = new FileInfo(sourcePath);
            Console.WriteLine($"Checking installer: {installerFileName}");

            // Проверяем, существует ли целевой файл
            if (!File.Exists(targetPath))
            {
                // Целевого файла нет - копируем
                Console.WriteLine($"Target file not found. Copying installer to {installerTargetPath}...");
                
                // Создаем директорию files, если её нет
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
                Console.WriteLine("Installer copied successfully.");
                Console.WriteLine();
                return;
            }

            var targetFile = new FileInfo(targetPath);

            // Сравниваем даты модификации
            if (sourceFile.LastWriteTimeUtc > targetFile.LastWriteTimeUtc)
            {
                Console.WriteLine($"Source installer is newer. Updating {installerTargetPath}...");
                File.Copy(sourcePath, targetPath, true);
                Console.WriteLine("Installer updated successfully.");
            }
            else
            {
                Console.WriteLine($"Installer is up to date. No update needed.");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to check/update installer: {ex.Message}");
            Console.WriteLine("Continuing with upload process...");
            Console.WriteLine();
        }
    }

    private static IAmazonS3 CreateS3Client(S3Config config)
    {
        var credentials = new BasicAWSCredentials(config.AccessKey, config.SecretKey);

        var s3Config = new AmazonS3Config
        {
            ServiceURL = config.Endpoint,
            ForcePathStyle = true,
            UseHttp = config.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };

        return new AmazonS3Client(credentials, s3Config);
    }

    private static async Task<bool> ShouldUploadFile(
        IAmazonS3 s3Client,
        string bucketName,
        string s3Key,
        FileInfo localFile)
    {
        try
        {
            // Try to get object metadata from S3
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = s3Key
            };

            var response = await s3Client.GetObjectMetadataAsync(request);

            // Compare last modified dates
            var s3LastModified = response.LastModified;
            var localLastModified = localFile.LastWriteTimeUtc;

            // Upload if local file is newer (with 1 second tolerance for timezone differences)
            return localLastModified > s3LastModified.AddSeconds(-1);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // File doesn't exist in S3, should upload
            return true;
        }
    }

    private static async Task UploadFile(
        IAmazonS3 s3Client,
        string bucketName,
        string s3Key,
        string localFilePath)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = s3Key,
            FilePath = localFilePath
        };

        await s3Client.PutObjectAsync(request);
    }
}

class S3Config
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? BucketName { get; set; }
}
