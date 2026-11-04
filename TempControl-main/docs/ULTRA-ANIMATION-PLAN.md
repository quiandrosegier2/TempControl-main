# Ultra Animation — staged implementation

The source contains 100 premium layouts plus 21 Studio Originals, not a verified 200-layout catalog. Preserve them and distinguish procedural effects from imported character/video animation.

## Hardware audit

Configured board: Waveshare ESP32-S3-Touch-LCD-1.46, SPD2010 412 × 412 over QSPI, 16 MB flash and 8 MB OPI PSRAM. This agrees with the manufacturer documentation: https://docs.waveshare.com/ESP32-S3-Touch-LCD-1.46 . Runtime free heap/PSRAM and device rendering FPS are not exposed by the connected legacy protocol; they must remain unknown rather than fabricated.

Existing firmware uses `ps_malloc(412*412*2)` for the incoming custom frame, alongside the Arduino canvas. FRAME2 validates the byte count and FNV checksum, acknowledges 1024-byte chunks and returns FRAMEOK after drawing/flushing. It does not persist animation assets in flash. The first pipeline therefore decodes/composites on Windows and streams RGB565. No partition changes or firmware flashing are needed for this mode; the PC must remain connected.

## Ordered gates

1. **GIF first:** bounded binary metadata inspection before native decoding; source timing/transparency; existing circular composition and live widgets; adaptive ACK-based pacing; progress only after actual acknowledgements; private content-hash source cache; software regression tests and a genuine moving/blinking/waving character fixture.
2. **Physical gate:** isolated portable monitor, actual GIF frame indices and acknowledged frame hashes, measured render/transfer time and throughput, concurrent sensor snapshots, then restoration of the installed monitor. Record measured results, not requested preset FPS. Hardware rendering FPS/memory remain unknown unless a future capability provides them.
3. After that gate succeeds, extend the import pipeline to WebP/video/APNG/sequences/sprite sheets with bounded Windows decoding, safe process arguments, license notices and no execution of imported content.
4. Add portable multi-layer assets, timeline, seek/loop, transforms/keyframes, cache limits and reactive rules with hysteresis/cooldown; preserve all existing saved projects.
5. Add personal asset-manager UI, compatibility/storage/performance metadata, preview controls and administrator-only shared publication through authenticated backend checks. Mandatory login and unfinished account infrastructure are not silently enabled.
6. Verify reconnect, cancellation, long-running playback, source data liveness and actual hardware before marking a template's genuine frame-based animation as device verified.

Presets are targets: Eco 6, Balanced 12, Smooth 20; Maximum starts conservatively and is capped by measured work/transport cost. The present full-frame mode has a 339,488-byte RGB565 payload per changed frame and no device-side asset storage. Further optimizer decisions must use the physical benchmark.


## Implemented in 2.5.0-rc.1 (2026-11-04)

