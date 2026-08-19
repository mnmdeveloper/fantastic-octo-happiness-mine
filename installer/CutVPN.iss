; CutVPN visible, transparent Windows installer.
; Build with Inno Setup 6 after publishing ui/CutVPN.csproj.

#define MyAppName "CutVPN"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CutVPN Project"
#define MyAppExeName "CutVPN.exe"
#define PublishDir "..\ui\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{7C1D6D66-7E4A-4E7D-B2A9-CUTVPN10000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\CutVPN
DefaultGroupName=CutVPN
DisableProgramGroupPage=yes
OutputDir=.\out
OutputBaseFilename=CutVPN-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=classic
PrivilegesRequired=lowest
Uninstallable=yes
UninstallDisplayName=CutVPN

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"
Name: "startup"; Description: "Запускать CutVPN при входе в Windows"; GroupDescription: "Дополнительные настройки:"

[Files]
Source: "{#PublishDir}\CutVPN.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\CutVPN"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\CutVPN"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\CutVPN"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить CutVPN"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\CutVPN"

[Code]
procedure InitializeWizard;
begin
  WizardForm.Color := clSilver;
  WizardForm.Caption := 'CutVPN — Мастер шиттинга Чебурнета';
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];
  WizardForm.WelcomeLabel1.Font.Color := clNavy;
  WizardForm.WelcomeLabel2.Caption :=
    'Добро пожаловать в исключительно серьёзный установщик CutVPN.' + #13#10 +
    'Внутри: сетевые настройки, Генсуха, вязанка и немного Чебурнета.' + #13#10 +
    'Нажмите «Далее», чтобы продолжить абсолютно необходимую процедуру.';
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectDir then
    WizardForm.NextButton.Caption := 'Начать шиттинг >';
  if CurPageID = wpReady then
    WizardForm.NextButton.Caption := 'Установить!';
  if CurPageID = wpFinished then
    WizardForm.NextButton.Caption := 'Готово';
end;
