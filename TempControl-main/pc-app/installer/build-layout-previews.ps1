# Schematische voorbeelden in dezelfde 412x412-coordinaten als de firmware.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = Join-Path $PSScriptRoot 'layout-previews'
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($layout in @('classic', 'gauges', 'minimal', 'orbit', 'radar', 'neon-pulse', 'reactor', 'waveform', 'matrix', 'starfield', 'speedometer', 'aurora', 'hex-grid')) {
    $bitmap = New-Object Drawing.Bitmap(412, 412)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([Drawing.Color]::FromArgb(32, 32, 32))
    $g.FillEllipse([Drawing.Brushes]::Black, 0, 0, 411, 411)
    $clip = New-Object Drawing.Drawing2D.GraphicsPath
    $clip.AddEllipse(0,0,412,412); $g.SetClip($clip)
    $white = [Drawing.Color]::White
    $grey = [Drawing.Color]::FromArgb(150,150,160)
    $blue = [Drawing.Color]::FromArgb(60,190,255)
    $red = [Drawing.Color]::FromArgb(255,70,60)
    $divider = [Drawing.Color]::FromArgb(55,55,65)
    $mint = [Drawing.Color]::FromArgb(50,235,175)
    $purple = [Drawing.Color]::FromArgb(175,100,255)
    function Text([string]$value, [float]$x, [float]$y, [float]$size, [Drawing.Color]$color) {
        $font = New-Object Drawing.Font('Segoe UI', $size, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object Drawing.SolidBrush($color)
        $format = New-Object Drawing.StringFormat
        $format.Alignment = 'Center'; $format.LineAlignment = 'Center'
        $g.DrawString($value, $font, $brush, $x, $y, $format)
        $format.Dispose(); $brush.Dispose(); $font.Dispose()
    }
    function Dim([Drawing.Color]$color, [float]$brightness) {
        [Drawing.Color]::FromArgb([int]($color.R*$brightness),[int]($color.G*$brightness),[int]($color.B*$brightness))
    }
    function Line([float]$x1,[float]$y1,[float]$x2,[float]$y2,[Drawing.Color]$color,[float]$width=1) {
        $pen = New-Object Drawing.Pen($color,$width)
        $g.DrawLine($pen,$x1,$y1,$x2,$y2); $pen.Dispose()
    }
    function Circle([float]$x,[float]$y,[float]$radius,[Drawing.Color]$color,[float]$width=1) {
        $pen = New-Object Drawing.Pen($color,$width)
        $g.DrawEllipse($pen,($x-$radius),($y-$radius),(2*$radius),(2*$radius)); $pen.Dispose()
    }
    function Dot([float]$x,[float]$y,[float]$radius,[Drawing.Color]$color) {
        $brush = New-Object Drawing.SolidBrush($color)
        $g.FillEllipse($brush,($x-$radius),($y-$radius),(2*$radius),(2*$radius)); $brush.Dispose()
    }
    function Polar([float]$inner,[float]$outer,[float]$degrees,[Drawing.Color]$color) {
        $r = $degrees * [Math]::PI/180
        Line (206+[Math]::Cos($r)*$inner) (206+[Math]::Sin($r)*$inner) (206+[Math]::Cos($r)*$outer) (206+[Math]::Sin($r)*$outer) $color
    }
    function Hex([float]$x,[float]$y,[float]$radius,[Drawing.Color]$color) {
        $points = [Drawing.PointF[]]@(for($j=0;$j -lt 6;$j++) {
            $a=(30+$j*60)*[Math]::PI/180
            [Drawing.PointF]::new([float]($x+[Math]::Cos($a)*$radius),[float]($y+[Math]::Sin($a)*$radius))
        })
        $pen = New-Object Drawing.Pen($color,1)
        $g.DrawPolygon($pen,$points); $pen.Dispose()
    }
    function Fps([int]$y=201) { Text '165' 206 $y 56 $white; Text 'FPS' 206 ($y+49) 22 $grey }
    function Temperatures([int]$y=325) {
        $g.FillRectangle([Drawing.Brushes]::Black,51,($y-16),148,32)
        $g.FillRectangle([Drawing.Brushes]::Black,213,($y-16),148,32)
        Text 'CPU 42 C' 125 $y 22 $blue; Text 'GPU 58 C' 287 $y 22 $red
    }
    function Rows([bool]$pulse=$false) {
        $labels=@('CPU','GPU','FPS'); $values=@('42','58','165'); $colors=@($blue,$red,$purple)
        for($i=0;$i -lt 3;$i++) {
            $y=153+$i*80
            $g.FillRectangle([Drawing.Brushes]::Black,73,($y-32),266,65)
            $pen=New-Object Drawing.Pen((Dim $colors[$i] .6),1)
            $g.DrawRectangle($pen,73,($y-32),266,65); $pen.Dispose()
            Text $labels[$i] 119 $y 22 $grey; Text $values[$i] 260 $y 43 $colors[$i]
            if($i -lt 2) { Text 'C' 323 $y 22 $colors[$i] }
            if($pulse) { Line 113 ($y+25) (190+$i*35) ($y+25) $colors[$i] 3 }
        }
    }
    if ($layout -notin @('classic','gauges','minimal')) {
        $phase=1.6
        switch($layout) {
            'orbit' {
                for($i=0;$i -lt 3;$i++) {
                    $color=if($i -eq 1){$purple}else{$blue}
                    $pen=New-Object Drawing.Pen((Dim $color .24),1)
                    $g.DrawEllipse($pen,(67-$i*13),(133-$i*20),(278+$i*26),(148+$i*40)); $pen.Dispose()
                    $a=$phase*(.7+$i*.22)+$i*2
                    Dot (206+[Math]::Cos($a)*(139+$i*13)) (207+[Math]::Sin($a)*(74+$i*20)) 4 $color
                }
                Fps; Temperatures 347
            }
            'radar' {
                foreach($radius in @(47,94,141,188)){Circle 206 206 $radius (Dim $mint .16)}
                Line 18 206 394 206 (Dim $mint .12); Line 206 18 206 394 (Dim $mint .12)
                for($tail=0;$tail -lt 45;$tail+=3){Polar 30 187 ($phase*45-$tail) (Dim $mint (.5*(45-$tail)/45))}
                Dot 103 126 3 $blue; Dot 304 278 3 $red; Rows
            }
            'neon-pulse' { for($r=196;$r -lt 202;$r++){Circle 206 206 $r (Dim $purple .26)}; Rows $true }
            'reactor' {
                for($ring=0;$ring -lt 3;$ring++){for($segment=0;$segment -lt 48;$segment++){
                    $speed=if($ring%2){-16}else{22}; $color=if($ring -eq 1){$purple}else{$mint}
                    Polar (152+$ring*15) (161+$ring*15) ($segment*7.5+$phase*$speed) (Dim $color .65)
                }}
                Hex 206 206 107 (Dim $mint .45); Fps; Temperatures 341
            }
            'waveform' {
                Fps 132
                foreach($top in @(232,323)) {
                    $color=if($top -eq 232){$blue}else{$red}
                    $label=if($top -eq 232){'CPU 42 C'}else{'GPU 58 C'}
                    Text $label 206 ($top-18) 22 $color
                    for($j=0;$j -le 2;$j++){Line 83 ($top+$j*20) 329 ($top+$j*20) (Dim $divider .6)}
                    for($x=84;$x -le 329;$x+=4){
                        $y1=$top+22+[Math]::Sin(($x-4)*.07+$top)*5
                        $y2=$top+22+[Math]::Sin($x*.07+$top)*5
                        Line ($x-4) $y1 $x $y2 $color
                    }
                    Line 147 $top 147 ($top+40) (Dim $color .3)
                }
            }
            'matrix' {
                for($column=0;$column -lt 13;$column++) {
                    $x=26+$column*30; $head=($phase*(32+$column%4*7)+$column*51)%500-40
                    for($tail=0;$tail -lt 7;$tail++) {
                        $y=$head-$tail*24
                        if($y -gt 20 -and $y -lt 392 -and [Math]::Pow($x-206,2)+[Math]::Pow($y-206,2) -lt 188*188){
                            Text ([string](($column*7+$tail*3)%10)) $x $y 22 ([Drawing.Color]::FromArgb(0,(120-$tail*13),55))
                        }
                    }
                }; Rows
            }
            'starfield' {
                for($star=0;$star -lt 48;$star++) {
                    $angle=$star*2.399963; $depth=($phase*(.17+$star%5*.02)+$star*.037)%1
                    $r=24+$depth*174; $color=if($star%3){$white}else{$blue}
                    Dot (206+[Math]::Cos($angle)*$r) (206+[Math]::Sin($angle)*$r) (1+[int]($depth -gt .75)) (Dim $color (.15+$depth*.65))
                }
                $g.FillRectangle([Drawing.Brushes]::Black,107,145,198,115); Fps; Temperatures 335
            }
            'speedometer' {
                function DialLine([float]$x,[float]$y,[float]$inner,[float]$outer,[float]$degrees,[Drawing.Color]$color) {
                    $a=$degrees*[Math]::PI/180
                    Line ($x+[Math]::Cos($a)*$inner) ($y+[Math]::Sin($a)*$inner) ($x+[Math]::Cos($a)*$outer) ($y+[Math]::Sin($a)*$outer) $color
                }
                function SpeedDial([string]$label,[float]$value,[float]$maximum,[int]$x,[int]$y,[int]$radius,[bool]$temperature) {
                    $needleColor=if($temperature){if($value -gt 50){$red}else{$blue}}else{$purple}
                    for($a=135;$a -le 405;$a+=3) {
                        $scaleValue=($a-135)*$maximum/270
                        $color=if($temperature){if($scaleValue -gt 50){$red}else{$blue}}else{if($scaleValue -gt $maximum*.75){$purple}else{$blue}}
                        DialLine $x $y ($radius-5) $radius $a (Dim $color .65)
                    }
                    for($tick=0;$tick -le 8;$tick++){DialLine $x $y ($radius-12) $radius (135+$tick*33.75) (Dim $grey .85)}
                    if(-not $temperature){for($tail=0;$tail -lt 18;$tail+=3){
                        DialLine $x $y ($radius+3) ($radius+7) (135+($phase*35-$tail+270)%270) (Dim $purple (.85*(18-$tail)/18))
                    }}
                    DialLine $x $y -6 ($radius-22) (135+[Math]::Min([Math]::Max($value,0),$maximum)*270/$maximum) $needleColor
                    Dot $x $y $(if($temperature){3}else{4}) $white
                    Text $label $x ($y-$(if($temperature){28}else{35})) 22 $grey
                    if($temperature){Text "$value C" $x ($y+28) 22 $needleColor}else{Text "$value" $x ($y+46) 43 $white}
                    Text '0' ($x-$radius*.8) ($y+$radius*.7) 9 $grey
                    Text "$maximum" ($x+$radius*.8) ($y+$radius*.7) 9 $grey
                }
                SpeedDial 'FPS' 165 240 206 164 88 $false
                SpeedDial 'CPU' 42 120 122 305 65 $true
                SpeedDial 'GPU' 58 120 290 305 65 $true
            }
            'aurora' {
                for($wave=0;$wave -lt 4;$wave++){for($x=18;$x -lt 394;$x+=4){
                    $base=155+$wave*38+[Math]::Sin($x*.017+$phase*.55+$wave)*38
                    $color=if($wave%2){$purple}else{$mint}
                    for($tail=0;$tail -lt 5;$tail++){
                        $brush=New-Object Drawing.SolidBrush((Dim $color (.13*(5-$tail)/5)))
                        $g.FillRectangle($brush,$x,[float]($base+$tail*5),4,5); $brush.Dispose()
                    }
                }}
                $g.FillRectangle([Drawing.Brushes]::Black,114,143,184,120); Fps; Temperatures 343
            }
            'hex-grid' {
                for($row=0;$row -lt 8;$row++){for($column=0;$column -lt 8;$column++){
                    $x=20+$column*54+($row%2)*27; $y=26+$row*47
                    if([Math]::Pow($x-206,2)+[Math]::Pow($y-206,2) -lt 172*172){Hex $x $y 29 (Dim $blue .22)}
                }}
                $xs=@(126,286,206);$ys=@(180,180,304);$labels=@('CPU','GPU','FPS');$vals=@('42','58','165');$colors=@($blue,$red,$purple)
                for($i=0;$i -lt 3;$i++){
                    $x=$xs[$i];$y=$ys[$i]
                    $g.FillRectangle([Drawing.Brushes]::Black,($x-57),($y-53),114,107)
                    Hex $x $y 68 (Dim $colors[$i] .6)
                    Text $labels[$i] $x ($y-29) 22 $grey; Text $vals[$i] $x ($y+6) 43 $colors[$i]
                    if($i -lt 2){Text 'C' $x ($y+42) 22 $colors[$i]}
                }
            }
        }
        if($layout -eq 'speedometer') {
            Text 'QPRINTS' 206 40 22 $white
        } else {
            if($layout -eq 'matrix') { $g.FillRectangle([Drawing.Brushes]::Black,76,31,260,74) }
            Text 'QPRINTS' 206 55 22 $white
            $title=if($layout -eq 'waveform'){'THERMAL HISTORY'}else{$layout.Replace('-',' ').ToUpperInvariant()}
            Text $title 206 88 22 $grey
        }
    } elseif ($layout -eq 'classic') {
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
    $g.Dispose(); $clip.Dispose(); $bitmap.Dispose()
}
