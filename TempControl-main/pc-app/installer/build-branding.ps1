# Bestaand QSCREEN //-beeldmerk, gerenderd voor Setup en het firmwarevenster.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot 'branding'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$background = [Drawing.Color]::FromArgb(32,32,32)
$red = [Drawing.Color]::FromArgb(230,35,40)
function Surface([int]$width,[int]$height) {
    $bitmap = New-Object Drawing.Bitmap($width,$height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.TextRenderingHint = 'AntiAliasGridFit'
    return @($bitmap,$graphics)
}
function CenterText($graphics,[string]$text,[float]$x,[float]$y,[float]$size,[Drawing.Color]$color) {
    $font = New-Object Drawing.Font('Segoe UI',$size,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object Drawing.SolidBrush($color)
    $format = New-Object Drawing.StringFormat
    $format.Alignment='Center'; $format.LineAlignment='Center'
    $graphics.DrawString($text,$font,$brush,$x,$y,$format)
    $format.Dispose(); $brush.Dispose(); $font.Dispose()
}
$wordmark,$g = Surface 640 144
$g.Clear([Drawing.Color]::Transparent)
$font = New-Object Drawing.Font('Segoe UI',98,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$whiteBrush = New-Object Drawing.SolidBrush([Drawing.Color]::White)
$redBrush = New-Object Drawing.SolidBrush($red)
$brandWidth=$g.MeasureString('QSCREEN',$font).Width
$slashWidth=$g.MeasureString('//',$font).Width
$left=(640-$brandWidth-$slashWidth)/2
$g.DrawString('QSCREEN',$font,$whiteBrush,[float]$left,[float]7)
$g.DrawString('//',$font,$redBrush,[float]($left+$brandWidth-6),[float]7)
$wordmark.Save((Join-Path $output 'qprints-wordmark.png'),[Drawing.Imaging.ImageFormat]::Png)
$footer,$footerG = Surface 640 144
$footerG.Clear($background); $footerG.DrawImageUnscaled($wordmark,0,0)
$footer.Save((Join-Path $output 'qprints-footer.bmp'),[Drawing.Imaging.ImageFormat]::Bmp)
$footerG.Dispose(); $footer.Dispose()
$g.Dispose(); $font.Dispose(); $whiteBrush.Dispose(); $redBrush.Dispose()

$iconFrames = @()
foreach($size in @(16,32,48,64,128,256)) {
    $bitmap,$g = Surface $size $size
    $g.Clear([Drawing.Color]::FromArgb(23,24,30))
    CenterText $g 'Q' ($size*.43) ($size*.46) ($size*.72) ([Drawing.Color]::White)
    CenterText $g '//' ($size*.73) ($size*.73) ($size*.39) $red
    if($size -eq 256) { $bitmap.Save((Join-Path $output 'qprints-mark.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $iconFrames += ,@($size,$stream.ToArray())
    $stream.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$iconStream = [IO.File]::Create((Join-Path $output 'qprints.ico'))
$writer = New-Object IO.BinaryWriter($iconStream)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$iconFrames.Count)
$offset=6+16*$iconFrames.Count
foreach($frame in $iconFrames) {
    $dimension=if($frame[0] -eq 256){0}else{$frame[0]}
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([int]$frame[1].Length); $writer.Write([int]$offset)
    $offset += $frame[1].Length
}
foreach($frame in $iconFrames){$writer.Write([byte[]]$frame[1])}
$writer.Dispose(); $iconStream.Dispose()

$banner,$g = Surface 328 628
$brush = New-Object Drawing.Drawing2D.LinearGradientBrush([Drawing.Point]::new(0,0),[Drawing.Point]::new(328,628),[Drawing.Color]::FromArgb(17,18,24),[Drawing.Color]::FromArgb(38,23,31))
$g.FillRectangle($brush,0,0,328,628); $brush.Dispose()
$g.DrawImage($wordmark,14,38,300,68)
$pen = New-Object Drawing.Pen([Drawing.Color]::FromArgb(52,55,66),2)
$g.DrawEllipse($pen,60,202,208,208); $pen.Dispose()
$pen = New-Object Drawing.Pen($red,5)
$g.DrawArc($pen,60,202,208,208,220,65); $pen.Dispose()
CenterText $g 'Q' 151 296 133 ([Drawing.Color]::White)
CenterText $g '//' 216 338 64 $red
CenterText $g 'ESP DISPLAY' 164 455 27 ([Drawing.Color]::White)
CenterText $g 'CPU  /  GPU  /  FPS' 164 493 18 ([Drawing.Color]::FromArgb(160,163,177))
CenterText $g 'Qscreen' 164 578 20 ([Drawing.Color]::FromArgb(204,206,215))
$banner.Save((Join-Path $output 'qprints-banner.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $banner.Dispose(); $wordmark.Dispose()
