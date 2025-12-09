# S3 Upload Tool

Console application for uploading files from `Installer\auto_update_files` folder to Yandex Cloud S3-compatible storage.

## Features

- ✅ Uploads files from `Installer\auto_update_files` folder
- ✅ Checks last modified dates and only uploads new/updated files
- ✅ Supports Yandex Cloud S3-compatible storage
- ✅ Configurable via `s3config.json` file

## Configuration

Create `s3config.json` file in the same directory as the executable with the following structure:

```json
{
  "Endpoint": "https://storage.yandexcloud.kz",
  "AccessKey": "YOUR_ACCESS_KEY",
  "SecretKey": "YOUR_SECRET_KEY",
  "BucketName": "your-bucket-name"
}
```

**Note:** `BucketName` is optional. If not specified, defaults to `"menu-db"`.

**Note:** The default `s3config.json` file is included with placeholder credentials. Make sure to update it with your actual credentials.

## Usage

### Build the application

```batch
cd S3UploadTool
dotnet build -c Release
```

Or use the build script:

```batch
build.bat
```

### Run the application

```batch
dotnet run
```

Or run the executable:

```batch
bin\Release\net9.0\S3UploadTool.exe
```

### Specify custom source folder

```batch
dotnet run "path\to\your\folder"
```

Or:

```batch
S3UploadTool.exe "path\to\your\folder"
```

## How it works

1. The tool reads configuration from `s3config.json`
2. Scans all files in the source folder (default: `Installer\auto_update_files`)
3. For each file:
   - Checks if the file exists in S3
   - Compares last modified dates
   - Uploads only if the local file is newer or doesn't exist in S3
4. Displays a summary of uploaded, skipped, and error files

## Output

The tool provides detailed output:

```
=== S3 Upload Tool ===

Source folder: C:\My\menu\PaymProd\Installer\auto_update_files
S3 Endpoint: https://storage.yandexcloud.kz
S3 Bucket: menu-db

Found 3 file(s) to check:

Uploading: MenuCalc.start.db... OK
Uploading: PaymProdNet9_Setup.exe... OK
Skipped (up to date): update-info.example.json

=== Summary ===
Uploaded: 2
Skipped: 1
Errors: 0
```

## Requirements

- .NET 9.0 SDK or Runtime
- Valid S3 credentials (Access Key and Secret Key)
- Network access to Yandex Cloud storage endpoint

## S3 Bucket Configuration

The bucket name can be configured in `s3config.json` using the `BucketName` property. If not specified, it defaults to `"menu-db"`.

## Security Note

The `s3config.json` file contains sensitive credentials. Make sure to:

- Add it to `.gitignore` if committing to a repository
- Keep it secure and don't share it publicly
- Use environment variables or secure configuration management in production

