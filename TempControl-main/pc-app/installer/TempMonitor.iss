; Inno Setup-script voor TempMonitor.
; Bouwen: voer ..\build-installer.ps1 uit (publiceert de app, haalt PresentMon op en compileert dit script).

#define AppName "TempMonitor"
#ifndef AppVersion
  #define AppVersion "1.1.2"
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
Source: "layout-previews\*.bmp"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\TempMonitor\Firmware installeren"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\flash-firmware.ps1"""; WorkingDir: "{app}"

[Run]
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
  LayoutPage: TWizardPage;
  LayoutChoice: TNewComboBox;
  LayoutPreview: TBitmapImage;
  LayoutDescription: TNewStaticText;

function SelectedLayout: String;
begin
  case LayoutChoice.ItemIndex of
    1: Result := 'gauges';
    2: Result := 'minimal';
  else
    Result := 'classic';
  end;
end;

procedure LayoutChanged(Sender: TObject);
begin
  LayoutPreview.Bitmap.LoadFromFile(ExpandConstant('{tmp}\layout-' + SelectedLayout + '.bmp'));
  case LayoutChoice.ItemIndex of
    1: LayoutDescription.Caption := 'Gauges: twee ronde temperatuurmeters met FPS eronder.';
    2: LayoutDescription.Caption := 'Minimal: grote FPS-weergave met compacte CPU- en GPU-temperaturen.';
  else
    LayoutDescription.Caption := 'Classic: de originele QPRINTS-weergave met CPU, GPU en FPS in drie rijen.';
  end;
end;

procedure InitializeWizard;
var
  SavedLayout: AnsiString;
  InitialLayout: String;
  Note: TNewStaticText;
begin
  ExtractTemporaryFile('layout-classic.bmp');
  ExtractTemporaryFile('layout-gauges.bmp');
  ExtractTemporaryFile('layout-minimal.bmp');
  LayoutPage := CreateCustomPage(wpSelectTasks, 'Kies je schermlayout',
    'Alle layouts tonen CPU-temperatuur, GPU-temperatuur en FPS.');
  LayoutChoice := TNewComboBox.Create(LayoutPage);
  LayoutChoice.Parent := LayoutPage.Surface;
  LayoutChoice.SetBounds(0, ScaleY(12), ScaleX(205), ScaleY(28));
  LayoutChoice.Style := csDropDownList;
  LayoutChoice.Items.Add('Classic');
  LayoutChoice.Items.Add('Gauges');
  LayoutChoice.Items.Add('Minimal');
  LayoutChoice.ItemIndex := 0;
  InitialLayout := 'classic';
  if LoadStringFromFile(AddBackslash(WizardDirValue) + 'layout.txt', SavedLayout) then
    InitialLayout := Trim(String(SavedLayout));
  InitialLayout := ExpandConstant('{param:LAYOUT|' + InitialLayout + '}');
  if InitialLayout = 'gauges' then LayoutChoice.ItemIndex := 1
  else if InitialLayout = 'minimal' then LayoutChoice.ItemIndex := 2
  else if InitialLayout <> 'classic' then
    RaiseException('Onbekende layout. Kies classic, gauges of minimal.');
  LayoutDescription := TNewStaticText.Create(LayoutPage);
  LayoutDescription.Parent := LayoutPage.Surface;
  LayoutDescription.AutoSize := False;
  LayoutDescription.WordWrap := True;
  LayoutDescription.SetBounds(0, ScaleY(58), ScaleX(205), ScaleY(115));
  LayoutPreview := TBitmapImage.Create(LayoutPage);
  LayoutPreview.Parent := LayoutPage.Surface;
  LayoutPreview.SetBounds(LayoutPage.SurfaceWidth - ScaleX(220), 0, ScaleX(220), ScaleY(220));
  LayoutPreview.Stretch := True;
  Note := TNewStaticText.Create(LayoutPage);
  Note.Parent := LayoutPage.Surface;
  Note.AutoSize := False;
  Note.WordWrap := True;
  Note.SetBounds(0, ScaleY(190), ScaleX(205), ScaleY(45));
  Note.Caption := 'Voorbeeld met testwaarden.' + #13#10 + 'Later wisselen? Start Setup opnieuw.';
  LayoutChoice.OnChange := @LayoutChanged;
  LayoutChanged(nil);
end;

function PawnIOInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\PawnIO');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
    if not SaveStringToFile(ExpandConstant('{app}\layout.txt'), SelectedLayout + #13#10, False) then
      RaiseException('De gekozen schermlayout kon niet worden opgeslagen.');
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
  if CurStep = ssPostInstall then
  begin
    WizardForm.StatusLabel.Caption := 'Automatisch starten instellen...';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\task.ps1') +
      '" -Action Install -ExePath "' + ExpandConstant('{app}\TempMonitor.exe') + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      RaiseException('Automatisch starten kon niet worden ingesteld.');
    if ResultCode <> 0 then
      RaiseException('Automatisch starten is mislukt. Zie C:\ProgramData\TempMonitor\task-error.log.');
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
