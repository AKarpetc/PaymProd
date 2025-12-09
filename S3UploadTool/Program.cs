using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Console.WriteLine("=== S3 Upload Tool ===");
        Console.WriteLine();

        try
        {
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
