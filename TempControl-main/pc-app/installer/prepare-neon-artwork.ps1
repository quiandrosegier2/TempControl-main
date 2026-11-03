param([Parameter(Mandatory=$true)][string]$Manifest,[string]$AssetDirectory=(Join-Path $PSScriptRoot '..\assets\neon50'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assetRoot=[IO.Path]::GetFullPath($AssetDirectory)
New-Item -ItemType Directory -Force -Path $assetRoot | Out-Null
foreach($entry in (Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json)){
    if($entry.slug -notmatch '^[a-z0-9-]+$'){throw 'Invalid asset name'}
    $destination=Join-Path $assetRoot ($entry.slug+'.png')
    $source=[Drawing.Image]::FromFile($entry.source)
    $bitmap=New-Object Drawing.Bitmap 412,412
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    try{
        $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source,[Drawing.Rectangle]::new(0,0,412,412))
        $bitmap.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
        Write-Output ('Prepared: '+$entry.slug+' / 412x412 / '+(Get-Item -LiteralPath $destination).Length+' bytes')
    }finally{$graphics.Dispose();$bitmap.Dispose();$source.Dispose()}
}
