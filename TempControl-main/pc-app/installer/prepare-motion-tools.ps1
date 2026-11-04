$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot 'tool-cache'
$archive=Join-Path $cache 'ffmpeg-lgpl.zip'
$expected=@('3f91f0581df7cbe62bab08d670a41f11bc80e064143b09376dea05ffcc9d09fc','890397244b378ff565442b30db907e3ab4880728ff0fd5adfa4cd43a9852dc8d')
if(!(Test-Path -LiteralPath $archive)){
    New-Item -ItemType Directory -Path $cache -Force | Out-Null
    Invoke-WebRequest 'https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-11-03-12-50/ffmpeg-N-128352-gec887b13a8-win64-lgpl.zip' -OutFile $archive
}
if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -notin $expected){throw 'FFmpeg archive differs from the reviewed build. Review and pin a new digest before updating.'}
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $cache 'ffmpeg') -Force
$pinned=Join-Path $cache 'ffmpeg/ffmpeg-N-128352-gec887b13a8-win64-lgpl'
if(Test-Path -LiteralPath $pinned){
    $normalized=Join-Path $cache 'ffmpeg/ffmpeg-master-latest-win64-lgpl'
    New-Item -ItemType Directory -Path (Join-Path $normalized 'bin') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $pinned 'bin/ffmpeg.exe') -Destination (Join-Path $normalized 'bin/ffmpeg.exe') -Force
    Copy-Item -LiteralPath (Join-Path $pinned 'LICENSE.txt') -Destination (Join-Path $normalized 'LICENSE.txt') -Force
}
Write-Output 'Verified FFmpeg LGPL build is ready for Qscreen publish.'
