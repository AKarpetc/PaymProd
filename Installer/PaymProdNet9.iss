; Inno Setup Script для PaymProdNet9
; Требуется Inno Setup 6.0 или новее
; Скачать: https://jrsoftware.org/isdl.php

#define MyAppName "PaymProdNet9"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "PaymProd Team"
#define MyAppURL "https://github.com/your-repo"
#define MyAppExeName "PaymProdNet9.exe"
#define MyAppId "A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D"

[Setup]
; Основные настройки
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
OutputDir=bin
OutputBaseFilename=PaymProdNet9_Setup
SetupIconFile=..\PaymProdNet9\Resources\Restaurant_Blue_2.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Язык
LanguageDetectionMethod=locale

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; Основной исполняемый файл и все зависимости
Source: "..\PaymProdNet9\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Проверка наличия .NET 9.0 Runtime (если приложение не self-contained)
function InitializeSetup(): Boolean;
var
  Version: String;
begin
  Result := True;
  // Если приложение self-contained, эта проверка не нужна
  // Раскомментируйте, если нужно проверить наличие .NET
  {
  if not RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedframework\Microsoft.NETCore.App', '9.0.0', Version) then
  begin
    MsgBox('Для работы приложения требуется .NET 9.0 Runtime.' + #13#10 +
           'Пожалуйста, установите его с https://dotnet.microsoft.com/download/dotnet/9.0',
           mbError, MB_OK);
    Result := False;
  end;
  }
end;

