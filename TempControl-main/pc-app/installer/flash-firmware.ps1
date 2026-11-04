# Standalone firmwarevenster; wordt door Setup en de startmenusnelkoppeling geopend.
param([switch]$ValidateOnly)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'firmware-safety.ps1')
function Stop-MonitoringProcesses {
    # Layout Studio uses the same executable and must stay open during uploads.
    Get-CimInstance Win32_Process -Filter "Name='Qscreen.exe' OR Name='TempMonitor.exe'" | Where-Object {
        $_.CommandLine -notmatch '(?i)(?:^|\s)--settings(?:\s|$)' -and
        ($_.Name -eq 'TempMonitor.exe' -or $_.CommandLine -match '(?i)(?:^|\s)--(?:monitor|port)(?:\s|$)')
    } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Get-Process -Name PresentMon -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $PSScriptRoot 'PresentMon.exe') } | Stop-Process -Force
}
$image = Join-Path $PSScriptRoot 'firmware\TempControl.factory.bin'
$tool = Join-Path $PSScriptRoot 'firmware\esptool\esptool.exe'
if ($ValidateOnly) {
    if (-not (Test-Path $image) -or -not (Test-Path $tool)) { throw 'Firmware of esptool ontbreekt' }
    & $tool --chip esp32s3 image-info $image
    if ($LASTEXITCODE) { throw 'Ongeldige firmware' }
    return
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try {
        Start-Process -FilePath "$PSHOME\powershell.exe" -Verb RunAs `
            -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + $PSCommandPath + '"') | Out-Null
    } catch {
        [void][Windows.Forms.MessageBox]::Show('Beheerdersrechten zijn nodig om Qscreen tijdelijk te stoppen en de firmware te installeren.', 'Qscreen')
    }
    return
}
[Windows.Forms.Application]::EnableVisualStyles()
if (-not ('QscreenWindowStyle' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class QscreenWindowStyle {
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("uxtheme.dll", CharSet=CharSet.Unicode)]
    public static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);
}
'@
}
$form = New-Object Windows.Forms.Form
$form.Text = 'Qscreen - Firmware'
$form.ClientSize = New-Object Drawing.Size(660, 530)
$form.BackColor = [Drawing.Color]::FromArgb(24,25,31)
$form.ForeColor = [Drawing.Color]::FromArgb(238,239,244)
$form.Font = [Drawing.Font]::new('Segoe UI',9)
$form.Add_HandleCreated({
    $dark = 1
    [void][QscreenWindowStyle]::DwmSetWindowAttribute($form.Handle,20,[ref]$dark,4)
})
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$brandLogo = New-Object Windows.Forms.PictureBox
$brandLogo.SetBounds(18,10,205,46)
$brandLogo.SizeMode = 'Zoom'
$brandPath = Join-Path $PSScriptRoot 'branding\qprints-wordmark.png'
if (Test-Path $brandPath) {
    $stream = [IO.MemoryStream]::new([IO.File]::ReadAllBytes($brandPath))
    $loaded = [Drawing.Image]::FromStream($stream)
    $brandLogo.Image = [Drawing.Bitmap]::new($loaded)
    $loaded.Dispose(); $stream.Dispose()
}
$appIcon = $null
$iconPath = Join-Path $PSScriptRoot 'branding\qprints.ico'
if (Test-Path $iconPath) { $appIcon = [Drawing.Icon]::new($iconPath); $form.Icon = $appIcon }
$intro = New-Object Windows.Forms.Label
$intro.SetBounds(18, 65, 620, 100)
$intro.Text = "Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB). Kies je USB-poort.`r`nQscreen maakt eerst een volledige herstelkopie en controleert de indeling.`r`nBijwerken behoudt partities en gebruikersdata. BOOT/RESET bij verbindingsproblemen."
$initialize = New-Object Windows.Forms.CheckBox
$initialize.SetBounds(18,142,620,24)
$initialize.Text = 'Nieuw leeg scherm initialiseren (alleen volledig gewist flashgeheugen)'
$initialize.ForeColor = $form.ForeColor
$form.Controls.Add($initialize)
$ports = New-Object Windows.Forms.ComboBox
$ports.SetBounds(18, 170, 440, 28)
$ports.DropDownStyle = 'DropDownList'
$ports.BackColor = [Drawing.Color]::FromArgb(36,38,47)
$ports.ForeColor = $form.ForeColor
$ports.FlatStyle = 'Flat'
$ports.DrawMode = 'OwnerDrawFixed'
$ports.ItemHeight = 22
$ports.Add_DrawItem({
    param($sender,$eventArgs)
    $brush = [Drawing.SolidBrush]::new($ports.BackColor)
    $eventArgs.Graphics.FillRectangle($brush,$eventArgs.Bounds)
    $brush.Dispose()
    if ($eventArgs.Index -ge 0) {
        $flags = [Windows.Forms.TextFormatFlags]::Left -bor [Windows.Forms.TextFormatFlags]::VerticalCenter -bor [Windows.Forms.TextFormatFlags]::NoPrefix
        [Windows.Forms.TextRenderer]::DrawText($eventArgs.Graphics,[string]$ports.Items[$eventArgs.Index],$ports.Font,$eventArgs.Bounds,$ports.ForeColor,$flags)
    }
})
$refresh = New-Object Windows.Forms.Button
$refresh.SetBounds(470, 168, 170, 30)
$refresh.Text = 'Vernieuwen'
$refresh.FlatStyle = 'Flat'
$refresh.BackColor = [Drawing.Color]::FromArgb(43,45,55)
$refresh.ForeColor = $form.ForeColor
$refresh.FlatAppearance.BorderColor = [Drawing.Color]::FromArgb(75,78,93)
$flash = New-Object Windows.Forms.Button
$flash.SetBounds(18, 212, 220, 34)
$flash.Text = 'Firmware installeren'
$flash.FlatStyle = 'Flat'
$flash.BackColor = [Drawing.Color]::FromArgb(190,35,44)
$flash.ForeColor = [Drawing.Color]::White
$flash.FlatAppearance.BorderSize = 0
$log = New-Object Windows.Forms.TextBox
$log.SetBounds(18, 262, 622, 246)
$log.BackColor = [Drawing.Color]::FromArgb(32,34,42)
$log.ForeColor = $form.ForeColor
$log.BorderStyle = 'FixedSingle'
$log.Multiline = $true
$log.ReadOnly = $true
$log.ScrollBars = 'Vertical'
$form.Controls.AddRange(@($brandLogo, $intro, $ports, $refresh, $flash, $log))
[void][QscreenWindowStyle]::SetWindowTheme($ports.Handle,'DarkMode_CFD',$null)
[void][QscreenWindowStyle]::SetWindowTheme($log.Handle,'DarkMode_Explorer',$null)
$script:flashing = $false

function Refresh-Ports {
    $ports.Items.Clear()
    try {
        # Toon alleen native Espressif USB-poorten, geen willekeurige seriele apparaten.
        $devices = @(Get-CimInstance Win32_PnPEntity -Filter "PNPDeviceID LIKE '%VID_303A%'" |
            Where-Object { $_.Name -match '\(COM\d+\)' })
        foreach ($device in $devices) { [void]$ports.Items.Add($device.Name) }
        $ports.SelectedIndex = -1
        $flash.Enabled = $false
        if ($ports.Items.Count -eq 0) { $log.Text = 'Geen Espressif USB-scherm gevonden. Controleer de kabel en gebruik BOOT/RESET, daarna Vernieuwen.' }
        else { $log.Text = 'Kies je Waveshare-scherm en klik op Firmware installeren.' }
    } catch {
        $flash.Enabled = $false
        $log.Text = "USB-poorten ophalen mislukt: $($_.Exception.Message)"
    }
}
$refresh.Add_Click({ Refresh-Ports })
$ports.Add_SelectedIndexChanged({ $flash.Enabled = -not $script:flashing -and $ports.SelectedIndex -ge 0 })
$form.Add_FormClosing({ param($sender, $eventArgs) if ($script:flashing) { $eventArgs.Cancel = $true } })
$flash.Add_Click({
    if ($ports.SelectedItem -notmatch '\((COM\d+)\)') { return }
    $selectedPort = $Matches[1]
    $answer = [Windows.Forms.MessageBox]::Show(
        "Is $selectedPort het Waveshare ESP32-S3-Touch-LCD-1.46-scherm? De bestaande firmware wordt vervangen.",
        'Firmware installeren', 'YesNo', 'Question')
    if ($answer -ne 'Yes') { return }
    $script:flashing = $true
    $flash.Enabled = $false
    $refresh.Enabled = $false
    $ports.Enabled = $false
    $resumeMonitor = $false
    $process = $null
    try {
        if (-not (Test-Path $image) -or -not (Test-Path $tool)) { throw 'Firmware of uploadprogramma ontbreekt. Installeer Qscreen opnieuw.' }
        $identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Start Firmware installeren opnieuw als administrator om Qscreen tijdelijk te kunnen stoppen.'
        }
        $task = Get-ScheduledTask -TaskName TempMonitor -ErrorAction SilentlyContinue
        $resumeMonitor = $null -ne $task -and $task.State -eq 'Running'
        if ($resumeMonitor) { Stop-ScheduledTask -TaskName TempMonitor }
        Stop-MonitoringProcesses
        $logDir = Join-Path $env:ProgramData 'TempMonitor'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        $stdout = Join-Path $logDir 'firmware-upload.log'
        $stderr = Join-Path $logDir 'firmware-upload-error.log'
        function Run-FirmwareTool([string[]]$Command,[string]$Caption) {
            $arguments=@('--chip','esp32s3','--port',$selectedPort,'--baud','460800')+$Command
            $script:firmwareProcess=Start-Process -FilePath $tool -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            try {
                while(!$script:firmwareProcess.HasExited){$log.Text=$Caption+"`r`n"+(Get-Content $stdout -Raw -ErrorAction SilentlyContinue);$log.SelectionStart=$log.TextLength;$log.ScrollToCaret();[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 100}
                $script:firmwareProcess.WaitForExit()
                if($script:firmwareProcess.ExitCode -ne 0){throw ($Caption+" mislukt. "+(Get-Content $stderr -Raw -ErrorAction SilentlyContinue))}
            }finally{$script:firmwareProcess.Dispose();$script:firmwareProcess=$null}
        }
        $backupDir=Join-Path $logDir ('firmware-backups/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        $backup=Join-Path $backupDir 'flash-16MB.bin'
        Run-FirmwareTool @('read-flash','0','0x1000000',('"'+$backup+'"')) 'Volledige herstelkopie maken. Laat de USB-kabel aangesloten.'
        $plan=Get-SafeFirmwarePlan -Backup ([IO.File]::ReadAllBytes($backup)) -Factory ([IO.File]::ReadAllBytes($image)) -AllowBlank $initialize.Checked
        $candidate=Join-Path $backupDir 'candidate.bin';[IO.File]::WriteAllBytes($candidate,$plan.Image)
        (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash | Set-Content (Join-Path $backupDir 'backup.sha256')
        Run-FirmwareTool @('write-flash',$plan.Offset,('"'+$candidate+'"')) 'Gecontroleerde firmware schrijven'
        Run-FirmwareTool @('verify-flash',$plan.Offset,('"'+$candidate+'"')) 'Geschreven firmware teruglezen en controleren'
        $log.Text="Firmware geschreven en geverifieerd. Herstelkopie: "+$backupDir
        $log.AppendText("`r`nKlaar! Druk zo nodig op RESET of sluit het scherm opnieuw aan. Qscreen maakt automatisch verbinding.")
    } catch {
        $log.AppendText("`r`n$($_.Exception.Message)")
    } finally {
        if ($resumeMonitor) {
            try { Start-ScheduledTask -TaskName TempMonitor } catch { $log.AppendText("`r`nQscreen herstarten mislukt. Meld je opnieuw aan bij Windows.") }
        }
        if ($process) { $process.Dispose() }
        $script:flashing = $false
        $flash.Enabled = $ports.Items.Count -gt 0
        $refresh.Enabled = $true
        $ports.Enabled = $true
    }
})
Refresh-Ports
[void]$form.ShowDialog()
if ($brandLogo.Image) { $brandLogo.Image.Dispose() }
if ($appIcon) { $appIcon.Dispose() }
$form.Dispose()
