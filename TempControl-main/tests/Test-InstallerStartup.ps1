# Test een aparte installer die na InitializeWizard direct afsluit, voor installatie.
param([Parameter(Mandatory)][string]$InnoCompiler)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot
$installer = Join-Path $project 'pc-app\installer'
$cache = Join-Path $installer 'tool-cache\startup-tests'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$source = Get-Content (Join-Path $installer 'TempMonitor.iss') -Raw
$source = $source.Replace('PrivilegesRequired=admin', 'PrivilegesRequired=lowest')
$source = $source.Replace('DefaultDirName={autopf}\{#AppName}', 'DefaultDirName=' + $cache + '\fresh')
$source = $source.Replace('DisableDirPage=yes', "DisableDirPage=yes`r`nUsePreviousAppDir=no")
$source = $source.Replace('[Code]', "[Code]`r`nprocedure ExitProcess(Code: Cardinal); external 'ExitProcess@kernel32.dll stdcall';")
$marker = '  LayoutChanged(nil);'
if (-not $source.Contains($marker)) { throw 'InitializeWizard-testpunt ontbreekt' }
$assertions = @'
  LayoutChanged(nil);
  if SelectedLayout <> ExpandConstant('{param:EXPECTLAYOUT|classic}') then
  begin
    Log('LAYOUT_SMOKE_FAIL: ' + SelectedLayout);
    ExitProcess(91);
  end;
  if (LayoutPreview.Bitmap.Width = 0) or (LayoutPreview.Bitmap.Height = 0) then
    ExitProcess(92);
  LayoutChoice.ItemIndex := 0; LayoutChanged(nil);
  LayoutChoice.ItemIndex := 1; LayoutChanged(nil);
  LayoutChoice.ItemIndex := 2; LayoutChanged(nil);
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
        @{ Name='classic'; Directory=$fresh; Layout='classic'; Expected='classic' },
        @{ Name='gauges'; Directory=$fresh; Layout='gauges'; Expected='gauges' },
        @{ Name='minimal'; Directory=$fresh; Layout='minimal'; Expected='minimal' },
        @{ Name='upgrade'; Directory=$upgrade; Layout=''; Expected='minimal' },
        @{ Name='override'; Directory=$upgrade; Layout='gauges'; Expected='gauges' }
    )
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
