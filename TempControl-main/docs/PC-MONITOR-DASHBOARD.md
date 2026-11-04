# PC Monitor dashboard — 2.6.0-rc.1

PC Monitor now opens a live dashboard instead of the old full-window temperature chart. History is accessible through More → Historie and reuses the same dashboard instance and preferences.

## Interface

- CPU, GPU, RAM and Gaming/FPS cards; responsive 4/2/1 columns, rounded charcoal cards, readable primary values, animated utilization bars and minute sparklines.
- Supported optional cards for uptime, active network adapter throughput, free disk space, fan RPM, motherboard/storage temperatures and CPU/GPU power. Power cards are hidden by default because power is already on the main cards.
- Kaarten toggles visibility. Right-click a card to change its order or hide it. Preferences are per Windows user, separate from device/profile settings.
- Configurable graphs for CPU/GPU temperatures and loads, RAM load, FPS and frame time; 1/5/15/30/60-minute windows. Compare overlays use identical units only. Current values use the live snapshot; min/max/average come from the selected history window. Missing samples break lines, rather than becoming zeros.
- Automatic axes, legends, hover timestamps/values, compact chart heights and scrolling. Per-monitor DPI awareness is enabled for the desktop UI. Measurements starten and the existing history sampling interval remain available.

## One measurement source

The desktop UI never opens LibreHardwareMonitor or PresentMon. It consumes live-telemetry.json and a bounded dashboard-history.json written by the existing background monitor, which still supplies the ESP displays. An optional HardwareTelemetry record extends the existing ScreenReadings contract; older project files are unchanged.

The existing Computer instance supplies temperatures, loads, core clocks, powers, memory and supported motherboard/storage/fan sensors. Windows metadata supplies installed RAM/configured speed; OS counters supply uptime, disk space and one explicitly named active network adapter. Optional collector failures do not replace valid core readings with fabricated data. The GPU model, load, clock and VRAM refer to the same selected GPU.

CPU clocks are converted from MHz to GHz. RAM/VRAM capacities follow the provider's Windows-style binary GB values; disk space is explicitly GiB and network throughput is decimal MB/s. RAM's configured speed is labeled as the reported WMI MHz value, with a tooltip; it is not doubled or relabeled as measured MT/s. Microsoft defines ConfiguredClockSpeed as a reported MHz field: https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-physicalmemory .

FPS remains tied to the presenting foreground process. Its average is weighted by actual observed frames and elapsed sample time over the recent window. Process identification is best effort; inability to read a process name does not interrupt temperatures or FPS. Frame time comes only from a recognized PresentMon millisecond column, never from a manufactured reciprocal of FPS. CSV quoted fields are supported. PresentMon's CSV definitions: https://github.com/GameTechDev/PresentMon/blob/v2.6.0/README-ConsoleApplication.md .

Absent readings display Niet beschikbaar; unsupported optional cards remain hidden. Snapshots older than five seconds are not treated as live. New detailed fields require the updated background monitor installed by the 2.6 installer. No firmware change is needed for this dashboard update.

## Validation

Dashboard tests use clearly labeled synthetic fixtures in an isolated workspace directory. They check shared snapshot values, CPU clock conversion, frame-time history, absence of fake FPS, unavailable optional sensors, stale data, 4/2/1 column layouts, CSV quoting, persistence of card ordering and old project loading. Screenshots at wide/medium/narrow sizes are test artifacts, not real PC readings. Existing Design and Advanced suites verify design editing, USB framing/fallback, independent workers, history, alerts and packages.

No additional physical-display benchmark, firmware flash or second sensor provider was started for this UI redesign. Hardware-specific availability and PresentMon/game capture remain dependent on the machine and its existing monitoring permissions.
