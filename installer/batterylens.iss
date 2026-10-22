; BatteryLens installer. Build with scripts\build-installer.ps1 (publishes a self-contained build to dist\stage first).
#define AppName "BatteryLens"
#define AppFullName "BatteryLens - Laptop Battery Health & Capacity Analyzer"
#ifndef AppVersion
  #define AppVersion "1.1.0"
#endif
#define StageDir "..\dist\stage"

[Setup]
AppId={{085E4356-5A75-51D8-AF8E-6D1AA29EDF19}
AppName={#AppFullName}
AppVersion={#AppVersion}
AppPublisher=Md Anamul Hasan
AppPublisherURL=https://github.com/md-anamul-hasan/batterylens
AppSupportURL=https://github.com/md-anamul-hasan/batterylens/issues
DefaultDirName={autopf}\BatteryLens
DefaultGroupName=BatteryLens
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\BatteryLens.exe
UninstallDisplayName={#AppFullName}
SetupIconFile=..\Assets/icon.ico
LicenseFile=..\LICENSE
OutputDir=..\dist\installer
OutputBaseFilename=BatteryLens-Setup-{#AppVersion}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\BatteryLens"; Filename: "{app}\BatteryLens.exe"
Name: "{autodesktop}\BatteryLens"; Filename: "{app}\BatteryLens.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\BatteryLens.exe"; Description: "Launch BatteryLens"; Flags: nowait postinstall skipifsilent
