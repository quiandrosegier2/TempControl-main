$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'TempMonitor.iss') -Raw
$frameCount=[int][regex]::Match($source,'#define PreviewFrameCount (\d+)').Groups[1].Value
if($frameCount -lt 2){throw 'PreviewFrameCount ontbreekt'}
$layouts=@([regex]::Matches($source,"AddLayout\('([^']+)'")|ForEach-Object {$_.Groups[1].Value}|Select-Object -Skip 3 -First 23)
$output=Join-Path $PSScriptRoot 'preview-animation'
New-Item -ItemType Directory -Path $output -Force|Out-Null
Get-ChildItem -LiteralPath $output -Filter '*.bmp' -File|ForEach-Object {Remove-Item -LiteralPath $_.FullName}
for($frame=0;$frame -lt $frameCount;$frame++){
    & (Join-Path $PSScriptRoot 'build-layout-previews.ps1') -OutputDirectory $output -Layouts $layouts -FrameTime ($frame/10.0) -FrameIndex $frame -RenderSize 220
    if($frame%10 -eq 0){Write-Host "Animatievoorbeelden: $frame/$frameCount"}
}
Write-Host "Animatievoorbeelden klaar: $($layouts.Count) layouts, $frameCount beelden per layout."
