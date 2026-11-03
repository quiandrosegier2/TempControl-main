$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'TempMonitor.iss') -Raw
$count=[int][regex]::Match($source,'#define PreviewFrameCount (\d+)').Groups[1].Value
$layouts=@([regex]::Matches($source,"AddLayout\('([^']+)'")|ForEach-Object {$_.Groups[1].Value}|Select-Object -Skip 3 -First 23)
$output=Join-Path $PSScriptRoot 'app-previews'
New-Item -ItemType Directory -Path $output -Force|Out-Null
for($frame=0;$frame -lt $count;$frame++){
    & (Join-Path $PSScriptRoot 'build-layout-previews.ps1') -OutputDirectory $output -Layouts $layouts -FrameTime ($frame/10.0) -FrameIndex $frame -RenderSize 412 -FrameFormat Png
    if($frame%10 -eq 0){Write-Host "Scherpe app-voorbeelden: $frame/$count"}
}
