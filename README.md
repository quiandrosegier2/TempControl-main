# Qscreen

**Qscreen Studio 2.1 preview:** a [library of 21 editable premium templates](TempControl-main/PREMIUM-TEMPLATES.md), circular gauges, analog clocks, per-widget color rules, opacity, animation controls, favorites and an opt-in real weather provider. See the [2.1 prerelease](https://github.com/quiandrosegier2/TempControl-main/releases/tag/v2.1.0-rc.1). New template rendering has software tests; physical display validation is pending.

**New public test version: Qscreen Studio 2.0.0-rc.3.** Adds automatic profiles, multipage designs, touchscreen navigation, sensor history and graph widgets, a local DesignHub, brightness schedules, configurable alerts and independent multiple displays. See the [2.0 guide and hardware test limits](TempControl-main/QSCREEN-2.0-GUIDE.md). The [stable 1.6.0 release](https://github.com/quiandrosegier2/TempControl-main/releases/tag/v1.6.0) remains available; [2.0 release candidate](https://github.com/quiandrosegier2/TempControl-main/releases/tag/v2.0.0-rc.3) is a prerelease. Touch and brightness require the bundled 2.0 firmware; upload it only to the explicitly selected compatible display.

A compact PC system monitor featuring a round Waveshare ESP32-S3-Touch-LCD-1.46 display. View your CPU temperature, GPU temperature, and FPS on a dedicated screen connected via USB-C.

### Features
- One Windows installer for the companion app and USB display firmware; no VS Code, Python, PlatformIO or .NET installation needed by end users.
- **Qscreen** features a dark wizard and firmware window, with the QSCREEN // logo visible throughout setup.
- Live CPU temperature, GPU temperature, and FPS readings.
- Blue values and rings at or below 50 °C, turning red above 50 °C.
- Windows companion app that sends monitoring data to the display.
- Waiting indicator when no PC data is received.
- Choose from 31 layouts: 23 animated and 8 static designs. Moving offline previews with Play/Pause are available in Setup and the installed **Layout Studio**. The display remembers the selection.
- Open **Qscreen** from the desktop or Start menu at any time to preview and apply layouts, upload firmware or open monitoring logs. Closing Layout Studio keeps background monitoring running.

### Project structure
- `TempControl-main/firmware/`: firmware for the ESP32-S3 display.
- `TempControl-main/pc-app/`: Windows app for collecting and transmitting monitoring data.
- `TempControl-main/pc-app/installer/`: Windows installer files.

### Installing

Download the setup executable from [GitHub Releases](https://github.com/quiandrosegier2/TempControl-main/releases/latest).

1. Run `Qscreen-1.6.0.exe` on a Windows x64 PC and allow administrator access. Choose a layout and watch its demo on the layout page; use **Pauzeren/Afspelen** to pause or play.
2. Connect a **Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB)** using a USB-C data cable.
3. On a first installation or when firmware is outdated, select the firmware installation option at the end of Setup. UI-only updates do not require flashing.
4. Select the display's COM port and click **Firmware installeren**. This replaces any existing firmware on that display.
5. Keep the cable connected until the upload finishes. Press RESET or reconnect the display if needed.

If no port appears or connecting fails, hold BOOT, press RESET, release BOOT and click **Vernieuwen**. Select the new COM port and retry. Only Espressif USB devices are listed; verify you selected the supported Waveshare display.

You can repeat the upload from **Start > Qscreen tools > Firmware installeren**. The monitoring app pauses during the upload and resumes afterwards. Upload logs are saved in `C:\ProgramData\TempMonitor`.

The app, .NET runtime, firmware, uploader and optional PawnIO driver are included. No internet connection or winget is needed during installation. Windows may request a restart after installing the driver. Developer build instructions are in [the project guide](TempControl-main/Readme.md).

To change layouts later, open **Qscreen** from the desktop or Start menu, select a layout and click **Layout toepassen**. The monitor picks up the selection within its next one-second update; no restart is needed. Upload the bundled **1.5.1 firmware** once to enable all 31 designs. Further layout changes require no flashing. Updates preserve the current layout selection. Layout Studio can also be opened directly with `Qscreen.exe --settings`.

### Qscreen Design Studio — version 1.5.0

Open **Eigen ontwerp maken…** in Qscreen to create a custom design for the round 412 × 412 display:

- Import a PNG, JPG or BMP background, or drag it onto the canvas. Choose Cover, Contain or Stretch and adjust darkening or the background color.
- Add and drag text, CPU/GPU temperatures, FPS, clock, date, CPU/GPU load, memory usage, rectangles, ellipses and progress bars.
- Change each element's position, width, height, color, font, font size, bold, italic and alignment. **Lettertype…** opens the Windows font picker. Turn off **Temperatuurkleuren** to use your own temperature text color.
- Use `{value}` in sensor text, `HH:mm:ss` for a clock or `dd-MM-yyyy` for a date. Missing sensor readings display `--`.
- Duplicate, remove and reorder elements. Undo/redo supports edits and dragging; Ctrl+S saves a draft, Ctrl+Z/Ctrl+Y undo and redo.
- Save drafts, export portable `.qscreen` files including their background, and import them later. Export files to keep multiple designs or share them; fonts must be installed on the receiving Windows PC or a fallback font is used.
- Click **Toepassen op scherm** to activate the design. Applying a preset switches back to the existing firmware themes.

Custom designs require the bundled **1.5.1 firmware**. Windows renders the chosen fonts and images and sends complete, checksum-verified frames over USB. The PC app and USB connection must remain active; the refresh interval includes image transfer time and is intended for sensor/clock updates, not video playback. After loss of PC data the display returns to its built-in offline view. Designs and backgrounds are stored on the PC, rather than written to the screen's flash every second. Editing and previewing designs works without a connected display.

Double-clicking **Qscreen.exe** opens the app. The scheduled background task uses `Qscreen.exe --monitor`; `--settings`, `--port COM5` and `--list` remain available. Upgrading keeps the existing installation directory and task identity, and replaces the old launcher shortcuts with Qscreen shortcuts.

Validation includes schema and image validation, all twelve widget renderers, RGB565 byte order and checksum vectors, chunked transfer, old-firmware compatibility, designer dragging/undo/redo and configuration writes. The new USB frame path has been built and tested with a simulated connection; it has not yet been validated on a physical display.

### Preset layouts

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
| Drone Swarm | Moving quadcopter formations |
| Lightning Strike | Animated red lightning bolts |
| Space Fleet | Moving spaceships with engine trails |
| Race Grid | Scrolling perspective racing grid |
| Portal Rift | Rotating, rippling portal fragments |
| Tactical Steel | Static steel panel with rivets |
| Ammo Vault | Static ammunition display |
| Samurai Crest | Static helmet crest and crossed blades |
| Retro Arcade | Static pixel tiles and arcade frame |
| Stealth Map | Static tactical map and crosshair |

Animations run independently of incoming readings. Waveform retains up to 60 actual temperature samples in memory; unavailable readings remain gaps. Setup and Layout Studio include four-second looping, schematic demos with sample values for all 23 moving themes. Eight static designs have still previews; their real sensor readings continue to update. No screen or internet connection is needed to view the previews. Temperature values turn red above 50 °C in every layout.

[View all layouts](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.5.0/Layout-Overview-1.5.0.png) or [the eight gaming themes](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.3.0/Gaming-Layouts-1.3.0.png).

Gaming artwork is original and generated in code. Weapons, bullets, fire and enemy sprites are decorative animations; FPS, CPU/GPU temperatures and temperature bars use real incoming measurements. Upload the 1.3.0 firmware once to enable the new gaming themes.

Speedometer needles smoothly follow their own live readings; the digital numbers show the actual values. Missing readings hide the relevant needle. Values beyond a dial's range pin its needle at the scale limit while retaining the actual digital reading. Upload the 1.2.2 firmware once to get the three-dial Speedometer design.

Built with PlatformIO, .NET, LibreHardwareMonitor, and PresentMon.

### Applying a custom design (1.5.1)

Apply saves the design, starts the scheduled background monitor and waits for a fresh acknowledgment for that exact design. Green success appears only after the screen returns FRAMEOK. Missing USB, incompatible firmware and missing confirmation are displayed explicitly. Use Firmware bijwerken in the designer to install the bundled 1.5.1 firmware; updating only the Windows app does not update the screen. The revised protocol waits for FRAMECHUNK after every 1024 bytes and uses a 4096-byte receiver queue to prevent large images overflowing USB reception. Existing designs remain saved during upgrades.

### Qscreen 1.6 - refreshed workspace and smoother editing

The desktop app and designer share a new dark theme, rounded controls, a workspace sidebar, a circular canvas stage, clearer actions and live connection status. The inspector shows only properties relevant to the selected widget. Windows fit within the available desktop working area.

App previews are now full 412x412 PNG images. The 920 installed animation files total 16,563,874 bytes instead of 178,161,680 bytes for the former BMP library, a 90.7% reduction. One animation cycle is cached in memory (at most 40 images); cached frames do not reopen files. Custom previews update once per second and hidden/minimized previews stop rendering.

Undo/redo shares immutable background strings and stores only element metadata per edit. The renderer reuses a bounded cache of fonts; dragging is coalesced to a 33 ms UI timer, while selection changes redraw only selection guides.

This release retains firmware 1.5.1 and its working acknowledged USB protocol. Existing custom designs and backgrounds remain compatible. Firmware upload is optional and unchecked by default; users already on firmware 1.5.1 do not need to flash again. Tests cover image caching and bounds, shared-image undo with isolated widgets, font reuse, contextual properties, UI editing, sharp preview assets and installer startup.
