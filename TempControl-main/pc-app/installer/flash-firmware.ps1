# Standalone firmwarevenster; wordt door Setup en de startmenusnelkoppeling geopend.
param([switch]$ValidateOnly)

$ErrorActionPreference = 'Stop'
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
        [void][Windows.Forms.MessageBox]::Show('Beheerdersrechten zijn nodig om TempMonitor tijdelijk te stoppen en de firmware te installeren.', 'TempControl')
    }
    return
}
[Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object Windows.Forms.Form
$form.Text = 'TempControl - Firmware installeren'
$form.ClientSize = New-Object Drawing.Size(660, 470)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$intro = New-Object Windows.Forms.Label
$intro.SetBounds(18, 15, 620, 100)
$intro.Text = "Alleen voor Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB).`r`nSluit het scherm aan met een USB-C-datakabel en kies de poort hieronder.`r`nGeen poort of geen verbinding? Houd BOOT ingedrukt, druk op RESET,`r`nlaat BOOT los en klik op Vernieuwen. De COM-poort kan dan veranderen.`r`nInstalleren vervangt de bestaande firmware op het geselecteerde scherm."
$ports = New-Object Windows.Forms.ComboBox
$ports.SetBounds(18, 120, 440, 28)
$ports.DropDownStyle = 'DropDownList'
$refresh = New-Object Windows.Forms.Button
$refresh.SetBounds(470, 118, 170, 30)
$refresh.Text = 'Vernieuwen'
$flash = New-Object Windows.Forms.Button
$flash.SetBounds(18, 160, 220, 34)
$flash.Text = 'Firmware installeren'
$log = New-Object Windows.Forms.TextBox
$log.SetBounds(18, 210, 622, 240)
$log.Multiline = $true
$log.ReadOnly = $true
$log.ScrollBars = 'Vertical'
$form.Controls.AddRange(@($intro, $ports, $refresh, $flash, $log))
$script:flashing = $false

function Refresh-Ports {
    $ports.Items.Clear()
    try {
        # Toon alleen native Espressif USB-poorten, geen willekeurige seriele apparaten.
        $devices = @(Get-CimInstance Win32_PnPEntity -Filter "PNPDeviceID LIKE '%VID_303A%'" |
            Where-Object { $_.Name -match '\(COM\d+\)' })
        foreach ($device in $devices) { [void]$ports.Items.Add($device.Name) }
        if ($ports.Items.Count -gt 0) { $ports.SelectedIndex = 0 }
        $flash.Enabled = $ports.Items.Count -gt 0
        if ($ports.Items.Count -eq 0) { $log.Text = 'Geen Espressif USB-scherm gevonden. Controleer de kabel en gebruik BOOT/RESET, daarna Vernieuwen.' }
        else { $log.Text = 'Kies je Waveshare-scherm en klik op Firmware installeren.' }
    } catch {
        $flash.Enabled = $false
        $log.Text = "USB-poorten ophalen mislukt: $($_.Exception.Message)"
    }
}
$refresh.Add_Click({ Refresh-Ports })
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
        if (-not (Test-Path $image) -or -not (Test-Path $tool)) { throw 'Firmware of uploadprogramma ontbreekt. Installeer TempMonitor opnieuw.' }
        $identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Start Firmware installeren opnieuw als administrator om TempMonitor tijdelijk te kunnen stoppen.'
        }
        $task = Get-ScheduledTask -TaskName TempMonitor -ErrorAction SilentlyContinue
        $resumeMonitor = $null -ne $task -and $task.State -eq 'Running'
        if ($resumeMonitor) { Stop-ScheduledTask -TaskName TempMonitor }
        Get-Process -Name TempMonitor, PresentMon -ErrorAction SilentlyContinue | Stop-Process -Force
        $logDir = Join-Path $env:ProgramData 'TempMonitor'
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        $stdout = Join-Path $logDir 'firmware-upload.log'
        $stderr = Join-Path $logDir 'firmware-upload-error.log'
        $log.Text = "Firmware wordt geschreven. Laat de USB-kabel aangesloten.`r`n"
        $arguments = @('--chip', 'esp32s3', '--port', $selectedPort, '--baud', '460800',
            '--before', 'default-reset', '--after', 'hard-reset', 'write-flash',
            '--flash-size', '16MB', '0x0', ('"' + $image + '"'))
        $process = Start-Process -FilePath $tool -ArgumentList $arguments -PassThru -WindowStyle Hidden `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        while (-not $process.HasExited) {
            $log.Text = "Firmware wordt geschreven. Laat de USB-kabel aangesloten.`r`n" + (Get-Content $stdout -Raw -ErrorAction SilentlyContinue)
            $log.SelectionStart = $log.TextLength
            $log.ScrollToCaret()
            [Windows.Forms.Application]::DoEvents()
            Start-Sleep -Milliseconds 100
        }
        $process.WaitForExit()
        $log.Text = (Get-Content $stdout -Raw) + "`r`n" + (Get-Content $stderr -Raw)
        if ($process.ExitCode -ne 0) { throw "Upload mislukt (code $($process.ExitCode)). Gebruik BOOT/RESET, klik op Vernieuwen en probeer opnieuw. Logs: $logDir" }
        $log.AppendText("`r`nKlaar! Druk zo nodig op RESET of sluit het scherm opnieuw aan. TempMonitor maakt automatisch verbinding.")
    } catch {
        $log.AppendText("`r`n$($_.Exception.Message)")
    } finally {
        if ($resumeMonitor) {
            try { Start-ScheduledTask -TaskName TempMonitor } catch { $log.AppendText("`r`nTempMonitor herstarten mislukt. Meld je opnieuw aan bij Windows.") }
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
$form.Dispose()
