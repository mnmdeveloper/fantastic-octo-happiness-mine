; CutVPN installer — visible, transparent installer for your own/administered PC.
; Build with Inno Setup 6.

#define MyAppName "CutVPN"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "mnmdeveloper"
#define MyAppExeName "CutVPN.exe"

[Setup]
AppId={{A8E1C1D7-7E5D-4E2E-9F8B-CUTVPN000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\CutVPN
DefaultGroupName=CutVPN
OutputDir=..\dist
OutputBaseFilename=CutVPN-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=classic
PrivilegesRequired=lowest

[Files]
Source: "..\ui\bin\Release\net8.0-windows\win-x64\publish\CutVPN.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\CutVPN"; Filename: "{app}\CutVPN.exe"
Name: "{autodesktop}\CutVPN"; Filename: "{app}\CutVPN.exe"

[Run]
Filename: "{app}\CutVPN.exe"; Description: "Launch CutVPN"; Flags: nowait postinstall skipifsilent
