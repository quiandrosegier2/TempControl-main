# TempControl

A compact PC system monitor featuring a round Waveshare ESP32-S3-Touch-LCD-1.46 display. View your CPU temperature, GPU temperature, and FPS on a dedicated screen connected via USB-C.

### Features
- One Windows installer for the companion app and USB display firmware; no VS Code, Python, PlatformIO or .NET installation needed by end users.
- Live CPU temperature, GPU temperature, and FPS readings.
- Blue values and rings at or below 50 °C, turning red above 50 °C.
- Windows companion app that sends monitoring data to the display.
- Waiting indicator when no PC data is received.

### Project structure
- `TempControl-main/firmware/`: firmware for the ESP32-S3 display.
- `TempControl-main/pc-app/`: Windows app for collecting and transmitting monitoring data.
- `TempControl-main/pc-app/installer/`: Windows installer files.

### Installing

Download the setup executable from [GitHub Releases](https://github.com/quiandrosegier2/TempControl-main/releases/latest).

1. Run `TempMonitor-Setup-1.0.0.exe` on a Windows x64 PC and allow administrator access.
2. Connect a **Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB)** using a USB-C data cable.
3. Leave the firmware installation option selected at the end of Setup.
4. Select the display's COM port and click **Firmware installeren**. This replaces any existing firmware on that display.
5. Keep the cable connected until the upload finishes. Press RESET or reconnect the display if needed.

If no port appears or connecting fails, hold BOOT, press RESET, release BOOT and click **Vernieuwen**. Select the new COM port and retry. Only Espressif USB devices are listed; verify you selected the supported Waveshare display.

You can repeat the upload from **Start > TempMonitor > Firmware installeren**. The monitoring app pauses during the upload and resumes afterwards. Upload logs are saved in `C:\ProgramData\TempMonitor`.

The app, .NET runtime, firmware, uploader and optional PawnIO driver are included. No internet connection or winget is needed during installation. Windows may request a restart after installing the driver. Developer build instructions are in [the project guide](TempControl-main/Readme.md).

Built with PlatformIO, .NET, LibreHardwareMonitor, and PresentMon.
