; Inno Setup-script voor TempMonitor.
; Bouwen: voer ..\build-installer.ps1 uit (publiceert de app, haalt PresentMon op en compileert dit script).

#define AppName "Qprints esp installer"
#define PreviewFrameCount 40
#ifndef AppVersion
  #define AppVersion "1.3.1"
#endif

[Setup]
AppId={{6F3C2A1E-8B4D-4C7A-9E2F-5D1B3A7C9E40}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=QPRINTS
AppComments=Stuurt CPU/GPU-temperatuur en FPS naar het ronde USB-scherm
DefaultDirName={autopf}\TempMonitor
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\dist
OutputBaseFilename=Qprints esp installer-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dark includetitlebar
DisableWelcomePage=no
SetupIconFile=branding\qprints.ico
WizardImageFile=branding\qprints-banner.png
WizardSmallImageFile=branding\qprints-mark.png
WizardImageBackColor=#17181e
WizardSmallImageBackColor=#202020
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\branding\qprints.ico
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
Source: "layout-previews\layout-classic.bmp"; Flags: dontcopy solidbreak
Source: "layout-previews\layout-gauges.bmp"; Flags: dontcopy solidbreak
Source: "layout-previews\layout-minimal.bmp"; Flags: dontcopy solidbreak
Source: "preview-animation\*.bmp"; Flags: dontcopy solidbreak
Source: "branding\qprints-footer.bmp"; Flags: dontcopy
Source: "branding\qprints-wordmark.png"; DestDir: "{app}\branding"; Flags: ignoreversion
Source: "branding\qprints.ico"; DestDir: "{app}\branding"; Flags: ignoreversion

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
const
  PreviewFrameCount = {#PreviewFrameCount};

// Inno Setup 6 gebruikt een 32-bit wizard, ook bij een x64-installatie.
function SetTimer(hWnd: HWND; nIDEvent: LongWord; uElapse: LongWord; lpTimerFunc: LongWord): LongWord;
  external 'SetTimer@user32.dll stdcall';
function KillTimer(hWnd: HWND; uIDEvent: LongWord): Integer;
  external 'KillTimer@user32.dll stdcall';

var
  DriverNeedsRestart: Boolean;
  LayoutPage: TWizardPage;
  LayoutChoice: TNewComboBox;
  LayoutPreview: TBitmapImage;
  LayoutDescription: TNewStaticText;
  LayoutIds, LayoutDescriptions: TStringList;
  BrandLogo: TBitmapImage;
  PreviewButton: TNewButton;
  PreviewFrame, PreviewTickCount: Integer;
  PreviewTimer, PreviewCallback: LongWord;
  PreviewPaused, PreviewClosing, PreviewPageActive, PreviewBusy: Boolean;

function SelectedLayout: String;
begin
  Result := 'classic';
  if LayoutChoice.ItemIndex >= 0 then Result := LayoutIds[LayoutChoice.ItemIndex];
end;

procedure StopPreview;
begin
  if PreviewTimer <> 0 then
  begin
    KillTimer(0, PreviewTimer);
    PreviewTimer := 0;
  end;
end;

procedure LoadPreviewFrame;
var
  Name, Path: String;
begin
  if LayoutChoice.ItemIndex < 3 then
    Name := 'layout-' + SelectedLayout + '.bmp'
  else
    Name := Format('%s-%.2d.bmp', [SelectedLayout, PreviewFrame]);
  Path := ExpandConstant('{tmp}\' + Name);
  if not FileExists(Path) then ExtractTemporaryFile(Name);
  LayoutPreview.Bitmap.LoadFromFile(Path);
end;

procedure AdvancePreview;
begin
  if LayoutChoice.ItemIndex < 3 then Exit;
  PreviewFrame := (PreviewFrame + 1) mod PreviewFrameCount;
  LoadPreviewFrame;
  Inc(PreviewTickCount);
end;

procedure PreviewTick(Arg1: HWND; Arg2, Arg3, Arg4: LongWord);
begin
  if PreviewClosing or PreviewBusy or not PreviewPageActive or PreviewPaused then Exit;
  if (PreviewTimer = 0) or (Arg3 <> PreviewTimer) then Exit;
  if (LayoutChoice = nil) or (LayoutChoice.ItemIndex < 3) then Exit;
  PreviewBusy := True;
  try
    try
      AdvancePreview;
    except
      StopPreview;
      PreviewPaused := True;
      PreviewButton.Caption := 'Afspelen';
      Log('Animatievoorbeeld gestopt: ' + GetExceptionMessage);
    end;
  finally
    PreviewBusy := False;
  end;
end;

procedure StartPreview;
begin
  if PreviewClosing or not PreviewPageActive or PreviewPaused then Exit;
  if LayoutChoice.ItemIndex < 3 then Exit;
  if PreviewCallback = 0 then PreviewCallback := CreateCallback(@PreviewTick);
  if PreviewTimer = 0 then PreviewTimer := SetTimer(0, 0, 100, PreviewCallback);
end;

procedure TogglePreview(Sender: TObject);
begin
  PreviewPaused := not PreviewPaused;
  if PreviewPaused then
  begin
    StopPreview;
    PreviewButton.Caption := 'Afspelen';
  end
  else
  begin
    StartPreview;
    PreviewButton.Caption := 'Pauzeren';
  end;
end;

procedure LayoutChanged(Sender: TObject);
begin
  StopPreview;
  PreviewFrame := 0;
  PreviewPaused := False;
  LoadPreviewFrame;
  PreviewButton.Enabled := LayoutChoice.ItemIndex >= 3;
  if PreviewButton.Enabled then PreviewButton.Caption := 'Pauzeren'
  else PreviewButton.Caption := 'Statisch';
  if LayoutChoice.ItemIndex >= 0 then
    LayoutDescription.Caption := LayoutDescriptions[LayoutChoice.ItemIndex];
  StartPreview;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if LayoutPage = nil then Exit;
  PreviewPageActive := CurPageID = LayoutPage.ID;
  if PreviewPageActive then StartPreview else StopPreview;
end;

procedure DeinitializeSetup;
begin
  PreviewClosing := True;
  StopPreview;
end;

procedure AddLayout(Id, Title, Description: String);
begin
  LayoutIds.Add(Id);
  LayoutChoice.Items.Add(Title);
  LayoutDescriptions.Add(Description);
end;

procedure InitializeWizard;
var
  SavedLayout: AnsiString;
  InitialLayout: String;
  Note: TNewStaticText;
  Index, PreviewSize: Integer;
begin
  ExtractTemporaryFile('qprints-footer.bmp');
  BrandLogo := TBitmapImage.Create(WizardForm);
  BrandLogo.Parent := WizardForm;
  BrandLogo.Bitmap.LoadFromFile(ExpandConstant('{tmp}\qprints-footer.bmp'));
  BrandLogo.Stretch := True;
  BrandLogo.SetBounds(ScaleX(12), WizardForm.ClientHeight - ScaleY(43), ScaleX(150), ScaleY(34));
  BrandLogo.Anchors := [akLeft, akBottom];
  LayoutIds := TStringList.Create;
  LayoutDescriptions := TStringList.Create;
  LayoutPage := CreateCustomPage(wpSelectTasks, 'Kies je schermlayout',
    '21 layouts, waarvan 18 bewegend. Nieuwe thema''s vereisen de meegeleverde firmware.');
  LayoutChoice := TNewComboBox.Create(LayoutPage);
  LayoutChoice.Parent := LayoutPage.Surface;
  LayoutChoice.SetBounds(0, ScaleY(12), ScaleX(205), ScaleY(28));
  LayoutChoice.Style := csDropDownList;
  AddLayout('classic', 'Classic', 'De originele QPRINTS-weergave met CPU, GPU en FPS in drie rijen.');
  AddLayout('gauges', 'Gauges', 'Twee ronde temperatuurmeters met FPS eronder.');
  AddLayout('minimal', 'Minimal', 'Grote FPS-weergave met compacte CPU- en GPU-temperaturen.');
  AddLayout('orbit', 'Orbit (bewegend)', 'Lichtpunten draaien in drie banen rond de FPS-weergave. Temperaturen staan onderaan.');
  AddLayout('radar', 'Radar (bewegend)', 'Een draaiende radarsweep met meetwaarden in drie goed leesbare panelen.');
  AddLayout('neon-pulse', 'Neon Pulse (bewegend)', 'Pulserende neonranden en temperatuur- en FPS-balken op een donkere achtergrond.');
  AddLayout('reactor', 'Reactor (bewegend)', 'Tegengesteld draaiende segmentringen en een pulserende zeshoek rond de FPS.');
  AddLayout('waveform', 'Waveform (bewegend)', 'Echte CPU- en GPU-temperatuurhistorie van de laatste 60 samples, met bewegende scanlijnen.');
  AddLayout('matrix', 'Matrix (bewegend)', 'Vallende cijferkolommen achter drie donkere panelen met de meetwaarden.');
  AddLayout('starfield', 'Starfield (bewegend)', 'Een bewegend sterrenveld met grote FPS en temperaturen onderaan.');
  AddLayout('speedometer', 'Speedometer (bewegend)', 'Drie onafhankelijke meters: grote FPS-meter (0-240) en kleinere CPU- en GPU-meters (0-120 C), met soepele naalden en echte digitale meetwaarden.');
  AddLayout('aurora', 'Aurora (bewegend)', 'Zacht golvende lichtgordijnen achter een centrale FPS-weergave.');
  AddLayout('hex-grid', 'Hex Grid (bewegend)', 'Een golvend verlicht honingraatraster met drie zeshoekige meetpanelen.');
  AddLayout('arsenal', 'Arsenal (gaming)', 'Geanimeerd geweersilhouet, mondingsflitsen en uitgeworpen hulzen rond een tactische FPS- en temperatuur-HUD.');
  AddLayout('bullet-storm', 'Bullet Storm (gaming)', 'Bewegende gouden patronen en lichtsporen achter een donkere combat-HUD.');
  AddLayout('inferno', 'Inferno (gaming)', 'Golvende vlammen en opstijgende vonken, met helder leesbare echte meetwaarden.');
  AddLayout('warzone-city', 'Warzone City (gaming)', 'Een bewegende skyline met gebouwen, brandhaarden en lichtsporen in een urban-combat-sfeer.');
  AddLayout('sniper-scope', 'Sniper Scope (gaming)', 'Groene viziermarkeringen, een bewegend doelkader en tactische uitlezing van CPU, GPU en FPS.');
  AddLayout('cyber-ops', 'Cyber Ops (gaming)', 'Neoncircuits, reizende datapulsen en een cyberpunk-HUD.');
  AddLayout('plasma-core', 'Plasma Core (gaming)', 'Een pulserende energiekern met elektrische bogen en een futuristische wapen-HUD.');
  AddLayout('boss-fight', 'Boss Fight (gaming)', 'Een bewegende pixelbaas met laseraccenten en echte CPU- en GPU-hittebalken.');
  // Alleen de drie statische voorbeelden vooraf laden; animatiebeelden volgen op aanvraag.
  for Index := 0 to 2 do
    ExtractTemporaryFile('layout-' + LayoutIds[Index] + '.bmp');
  LayoutChoice.ItemIndex := 0;
  InitialLayout := 'classic';
  if LoadStringFromFile(AddBackslash(WizardDirValue) + 'layout.txt', SavedLayout) then
    InitialLayout := Trim(String(SavedLayout));
  InitialLayout := ExpandConstant('{param:LAYOUT|' + InitialLayout + '}');
  LayoutChoice.ItemIndex := -1;
  for Index := 0 to LayoutIds.Count - 1 do
    if InitialLayout = LayoutIds[Index] then LayoutChoice.ItemIndex := Index;
  if LayoutChoice.ItemIndex < 0 then
    RaiseException('Onbekende layout. Kies een van de eenentwintig ondersteunde layouts.');
  LayoutDescription := TNewStaticText.Create(LayoutPage);
  LayoutDescription.Parent := LayoutPage.Surface;
  LayoutDescription.AutoSize := False;
  LayoutDescription.WordWrap := True;
  LayoutDescription.SetBounds(0, ScaleY(58), ScaleX(205), ScaleY(115));
  LayoutPreview := TBitmapImage.Create(LayoutPage);
  LayoutPreview.Parent := LayoutPage.Surface;
  PreviewSize := ScaleX(220);
  if PreviewSize > LayoutPage.SurfaceHeight - ScaleY(34) then
    PreviewSize := LayoutPage.SurfaceHeight - ScaleY(34);
  LayoutPreview.SetBounds(LayoutPage.SurfaceWidth - PreviewSize, 0, PreviewSize, PreviewSize);
  LayoutPreview.Stretch := True;
  PreviewButton := TNewButton.Create(LayoutPage);
  PreviewButton.Parent := LayoutPage.Surface;
  PreviewButton.SetBounds(LayoutPage.SurfaceWidth - PreviewSize, PreviewSize + ScaleY(6), PreviewSize, ScaleY(24));
  PreviewButton.OnClick := @TogglePreview;
  Note := TNewStaticText.Create(LayoutPage);
  Note.Parent := LayoutPage.Surface;
  Note.AutoSize := False;
  Note.WordWrap := True;
  Note.SetBounds(0, ScaleY(190), ScaleX(205), ScaleY(45));
  Note.Caption := 'Demo met testwaarden.' + #13#10 + 'Pauzeren kan rechts.' + #13#10 + 'Zonder scherm of internet.';
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
