; TimeFold Inno Setup Script
; Version and app metadata are injected via /D defines from CI.
; Context-menu registration is optional and UNCHECKED by default.

#define MyAppName "TimeFold"
#define MyAppPublisher "Appsphinx"
#define MyAppURL "https://github.com/chandrath/TimeFold-File-Folder-Organizer"
#define MyAppExeName "TimeFold.exe"
; MyAppVersion is injected at compile time: iscc /DMyAppVersion=1.1.0 timefold.iss
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

[Setup]
AppId={{A7F3C2D1-8E4B-4F6A-9C0E-2B5D7F1A3E8C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppPublisher}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableDirPage=auto
DisableProgramGroupPage=auto
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
#ifdef OutputBaseName
OutputBaseFilename={#OutputBaseName}
#else
OutputBaseFilename=TimeFold-{#MyAppVersion}-win-x64-Setup
#endif
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
; Both context-menu options are UNCHECKED by default
Name: "contextmenu_folder"; Description: "Add ""Open in TimeFold"" to folder right-click menu"; GroupDescription: "Windows Explorer Integration (optional):"; Flags: unchecked
Name: "contextmenu_background"; Description: "Add ""Open this folder in TimeFold"" to folder background right-click menu"; GroupDescription: "Windows Explorer Integration (optional):"; Flags: unchecked

[Files]
Source: "..\publish_installer\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Install-presence marker — read by TimeFold to detect installed vs portable mode.
; NEVER contains user data. Removed cleanly on uninstall.
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey

; Context menu: right-click a FOLDER → "Open in TimeFold"
Root: HKCR; Subkey: "Directory\shell\TimeFold"; ValueType: string; ValueData: "Open in TimeFold"; Tasks: contextmenu_folder; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\shell\TimeFold"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: contextmenu_folder
Root: HKCR; Subkey: "Directory\shell\TimeFold\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: contextmenu_folder

; Context menu: right-click empty space INSIDE a folder → "Open this folder in TimeFold"
Root: HKCR; Subkey: "Directory\Background\shell\TimeFold"; ValueType: string; ValueData: "Open this folder in TimeFold"; Tasks: contextmenu_background; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\TimeFold"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: contextmenu_background
Root: HKCR; Subkey: "Directory\Background\shell\TimeFold\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%V"""; Tasks: contextmenu_background

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: dirifempty; Name: "{app}"
