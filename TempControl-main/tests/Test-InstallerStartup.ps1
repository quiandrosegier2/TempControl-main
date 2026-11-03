# Test een aparte installer die na InitializeWizard direct afsluit, voor installatie.
param([Parameter(Mandatory)][string]$InnoCompiler)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot
$installer = Join-Path $project 'pc-app\installer'
$cache = Join-Path $installer 'tool-cache\startup-tests'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$source = Get-Content (Join-Path $installer 'TempMonitor.iss') -Raw
$appManifest = [xml](Get-Content (Join-Path $project 'pc-app\app.manifest') -Raw)
$executionLevel = $appManifest.SelectSingleNode("//*[local-name()='requestedExecutionLevel']").GetAttribute('level')
$appLaunch = [regex]::Match($source, '(?m)^Filename: "\{app\}\\Qscreen\.exe";.*$').Value
if ($executionLevel -eq 'requireAdministrator' -and $appLaunch -notmatch '\bFlags:[^\r\n]*\brunascurrentuser\b') {
    throw 'Qscreen requires elevation: the postinstall launch must inherit Setup privileges to avoid error 740.'
}
Write-Output 'PASS: postinstall privileges match the Qscreen administrator manifest.'
$source = $source.Replace('PrivilegesRequired=admin', 'PrivilegesRequired=lowest')
$source = $source.Replace('DefaultDirName={autopf}\TempMonitor', 'DefaultDirName=' + $cache + '\fresh')
$source = $source.Replace('DisableDirPage=yes', "DisableDirPage=yes`r`nUsePreviousAppDir=no")
$nativeTest = @'
[Code]
type
  TPreviewTestMessage = record
    WindowHandle, MessageId, WParam, LParam, Time: LongWord;
    PointX, PointY: Integer;
    PrivateData: LongWord;
  end;
procedure ExitProcess(Code: Cardinal); external 'ExitProcess@kernel32.dll stdcall';
function PeekMessage(var Msg: TPreviewTestMessage; Window: HWND; Min, Max, Remove: LongWord): Integer;
  external 'PeekMessageW@user32.dll stdcall';
function DispatchMessage(var Msg: TPreviewTestMessage): Integer;
  external 'DispatchMessageW@user32.dll stdcall';
'@
$source = $source.Replace('[Code]', $nativeTest)
$source = $source.Replace('Index, PreviewSize: Integer;', "Index, PreviewSize, Pump, BeforeTicks: Integer;`r`n  TimerMessage: TPreviewTestMessage;")
$marker = '  LayoutChanged(nil);'
if (-not $source.Contains($marker)) { throw 'InitializeWizard-testpunt ontbreekt' }
$assertions = @'
  LayoutChanged(nil);
  if not ShouldSkipPage(LayoutPage.ID) then ExitProcess(101);
  if ShouldSkipPage(wpWelcome) or ShouldSkipPage(wpReady) then ExitProcess(102);
  if not HighContrastActive and not IsDarkInstallMode then ExitProcess(94);
  if (BrandLogo.Bitmap.Width = 0) or not BrandLogo.Visible then ExitProcess(95);
  if SelectedLayout <> ExpandConstant('{param:EXPECTLAYOUT|classic}') then
  begin
    Log('LAYOUT_SMOKE_FAIL: ' + SelectedLayout);
    ExitProcess(91);
  end;
  if (LayoutPreview.Bitmap.Width = 0) or (LayoutPreview.Bitmap.Height = 0) then
    ExitProcess(92);
  if LayoutChoice.Items.Count <> 31 then ExitProcess(93);
  for Index := 0 to LayoutChoice.Items.Count - 1 do
  begin
    LayoutChoice.ItemIndex := Index; LayoutChanged(nil);
    if (LayoutPreview.Bitmap.Width = 0) or (LayoutPreview.Bitmap.Height = 0) then ExitProcess(92);
    if (Index < 3) or (Index >= 26) then
    begin
      if PreviewButton.Enabled or (PreviewTimer <> 0) then ExitProcess(96);
    end
    else
    begin
      CurPageChanged(LayoutPage.ID);
      if PreviewTimer = 0 then ExitProcess(96);
      if Index = 3 then
      begin
        BeforeTicks := PreviewTickCount;
        Sleep(150);
        for Pump := 0 to 7 do
        begin
          if PeekMessage(TimerMessage, 0, 275, 275, 1) = 0 then Break;
          DispatchMessage(TimerMessage);
        end;
        if PreviewTickCount <= BeforeTicks then ExitProcess(100);
        LayoutChanged(nil);
      end;
      PreviewTick(0, 275, PreviewTimer, 0);
      if PreviewFrame <> 1 then ExitProcess(97);
      TogglePreview(nil);
      if not PreviewPaused or (PreviewTimer <> 0) then ExitProcess(98);
      PreviewTick(0, 275, 0, 0);
      if PreviewFrame <> 1 then ExitProcess(98);
      TogglePreview(nil);
      if PreviewPaused or (PreviewTimer = 0) then ExitProcess(99);
      PreviewTick(0, 275, PreviewTimer, 0);
      if PreviewFrame <> 2 then ExitProcess(99);
      CurPageChanged(wpReady);
      if PreviewTimer <> 0 then ExitProcess(99);
    end;
  end;
  Log('LAYOUT_SMOKE_PASS');
  ExitProcess(0);