- GIF sniffing, source timing/transparency and real Windows decoding; GIF-to-USB physically verified before extending formats.
- Motion Studio is available from the designer and More menu. It edits a copy until Save changes. Existing editor undo includes all layers without copying immutable source images into each undo record.
- Up to 12 independent art layers below the live widgets, with visibility/lock/order, position/size, opacity, speed, loop, start delay, fade-in/out, rotation, scale and interpolated keyframes. Timeline play/pause/stop/seek/loop and editable keyframe table; safe guides, sensors and performance toggles. Preview can follow fresh measured device speed; absent metrics are explicitly unknown.
- Windows FFmpeg conversion tested with actual MP4, WebM, animated WebP and APNG fixtures. Explicit clip duration/rate, at most 300 frames, 256 MB input, local file/pipe protocols only, fixed format allowlist, bounded logs/allocation request, timeout and cancellation. No imported content chooses executable or command arguments.
- Native `.qanim` is a validated ZIP containing animation.json and numbered PNG frames. Maximum 24 MB encoded asset, 300 frames, 412 pixels per dimension, checked PNG headers and bounded expanded size. Decoder retains one decoded frame per active asset. PNG sequences and transparent sprite sheets supported; oversized sequence frames are resized on Windows.
- User-local animation cache/library supports reuse, rename, folders, move and recoverable deletion to `.trash`. This does not publish anything to a shared service. Mandatory accounts and unfinished official shared publication remain deferred by the user's earlier instruction.
- Configurable sensor rules: threshold, hysteresis, cooldown, active speed/opacity, smoothed transitions, and unavailable-sensor fallback. Sensor acquisition remains independent of each USB worker.
- Optional negotiated PATCH1 dirty rectangles, payload FNV and final full-frame FNV. Firmware validates/stages the complete patch before replacing the last valid image. FRAME2 and the exact old CAPS response are retained; old clients remain supported. A rejected patch invalidates the host baseline and reconnect sends a full frame.
- STATS exposes actual free heap/PSRAM, total PSRAM/flash, completed draw counts and draw duration. The reported rate is completed images per measured interval, not LCD scan frequency. No temperature is invented.
- Firmware upgrade helper backs up full flash, checks the actual partition/OTA selection and writes only app0. Recovery uses the original app0 image from that backup. The installer flasher now applies equivalent checks; full factory initialization is allowed only for verified entirely erased flash after an explicit checkbox.

## Physical results

All figures below are measurements on COM3, not preset promises. Acknowledgements are emitted after drawing/flushing. No camera/manual visual inspection was recorded.

| Mode | Result |
| --- | --- |
| Legacy full RGB565 | 14 ACKs; ~0.81 images/s; ~1,150 ms/frame transfer; 18 live sensor snapshots |
| Firmware 2.5 PATCH1, enlarged GIF | Two-minute run passed; about 2 completed images/s; source frames and changing hashes confirmed |
| Firmware 2.5 PATCH1, 220px transparent character layer | 186 ACKs; 4.31 images/s mean after connection, last device interval 4.99 images/s; all 12 source frames; 19 distinct composite hashes; 39 live sensor snapshots |
| Character layer latest payload | 27,234 bytes; 180 ms transfer; 2.14 ms Windows render; 63.99 ms device draw |
| Actual device memory | 16,777,216-byte flash; 8,388,608-byte PSRAM; 7,012,960-byte PSRAM free; 328,512-byte heap free |

The approved firmware update was written and verified at 0x10000. Original partition table, boot selection and user data were preserved; the full recovery backup remains local in installer/tool-cache/motion-firmware-backup and must never be committed. Each physical test restored the original scheduled monitoring task.

A USB reconnection while a FRAME2 packet is incomplete may require its existing 10-second receiver timeout and a subsequent retry. The test harness excludes stale telemetry/runtime metrics. Manual cable removal, camera inspection, all third-party codecs, hour-long endurance and a full recovery write remain unverified. Do not label arbitrary imported assets or the complete catalog as device-verified animations.

## Using Motion Studio

1. Create/edit a personal design, then choose Motion Studio (also available under More).
2. Import GIF/PNG, convert a chosen video/WebP/APNG clip, import a PNG sequence or cut a sprite grid. The original Ghost Cute example is included.
3. Choose a layer and set its size/position and duration. Place keyframes at different times; the keyframe table allows exact editing.
4. Keep live clock/CPU/GPU/FPS widgets in the designer above the art. Save Motion Studio changes, then save/apply the design normally.
5. Keep the PC connected over USB. This release streams composites; it does not store video files in ESP flash. Performance depends on changed area and source content.

## Recovery

The development helper `tests/Update-MotionFirmware.ps1 -Restore -ExpectedBackupSha <reviewed full-backup SHA-256>` restores only the original app0, with the same partition guards and read-back verification. It requires Windows administrator rights and a retained full backup; it is not run automatically. Public release publication and forced accounts are not part of this test build.
