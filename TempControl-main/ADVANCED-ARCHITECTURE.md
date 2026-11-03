# Qscreen advanced update

The existing application remains a .NET 10 Windows Forms editor and background companion, using LibreHardwareMonitor, PresentMon and System.IO.Ports. The firmware remains PlatformIO/Arduino on the Waveshare ESP32-S3-Touch-LCD-1.46, Arduino_GFX SPD2010 QSPI display and GPIO 5 backlight. Existing built-in themes, FRAME2 checksums and 1024-byte acknowledged chunks remain compatible. The HTML file is a standalone legacy guide, not an application frontend or server.

Existing `.qscreen` files are JSON with embedded images. Optional page, touch and alert properties extend schema 1; old single-page projects remain readable. Profiles and app/device policies are separate JSON configuration, so automatic switching does not rewrite projects or flash firmware. Every import uses a new identifier. Package imports read a fixed allowlist in memory and never extract or execute files.

One Windows sensor sampler and bounded in-memory history feed independent, bounded per-device workers. Each worker owns exactly one COM connection, negotiates Qscreen identity/capabilities, selects its profile/page, evaluates brightness and alerts, and sends only current work. No worker shares a serial port. A monitor singleton prevents competing background instances. Stable USB identifiers are preferred to COM names; firmware identity improves identification. Unknown Espressif boards are not treated as compatible displays.

Long history and all pages/assets remain on Windows. The ESP32 retains only the current framebuffer and transfer buffer. Touch gestures are emitted as serial events and demultiplexed from FRAME2 acknowledgments. Brightness uses GPIO 5 PWM; no ambient sensor is assumed. Controls are capability-gated for old firmware.

Touch register handling follows the official Waveshare SPD2010 source at revision `fda89ff2ff32eb1bce561901225c6a36fa684237`, under `example/Arduino-3.1.1/examples/LVGL_Arduino/Touch_SPD2010.cpp`, with bounded I2C packet reads and bounded controller-state retries instead of unbounded loops. Source: https://github.com/waveshareteam/ESP32-S3-Touch-LCD-1.46

Validation must cover legacy projects and frame transfers, deterministic profile delays and overrides, page ordering/rotation/navigation, bounded sensor history and unavailable samples, package traversal/size rejection, overnight brightness schedules, alert hysteresis/cooldowns, independent device sessions and disconnect recovery. Hardware-dependent touch/backlight operation must be distinguished from simulated tests; no physical device is flashed automatically.
