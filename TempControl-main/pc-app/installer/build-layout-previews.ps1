# Schematische voorbeelden in dezelfde 412x412-coordinaten als de firmware.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot 'layout-previews'
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($layout in @('classic', 'gauges', 'minimal')) {
    $bitmap = New-Object Drawing.Bitmap(412, 412)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([Drawing.Color]::FromArgb(240, 240, 240))
    $g.FillEllipse([Drawing.Brushes]::Black, 0, 0, 411, 411)
    $white = [Drawing.Color]::White
    $grey = [Drawing.Color]::FromArgb(150,150,160)
    $blue = [Drawing.Color]::FromArgb(60,190,255)
    $red = [Drawing.Color]::FromArgb(255,70,60)
    $divider = [Drawing.Color]::FromArgb(55,55,65)
    function Text([string]$value, [float]$x, [float]$y, [float]$size, [Drawing.Color]$color) {
        $font = New-Object Drawing.Font('Segoe UI', $size, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object Drawing.SolidBrush($color)
        $format = New-Object Drawing.StringFormat
        $format.Alignment = 'Center'; $format.LineAlignment = 'Center'
        $g.DrawString($value, $font, $brush, $x, $y, $format)
        $format.Dispose(); $brush.Dispose(); $font.Dispose()
    }
    if ($layout -eq 'classic') {
        Text 'QPRINTS //' 206 90 39 $white
        Text 'SYSTEM MONITOR' 206 127 23 $grey
        $ys = @(176,250,324); $labels = @('CPU','GPU','FPS'); $values = @('42 C','58 C','165')
        for ($i=0; $i -lt 3; $i++) {
            Text $labels[$i] 167 $ys[$i] 39 $white
            $color = if ($i -eq 0) { $blue } elseif ($i -eq 1) { $red } else { $white }
            Text $values[$i] 300 $ys[$i] 43 $color
        }
        $pen = New-Object Drawing.Pen($divider, 1)
        $g.DrawLine($pen,84,213,328,213); $g.DrawLine($pen,84,287,328,287); $pen.Dispose()
    } else {
        Text 'QPRINTS' 206 77 39 $white
        Text 'SYSTEM MONITOR' 206 116 22 $grey
        if ($layout -eq 'gauges') {
            foreach ($x in @(126,286)) {
                $temperature = if ($x -eq 126) { 42 } else { 58 }
                $color = if ($x -eq 126) { $blue } else { $red }
                $pen = New-Object Drawing.Pen($divider,7)
                $g.DrawArc($pen,($x-61),151,122,122,135,270); $pen.Dispose()
                $pen = New-Object Drawing.Pen($color,7)
                $g.DrawArc($pen,($x-61),151,122,122,135,([float]($temperature*2.7))); $pen.Dispose()
                $label = if ($x -eq 126) { 'CPU' } else { 'GPU' }
                Text $label $x 186 22 $grey
                Text "$temperature" $x 225 43 $color
                Text 'C' $x 267 22 $color
            }
            Text '165' 206 326 43 $white; Text 'FPS' 206 367 22 $grey
        } else {
            Text '165' 206 198 112 $white; Text 'FPS' 206 262 22 $grey
            $pen = New-Object Drawing.Pen($divider,1)
            $g.DrawLine($pen,83,287,329,287); $pen.Dispose()
            Text 'CPU' 133 313 22 $grey; Text 'GPU' 279 313 22 $grey
            Text '42 C' 133 347 22 $blue; Text '58 C' 279 347 22 $red
        }
    }
    $bitmap.Save((Join-Path $output "layout-$layout.bmp"), [Drawing.Imaging.ImageFormat]::Bmp)
    $bitmap.Save((Join-Path $output "layout-$layout.png"), [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bitmap.Dispose()
}
