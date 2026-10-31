# TempControl-main
# TempControl

A compact PC system monitor featuring a round Waveshare ESP32-S3-Touch-LCD-1.46 display. View your CPU temperature, GPU temperature, and FPS on a dedicated screen connected via USB-C.

### Features
- Live CPU temperature, GPU temperature, and FPS readings.
- Blue values and rings at or below 50 °C, turning red above 50 °C.
- Windows companion app that sends monitoring data to the display.
- Waiting indicator when no PC data is received.

### Project structure
- `firmware/`: firmware for the ESP32-S3 display.
- `pc-app/`: Windows app for collecting and transmitting monitoring data.
- `pc-app/installer/`: Windows installer files.

Built with PlatformIO, .NET, LibreHardwareMonitor, and PresentMon.
