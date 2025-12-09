@echo off
echo Building S3UploadTool...
dotnet build -c Release
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Executable location: bin\Release\net9.0\S3UploadTool.exe
) else (
    echo.
    echo Build failed!
    exit /b 1
)