'@
$source = $source.Replace($marker, $assertions)
# Bronpaden blijven relatief aan de installer-map; de tijdelijke testbron wordt niet gecommit.
$testSource = Join-Path $installer 'startup-smoke.iss'
try {
    Set-Content -LiteralPath $testSource -Value $source -Encoding UTF8
    & $InnoCompiler '/Q' "/O$cache" '/Fstartup-smoke' $testSource
    if ($LASTEXITCODE) { throw 'Opstarttest compileren mislukt' }
    $fresh = Join-Path $cache 'fresh'
    $upgrade = Join-Path $cache 'upgrade'
    New-Item -ItemType Directory -Path $fresh,$upgrade -Force | Out-Null
    Set-Content (Join-Path $upgrade 'layout.txt') -Value 'minimal' -Encoding ASCII
    $cases = @(
        @{ Name='fresh'; Directory=$fresh; Layout=''; Expected='classic' },
        @{ Name='upgrade'; Directory=$upgrade; Layout=''; Expected='minimal' },
        @{ Name='override'; Directory=$upgrade; Layout='gauges'; Expected='gauges' }
    )
    $ids = [regex]::Matches($source, "AddLayout\('([^']+)'") | ForEach-Object { $_.Groups[1].Value }
    if ($ids.Count -ne 31) { throw 'Verwacht eenentwintig layouts in de installer' }
    foreach ($id in $ids) { $cases += @{ Name=$id; Directory=$fresh; Layout=$id; Expected=$id } }
    foreach ($case in $cases) {
        $log = Join-Path $cache ($case.Name + '.log')
        $arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/SP-', ('/LOG="' + $log + '"'),
            ('/DIR="' + $case.Directory + '"'), ('/EXPECTLAYOUT=' + $case.Expected))
        if ($case.Layout) { $arguments += '/LAYOUT=' + $case.Layout }
        $process = Start-Process (Join-Path $cache 'startup-smoke.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(20000)) { $process.Kill(); throw "Opstarttest vastgelopen: $($case.Name)" }
        if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern 'LAYOUT_SMOKE_PASS' -Quiet)) {
            throw "Opstarttest mislukt: $($case.Name). Zie $log"
        }
        "PASS: $($case.Name)"
        $process.Dispose()
    }
} finally {
    Remove-Item -LiteralPath $testSource -ErrorAction SilentlyContinue
}
