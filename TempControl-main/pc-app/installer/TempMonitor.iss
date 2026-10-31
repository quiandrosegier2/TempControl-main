; Inno Setup-script voor TempMonitor.
; Bouwen: voer ..\build-installer.ps1 uit (publiceert de app, haalt PresentMon op en compileert dit script).

#define AppName "TempMonitor"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{6F3C2A1E-8B4D-4C7A-9E2F-5D1B3A7C9E40}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=QPRINTS
AppComments=Stuurt CPU/GPU-temperatuur en FPS naar het ronde USB-scherm
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\dist
OutputBaseFilename=TempMonitor-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\TempMonitor.exe
CloseApplications=no

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "pawnio"; Description: "PawnIO-driver installeren (nodig om de CPU-temperatuur te lezen)"; Check: not PawnIOInstalled

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "task.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "flash-firmware.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\TempMonitor\Firmware installeren"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\flash-firmware.ps1"""; WorkingDir: "{app}"

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\task.ps1"" -Action Install -ExePath ""{app}\TempMonitor.exe"""; \
  StatusMsg: "Automatisch starten instellen..."; Flags: runhidden waituntilterminated
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
  Parameters: "-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\flash-firmware.ps1"""; \
  Description: "Firmware op het aangesloten Waveshare-scherm installeren"; Flags: postinstall skipifsilent waituntilterminated runhidden

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\task.ps1"" -Action Uninstall"; \
  RunOnceId: "RemoveTask"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\TempMonitor"

[Code]
var
  DriverNeedsRestart: Boolean;

function PawnIOInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\PawnIO');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('pawnio') and not PawnIOInstalled then
  begin
    WizardForm.StatusLabel.Caption := 'PawnIO-driver installeren...';
    if not Exec(ExpandConstant('{app}\drivers\PawnIO_setup.exe'), '-install -silent',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException('PawnIO kon niet worden gestart.');
    if (ResultCode = 3010) or (ResultCode = 1641) then
      DriverNeedsRestart := True
    else if ResultCode <> 0 then
      RaiseException(Format('PawnIO-installatie mislukt (code %d).', [ResultCode]));
    if not PawnIOInstalled then
      RaiseException('PawnIO-driver ontbreekt na installatie. Installeer de driver opnieuw.');
  end;
end;

function NeedRestart: Boolean;
begin
  Result := DriverNeedsRestart;
end;

// Een draaiende versie stoppen voordat de bestanden overschreven worden (bij een update).
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -Command "Stop-ScheduledTask -TaskName TempMonitor -EA SilentlyContinue; Stop-Process -Name TempMonitor,PresentMon -Force -EA SilentlyContinue"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
