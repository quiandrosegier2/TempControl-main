# Bouwt dist\Qprints esp installer-<versie>.exe
#   powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
#   powershell -ExecutionPolicy Bypass -File .\build-installer.ps1 -OutputDir E:\   (bv. rechtstreeks naar een USB-stick)
# Vereist: pioarduino 6.2.0, .NET 10 SDK en Inno Setup 6.6 of nieuwer binnen 6.x.

param([string]$OutputDir, [string]$PlatformIO, [string]$InnoCompiler)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = $PSScriptRoot
if (-not $OutputDir) { $OutputDir = Join-Path (Split-Path $root) 'dist' }
$installerDir = Join-Path $root 'installer'
$publishDir = Join-Path $installerDir 'publish'
$presentMonVersion = '2.6.0'
$presentMonCache = Join-Path $installerDir "PresentMon-$presentMonVersion-x64.exe"

$version = ([xml](Get-Content (Join-Path $root 'TempMonitor.csproj'))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

# 1. Firmware bouwen. Alleen de bouwer heeft PlatformIO nodig.
$projectDir = Split-Path $root
$firmwareDir = Join-Path $projectDir 'firmware'
if (-not $PlatformIO) {
    $localPio = Join-Path $projectDir '.platformio\penv\Scripts\pio.exe'
    if (Test-Path $localPio) {
        $PlatformIO = $localPio
        if (-not $env:PLATFORMIO_CORE_DIR) { $env:PLATFORMIO_CORE_DIR = Join-Path $projectDir '.platformio' }
    } else {
        $PlatformIO = (Get-Command pio -ErrorAction Stop).Source
    }
}
& $PlatformIO run --project-dir $firmwareDir -e waveshare_s3_touch_lcd_146
if ($LASTEXITCODE) { throw 'Firmware bouwen mislukt' }
$factoryImage = Join-Path $firmwareDir '.pio\build\waveshare_s3_touch_lcd_146\firmware.factory.bin'
if (-not (Test-Path $factoryImage)) { throw "Factory-firmware ontbreekt: $factoryImage" }

# 2. App publiceren (self-contained: .NET hoeft niet op de doel-pc te staan).
# Oude build-uitvoer verwijderen binnen de gecontroleerde publish-map.
if (Test-Path $publishDir) {
    $resolvedPublish = [IO.Path]::GetFullPath($publishDir)
    if ($resolvedPublish -ne [IO.Path]::GetFullPath((Join-Path $root 'installer\publish'))) { throw 'Ongeldige publish-map' }
    Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
}
dotnet publish (Join-Path $root 'TempMonitor.csproj') -c Release -r win-x64 --self-contained true `
    -p:OutputType=WinExe -p:DebugType=none -o $publishDir
if ($LASTEXITCODE) { throw 'dotnet publish mislukt' }

# 3. PresentMon (FPS-meting) ophalen
if (-not (Test-Path $presentMonCache)) {
    $url = "https://github.com/GameTechDev/PresentMon/releases/download/v$presentMonVersion/PresentMon-$presentMonVersion-x64.exe"
    Write-Host "PresentMon downloaden: $url"
    Invoke-WebRequest $url -OutFile $presentMonCache
}
Copy-Item $presentMonCache (Join-Path $publishDir 'PresentMon.exe')

# 4. Officieel standalone esptool bundelen: geen Python nodig op de doel-pc.
$esptoolVersion = '5.1.0'
$assetName = "esptool-v$esptoolVersion-windows-amd64.zip"
$toolCache = Join-Path $installerDir 'tool-cache'
New-Item -ItemType Directory -Path $toolCache -Force | Out-Null
$archive = Join-Path $toolCache $assetName
$release = Invoke-RestMethod "https://api.github.com/repos/espressif/esptool/releases/tags/v$esptoolVersion"
$asset = $release.assets | Where-Object name -eq $assetName | Select-Object -First 1
if (-not $asset -or $asset.digest -notmatch '^sha256:([a-fA-F0-9]{64})$') { throw 'Geen SHA256-controle beschikbaar voor esptool' }
$expectedHash = $Matches[1]
if (-not (Test-Path $archive)) { Invoke-WebRequest $asset.browser_download_url -OutFile $archive }
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $expectedHash) { throw "esptool-controle mislukt. Verwijder $archive en bouw opnieuw." }
$expanded = Join-Path $toolCache "esptool-$esptoolVersion"
Expand-Archive -LiteralPath $archive -DestinationPath $expanded -Force
$esptool = Get-ChildItem $expanded -Filter esptool.exe -Recurse | Select-Object -First 1
if (-not $esptool) { throw 'esptool.exe ontbreekt in het release-archief' }
$firmwareOutput = Join-Path $publishDir 'firmware'
New-Item -ItemType Directory -Path $firmwareOutput -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $firmwareOutput 'esptool') -Force | Out-Null
Copy-Item $factoryImage (Join-Path $firmwareOutput 'TempControl.factory.bin')
Copy-Item $esptool.FullName (Join-Path $firmwareOutput 'esptool\esptool.exe')
Copy-Item (Join-Path $esptool.DirectoryName 'LICENSE') (Join-Path $firmwareOutput 'esptool\LICENSE')

# PawnIO meeleveren, zodat ook de driver zonder winget/internet installeert.
$pawnio = Join-Path $toolCache 'PawnIO-2.2.0.exe'
if (-not (Test-Path $pawnio)) {
    Invoke-WebRequest 'https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe' -OutFile $pawnio
}
if ((Get-FileHash $pawnio -Algorithm SHA256).Hash -ne '1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032') {
    throw 'PawnIO SHA256-controle mislukt'
}
New-Item -ItemType Directory -Path (Join-Path $publishDir 'drivers') -Force | Out-Null
Copy-Item $pawnio (Join-Path $publishDir 'drivers\PawnIO_setup.exe')

# 5. Installer compileren
& (Join-Path $installerDir 'build-layout-previews.ps1')
& (Join-Path $installerDir 'build-animated-previews.ps1')
& (Join-Path $installerDir 'build-branding.ps1')
$iscc = $InnoCompiler
if (-not $iscc) { $iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1 }
if (-not $iscc) { throw 'Inno Setup 6 niet gevonden (winget install JRSoftware.InnoSetup)' }

& $iscc "/DAppVersion=$version" "/O$OutputDir" (Join-Path $installerDir 'TempMonitor.iss')
if ($LASTEXITCODE) { throw 'Inno Setup mislukt' }

Write-Host "`nKlaar: $(Join-Path $OutputDir "Qprints esp installer-$version.exe")"
