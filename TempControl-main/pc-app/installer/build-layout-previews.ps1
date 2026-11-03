# Schematische voorbeelden in dezelfde 412x412-coordinaten als de firmware.
param(
    [string]$OutputDirectory,
    [string[]]$Layouts = @('classic', 'gauges', 'minimal', 'orbit', 'radar', 'neon-pulse', 'reactor', 'waveform', 'matrix', 'starfield', 'speedometer', 'aurora', 'hex-grid', 'arsenal', 'bullet-storm', 'inferno', 'warzone-city', 'sniper-scope', 'cyber-ops', 'plasma-core', 'boss-fight', 'drone-swarm', 'lightning-strike', 'space-fleet', 'race-grid', 'portal-rift', 'tactical-steel', 'ammo-vault', 'samurai-crest', 'retro-arcade', 'stealth-map'),
    [float]$FrameTime = 1.6,
    [int]$FrameIndex = -1,
    [int]$RenderSize = 412,
    [ValidateSet('Bmp','Png')][string]$FrameFormat = 'Bmp'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$output = if($OutputDirectory){$OutputDirectory}else{Join-Path $PSScriptRoot 'layout-previews'}
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($layout in $Layouts) {
    $phase = $FrameTime
    $demoFps = if($FrameIndex -ge 0){[int][Math]::Round(165+30*[Math]::Sin($phase*.8))}else{165}
    $demoCpu = if($FrameIndex -ge 0){[int][Math]::Round(42+4*[Math]::Sin($phase*.7))}else{42}
    $demoGpu = if($FrameIndex -ge 0){[int][Math]::Round(58+5*[Math]::Sin($phase*.9))}else{58}
    $bitmap = New-Object Drawing.Bitmap($RenderSize, $RenderSize)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.ScaleTransform([float]($RenderSize/412.0),[float]($RenderSize/412.0))
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
    $gold = [Drawing.Color]::FromArgb(245,184,65)
    $orange = [Drawing.Color]::FromArgb(255,105,25)
    $tactical = [Drawing.Color]::FromArgb(120,235,95)
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
    function Fill([float]$x,[float]$y,[float]$w,[float]$h,[Drawing.Color]$color) {
        $brush=[Drawing.SolidBrush]::new($color); $g.FillRectangle($brush,$x,$y,$w,$h); $brush.Dispose()
    }
    function Triangle([float]$x1,[float]$y1,[float]$x2,[float]$y2,[float]$x3,[float]$y3,[Drawing.Color]$color) {
        $points=[Drawing.PointF[]]@([Drawing.PointF]::new($x1,$y1),[Drawing.PointF]::new($x2,$y2),[Drawing.PointF]::new($x3,$y3))
        $brush=[Drawing.SolidBrush]::new($color); $g.FillPolygon($brush,$points); $brush.Dispose()
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
    function Fps([int]$y=201) { Text "$demoFps" 206 $y 56 $white; Text 'FPS' 206 ($y+49) 22 $grey }
    function Temperatures([int]$y=325) {
        $g.FillRectangle([Drawing.Brushes]::Black,51,($y-16),148,32)
        $g.FillRectangle([Drawing.Brushes]::Black,213,($y-16),148,32)
        Text "CPU $demoCpu C" 125 $y 22 $blue; Text "GPU $demoGpu C" 287 $y 22 $red
    }
    function Rows([bool]$pulse=$false) {
        $labels=@('CPU','GPU','FPS'); $values=@("$demoCpu","$demoGpu","$demoFps"); $colors=@($blue,$red,$purple)
        for($i=0;$i -lt 3;$i++) {
            $y=153+$i*80
            $g.FillRectangle([Drawing.Brushes]::Black,73,($y-32),266,65)
            $brightness=if($pulse){.45+.35*(1+[Math]::Sin($phase*2-$i))/2}else{.45}
            $pen=New-Object Drawing.Pen((Dim $colors[$i] $brightness),1)
            $g.DrawRectangle($pen,73,($y-32),266,65); $pen.Dispose()
            Text $labels[$i] 119 $y 22 $grey; Text $values[$i] 260 $y 43 $colors[$i]
            if($i -lt 2) { Text 'C' 323 $y 22 $colors[$i] }
            if($pulse) {
                $factor=if($i -eq 2){.75}else{1.8}
                $width=[Math]::Min(186,[Math]::Max(0,[double]$values[$i]*$factor))
                Line 113 ($y+25) (113+$width) ($y+25) $colors[$i] 3
            }
        }
    }
    function CombatHud([Drawing.Color]$accent) {
        foreach($x in @(69,343)){foreach($y in @(116,318)){
            $left=if($x -eq 343){$x-18}else{$x}; $top=if($y -eq 318){$y-18}else{$y}
            Line $left $y ($left+18) $y (Dim $accent .6); Line $x $top $x ($top+18) (Dim $accent .6)
        }}
        Fill 95 29 222 72 ([Drawing.Color]::Black)
        Fill 124 123 164 112 ([Drawing.Color]::Black)
        Fps 166; Temperatures 346
        Line 74 363 184 363 $divider 3; Line 228 363 338 363 $divider 3
        Line 74 363 (74+$demoCpu*110/120) 363 $blue 3; Line 228 363 (228+$demoGpu*110/120) 363 $red 3
    }
    if ($layout -notin @('classic','gauges','minimal')) {
        switch($layout) {
            'drone-swarm' {
                for($i=0;$i -lt 8;$i++){ $a=$i*[Math]::PI/4+$phase*.6; $x=206+[Math]::Cos($a)*142;$y=236+[Math]::Sin($a)*80; Line ($x-14) ($y-9) ($x+14) ($y+9) $blue; Line ($x-14) ($y+9) ($x+14) ($y-9) $blue; foreach($k in @(-1,1)){foreach($j in @(-1,1)){Circle ($x+$k*14) ($y+$j*9) 7 (Dim $blue .6)}} }
                CombatHud $blue
            }
            'lightning-strike' {
                for($i=0;$i -lt 7;$i++){ $x=60+$i*48;$prev=$x;for($y=110;$y -lt 323;$y+=18){$next=$x+18*[Math]::Sin($phase*8+$i+$y);Line $prev $y $next ($y+18) (Dim $red .65);$prev=$next}}
                CombatHud $red
            }
            'space-fleet' {
                for($i=0;$i -lt 14;$i++){ $t=($phase*.15+$i*.137)%1;$x=38+($i*61)%336;$y=390-$t*360;Triangle $x ($y-14) ($x-9) ($y+9) ($x+9) ($y+9) (Dim $purple .7);Line $x ($y+10) $x ($y+24) (Dim $orange .8)}
                CombatHud $purple
            }
            'race-grid' {
                for($i=-7;$i -le 7;$i++){Line (206+$i*9) 210 (206+$i*48) 385 (Dim $gold .4)};for($i=0;$i -lt 11;$i++){$t=($phase*.4+$i*.1)%1;$y=210+$t*$t*175;Line 35 $y 377 $y (Dim $gold (.2+$t*.4))}
                CombatHud $gold
            }
            'portal-rift' {
                for($i=0;$i -lt 44;$i++){ $a=$i*[Math]::PI/22+$phase;$r=108+16*[Math]::Sin($i*.7+$phase*3);Line (206+[Math]::Cos($a)*$r) (247+[Math]::Sin($a)*$r*.55) (206+[Math]::Cos($a+.15)*($r+9)) (247+[Math]::Sin($a+.15)*($r+9)*.55) (Dim $tactical .8)}
                CombatHud $tactical
            }
            'tactical-steel' {
                for($i=0;$i -lt 6;$i++){Line (40+$i*5) (100+$i*5) (372-$i*5) (100+$i*5) (Dim $blue (.12+$i*.06));Line (40+$i*5) (327-$i*5) (372-$i*5) (327-$i*5) (Dim $blue (.12+$i*.06))};for($x=66;$x -lt 355;$x+=28){Dot $x 302 3 $gold}
                CombatHud $blue
            }
            'ammo-vault' {
                for($i=0;$i -lt 11;$i++){$x=58+$i*28;Triangle $x 261 ($x+6) 248 ($x+12) 261 $gold;Fill $x 261 12 43 (Dim $gold .6);Line $x 296 ($x+12) 296 $red}
                CombatHud $red
            }
            'samurai-crest' {
                Circle 206 253 71 (Dim $purple .5);Triangle 140 292 206 241 272 292 (Dim $purple .6);Triangle 150 275 127 219 189 259 $gold;Triangle 262 275 285 219 223 259 $gold;Line 155 310 257 237 $purple;Line 155 237 257 310 $purple
                CombatHud $purple
            }
            'retro-arcade' {
                for($i=0;$i -lt 8;$i++){$x=75+$i*34;$a=if($i%2){$gold}else{$purple};$b=if($i%2){$purple}else{$gold};Fill $x 276 15 15 $a;Fill ($x+16) 292 15 15 $b};Line 57 111 355 111 (Dim $gold .6);Line 57 322 355 322 (Dim $gold .6)
                CombatHud $gold
            }
            'stealth-map' {
                for($i=0;$i -lt 10;$i++){$x=53+($i*73)%310;$y=117+($i*47)%205;Line $x $y ($x+22+($i%3)*12) $y (Dim $tactical .35);Line $x $y $x ($y+25) (Dim $tactical .35)};Circle 206 263 42 $tactical;Line 145 263 267 263 $tactical;Line 206 202 206 324 $tactical
                CombatHud $tactical
            }

            'arsenal' {
                for($y=115;$y -lt 320;$y+=20){Line 75 $y 337 $y (Dim $tactical .08)}
                $flash=($phase%1.6) -lt .13
                $x=76+$(if($flash){-3}else{0});$y=251;$metal=[Drawing.Color]::FromArgb(108,125,108)
                Triangle $x ($y+9) ($x+46) ($y+3) ($x+46) ($y+29) $metal
                Fill ($x+43) $y 92 27 $metal; Fill ($x+130) ($y+6) 83 9 $metal; Fill ($x+210) ($y+3) 13 15 $metal
                Fill ($x+73) ($y-10) 45 7 $metal; Fill ($x+81) ($y-3) 5 8 $metal
                Triangle ($x+98) ($y+25) ($x+118) ($y+25) ($x+106) ($y+59) $metal
                Triangle ($x+98) ($y+25) ($x+106) ($y+59) ($x+87) ($y+53) $metal
                Triangle ($x+59) ($y+25) ($x+75) ($y+25) ($x+63) ($y+47) $metal
                Line ($x+52) ($y+8) ($x+120) ($y+8) (Dim $gold .7)
                if($flash){
                    Triangle ($x+224) ($y+10) ($x+245) ($y-1) ($x+240) ($y+21) $orange
                    Triangle ($x+225) ($y+10) ($x+235) ($y+5) ($x+235) ($y+15) $gold
                }
                for($shell=0;$shell -lt 5;$shell++){
                    $t=($phase*.7+$shell*.2)%1; Fill (176+$t*115) (244-65*[Math]::Sin($t*[Math]::PI)) 8 3 (Dim $gold (.6*(1-$t)))
                }; CombatHud $tactical
            }
            'bullet-storm' {
                for($bullet=0;$bullet -lt 18;$bullet++){
                    $t=($phase*(.13+$bullet%3*.025)+$bullet*.137)%1; $x=35+($bullet*53)%340; $y=425-$t*470
                    Line ($x+6) ($y+35) ($x+17) ($y+68) (Dim $gold .13)
                    Triangle $x $y ($x+6) ($y-11) ($x+12) $y (Dim $gold .85)
                    Fill $x $y 12 24 (Dim $gold .5); Line $x ($y+20) ($x+12) ($y+20) (Dim $orange .8)
                }; CombatHud $gold
            }
            'inferno' {
                for($x=14;$x -lt 398;$x+=9){
                    $height=90+30*[Math]::Sin($x*.08+$phase*2)+24*[Math]::Sin($x*.17-$phase*3)
                    Triangle ($x-8) 399 ($x+6) 399 ($x+2) (399-$height) ([Drawing.Color]::FromArgb(155,27,12))
                    Triangle ($x-5) 400 ($x+6) 400 ($x+1) (419-$height) (Dim $orange .75)
                    Triangle ($x-3) 402 ($x+5) 402 ($x+1) (446-$height) (Dim $gold .8)
                }
                for($spark=0;$spark -lt 30;$spark++){
                    $t=($phase*.22+$spark*.081)%1; Dot (35+($spark*67)%340+8*[Math]::Sin($phase+$spark)) (400-$t*270) $(if($spark%4 -eq 0){2}else{1}) (Dim $gold ((1-$t)*.8))
                }; CombatHud $orange
            }
            'warzone-city' {
                for($layer=0;$layer -lt 2;$layer++){for($b=0;$b -lt 18;$b++){
                    $speed=if($layer){7}else{3};$x=($b*29+430-[int]($phase*$speed)%430)%430-20
                    $height=45+($b*31+$layer*47)%115;$width=if($layer){25}else{28};$base=if($layer){320}else{289}
                    $color=if($layer){[Drawing.Color]::FromArgb(34,42,49)}else{[Drawing.Color]::FromArgb(17,23,30)}
                    Fill $x ($base-$height) $width $height $color
                    for($wy=$base-$height+9;$wy -lt $base-5;$wy+=14){for($wx=$x+5;$wx -lt $x+$width-3;$wx+=9){
                        if(($wx+$wy+$b)%3){$c=if($b%4){$blue}else{$gold};Fill $wx $wy 3 5 (Dim $c .35)}
                    }}
                    if($layer -and $b%5 -eq 0){$flare=10+6*[Math]::Sin($phase*4+$b);Triangle ($x+4) $base ($x+22) $base ($x+13) ($base-$flare-13) (Dim $orange .8)}
                }}
                for($tracer=0;$tracer -lt 4;$tracer++){$t=($phase*.45+$tracer*.25)%1;$x=40+$t*340;$y=305-$t*165;Line $x $y ($x+22) ($y-11) (Dim $gold .55)}
                CombatHud $orange
            }
            'sniper-scope' {
                Circle 206 206 185 (Dim $tactical .5);Circle 206 206 153 (Dim $tactical .17)
                Line 22 206 390 206 (Dim $tactical .4);Line 206 22 206 390 (Dim $tactical .4)
                for($offset=-150;$offset -le 150;$offset+=15){Line (206+$offset) 202 (206+$offset) 211 (Dim $tactical .6);Line 202 (206+$offset) 211 (206+$offset) (Dim $tactical .6)}
                $x=125+[Math]::Sin($phase*.7)*34;$y=271+[Math]::Cos($phase*.9)*13
                Line ($x-17) ($y-17) ($x+17) ($y-17) $tactical;Line ($x-17) ($y+17) ($x+17) ($y+17) $tactical
                Line ($x-17) ($y-17) ($x-17) ($y+17) $tactical;Line ($x+17) ($y-17) ($x+17) ($y+17) $tactical
                Dot $x $y 3 $tactical;CombatHud $tactical
            }
            'cyber-ops' {
                for($wire=0;$wire -lt 14;$wire++){
                    $y=114+$wire*15;$x=35+($wire%4)*11;$turn=104+($wire*31)%193;$color=if($wire%2){$purple}else{$blue}
                    Line $x $y $turn $y (Dim $color .22);Line $turn $y ($turn+26) ($y+18) (Dim $color .22);Line ($turn+26) ($y+18) 379 ($y+18) (Dim $color .22)
                    $t=($phase*.22+$wire*.13)%1;$px=$x+$t*(379-$x);$py=if($px -lt $turn){$y}elseif($px -gt $turn+26){$y+18}else{$y+($px-$turn)*18/26}
                    Dot $px $py 2 (Dim $color .9)
                };CombatHud $blue
            }
            'plasma-core' {
                for($bolt=0;$bolt -lt 18;$bolt++){
                    $angle=($bolt*20+$phase*18)*[Math]::PI/180;$r=72+14*[Math]::Sin($phase*4+$bolt)
                    $x=206+[Math]::Cos($angle)*$r;$y=270+[Math]::Sin($angle)*$r;$mx=(206+$x)/2+7*[Math]::Sin($phase*7+$bolt);$my=(270+$y)/2
                    Line 206 270 $mx $my (Dim $purple .45);Line $mx $my $x $y (Dim $blue .7)
                }
                for($radius=43;$radius -gt 0;$radius-=4){$c=if($radius -gt 31){$purple}else{$blue};Dot 206 270 $radius (Dim $c (.12+(43-$radius)*.012))}
                Circle 206 270 (43+3*[Math]::Sin($phase*3)) (Dim $purple .8);CombatHud $purple
            }
            'boss-fight' {
                $sprite=@('001101100','011111110','110111011','111111111','001111100','010010010','100000001')
                $x=156+[Math]::Sin($phase*1.8)*16;$y=237+[Math]::Sin($phase*2.7)*5
                for($row=0;$row -lt 7;$row++){for($column=0;$column -lt 9;$column++){
                    if($sprite[$row][$column] -eq '1'){$c=if($row -lt 3){$purple}else{$red};Fill ($x+$column*11) ($y+$row*11) 10 10 (Dim $c .9)}
                }}
                $beam=(1+[Math]::Sin($phase*3))/2;Line ($x+22) ($y+25) 61 322 (Dim $red (.2+$beam*.45));Line ($x+77) ($y+25) 351 322 (Dim $red (.2+$beam*.45));CombatHud $red
            }
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
            'neon-pulse' { for($r=196;$r -lt 202;$r++){Circle 206 206 $r (Dim $purple (.12+.18*(1+[Math]::Sin($phase*2))/2))}; Rows $true }
            'reactor' {
                for($ring=0;$ring -lt 3;$ring++){for($segment=0;$segment -lt 48;$segment++){
                    $speed=if($ring%2){-16}else{22}; $color=if($ring -eq 1){$purple}else{$mint}
                    Polar (152+$ring*15) (161+$ring*15) ($segment*7.5+$phase*$speed) (Dim $color .65)
                }}
                Hex 206 206 (107+4*[Math]::Sin($phase*2)) (Dim $mint .45); Fps; Temperatures 341
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
                    $scan=83+($phase*40)%246
                    Line $scan $top $scan ($top+40) (Dim $color .3)
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
                SpeedDial 'FPS' $demoFps 240 206 164 88 $false
                SpeedDial 'CPU' $demoCpu 120 122 305 65 $true
                SpeedDial 'GPU' $demoGpu 120 290 305 65 $true
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
                    if([Math]::Pow($x-206,2)+[Math]::Pow($y-206,2) -lt 172*172){Hex $x $y 29 (Dim $blue (.08+.23*(1+[Math]::Sin($phase*1.5-$column*.7-$row*.35))/2))}
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
            Text 'QSCREEN' 206 40 22 $white
        } else {
            if($layout -eq 'matrix') { $g.FillRectangle([Drawing.Brushes]::Black,76,31,260,74) }
            Text 'QSCREEN' 206 55 22 $white
            $title=if($layout -eq 'waveform'){'THERMAL HISTORY'}else{$layout.Replace('-',' ').ToUpperInvariant()}
            Text $title 206 88 22 $grey
        }
    } elseif ($layout -eq 'classic') {
        Text 'QSCREEN //' 206 90 39 $white
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
        Text 'QSCREEN' 206 77 39 $white
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
    if($FrameIndex -ge 0){
        $extension=$FrameFormat.ToLowerInvariant()
        $format=if($FrameFormat -eq 'Png'){[Drawing.Imaging.ImageFormat]::Png}else{[Drawing.Imaging.ImageFormat]::Bmp}
        $bitmap.Save((Join-Path $output ('{0}-{1:D2}.{2}' -f $layout,$FrameIndex,$extension)),$format)
    }else{
        $bitmap.Save((Join-Path $output "layout-$layout.bmp"), [Drawing.Imaging.ImageFormat]::Bmp)
        $bitmap.Save((Join-Path $output "layout-$layout.png"), [Drawing.Imaging.ImageFormat]::Png)
    }
    $g.Dispose(); $clip.Dispose(); $bitmap.Dispose()
}
