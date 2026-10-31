# TempControl

A compact PC system monitor featuring a round Waveshare ESP32-S3-Touch-LCD-1.46 display. View your CPU temperature, GPU temperature, and FPS on a dedicated screen connected via USB-C.

### Features
- One Windows installer for the companion app and USB display firmware; no VS Code, Python, PlatformIO or .NET installation needed by end users.
- **Qprints esp installer** features a dark wizard and firmware window, with the QPRINTS // logo visible throughout setup.
- Live CPU temperature, GPU temperature, and FPS readings.
- Blue values and rings at or below 50 °C, turning red above 50 °C.
- Windows companion app that sends monitoring data to the display.
- Waiting indicator when no PC data is received.
- Choose from 21 layouts in Setup, including eighteen animated themes and eight gaming HUDs. Moving offline previews with Play/Pause let you try the animated layouts before installing. The display remembers the selection.

### Project structure
- `TempControl-main/firmware/`: firmware for the ESP32-S3 display.
- `TempControl-main/pc-app/`: Windows app for collecting and transmitting monitoring data.
- `TempControl-main/pc-app/installer/`: Windows installer files.

### Installing

Download the setup executable from [GitHub Releases](https://github.com/quiandrosegier2/TempControl-main/releases/latest).

1. Run `Qprints esp installer-1.3.1.exe` on a Windows x64 PC and allow administrator access. Choose a layout and watch its demo on the layout page; use **Pauzeren/Afspelen** to pause or play.
2. Connect a **Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB)** using a USB-C data cable.
3. Leave the firmware installation option selected at the end of Setup.
4. Select the display's COM port and click **Firmware installeren**. This replaces any existing firmware on that display.
5. Keep the cable connected until the upload finishes. Press RESET or reconnect the display if needed.

If no port appears or connecting fails, hold BOOT, press RESET, release BOOT and click **Vernieuwen**. Select the new COM port and retry. Only Espressif USB devices are listed; verify you selected the supported Waveshare display.

You can repeat the upload from **Start > TempMonitor > Firmware installeren**. The monitoring app pauses during the upload and resumes afterwards. Upload logs are saved in `C:\ProgramData\TempMonitor`.

The app, .NET runtime, firmware, uploader and optional PawnIO driver are included. No internet connection or winget is needed during installation. Windows may request a restart after installing the driver. Developer build instructions are in [the project guide](TempControl-main/Readme.md).

To change layouts later, run Setup again and choose another layout. Upload the bundled firmware once to get all twenty-one layouts and the current Speedometer design. After that, leave the final firmware installation option unchecked when changing layouts: no new flash is needed. Updates preserve the current layout selection.

### Layouts

| Layout | Appearance |
| --- | --- |
| Classic | Original three-row monitor |
| Gauges | Round CPU and GPU temperature meters |
| Minimal | Large FPS reading with compact temperatures |
| Orbit | Animated lights circling the FPS reading |
| Radar | Rotating radar sweep behind the readings |
| Neon Pulse | Pulsing neon panels and value bars |
| Reactor | Counter-rotating segmented rings and a pulsing core |
| Waveform | Real temperature history with moving scan lines |
| Matrix | Falling digit columns behind dark reading panels |
| Starfield | Stars moving outward around the FPS reading |
| Speedometer | Three independent needles: large FPS dial (0–240) above smaller CPU/GPU temperature dials (0–120 °C) |
| Aurora | Gently moving curtains of light |
| Hex Grid | A glowing honeycomb with hexagonal reading panels |
| Arsenal | Recoiling rifle silhouette, muzzle flashes and ejected casings |
| Bullet Storm | Moving gold cartridges and tracer streaks |
| Inferno | Animated flames and rising embers |
| Warzone City | Parallax city buildings, fires and moving tracers |
| Sniper Scope | Green scope marks and a moving target frame |
| Cyber Ops | Neon circuitry with travelling data pulses |
| Plasma Core | A pulsing energy orb and electrical arcs |
| Boss Fight | An animated pixel boss, lasers and live temperature bars |

Animations run independently of incoming readings. Waveform retains up to 60 actual temperature samples in memory; unavailable readings remain gaps. Setup includes four-second looping, schematic demos with sample values for all eighteen moving themes; Classic, Gauges and Minimal have static previews. No screen or internet connection is needed to view the previews. Temperature values turn red above 50 °C in every layout.

[View all layouts](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.3.0/Layout-Overview-1.3.0.png) or [the eight gaming themes](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.3.0/Gaming-Layouts-1.3.0.png).

Gaming artwork is original and generated in code. Weapons, bullets, fire and enemy sprites are decorative animations; FPS, CPU/GPU temperatures and temperature bars use real incoming measurements. Upload the 1.3.0 firmware once to enable the new gaming themes.

Speedometer needles smoothly follow their own live readings; the digital numbers show the actual values. Missing readings hide the relevant needle. Values beyond a dial's range pin its needle at the scale limit while retaining the actual digital reading. Upload the 1.2.2 firmware once to get the three-dial Speedometer design.

Built with PlatformIO, .NET, LibreHardwareMonitor, and PresentMon.
