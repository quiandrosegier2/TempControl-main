# Qscreen 2.5.2-rc.2 — USB and display changes

Scope: the connected Waveshare ESP32-S3 1.46-inch SPD2010 display only. Other displays remain deferred.

## Changes included

- Negotiated 8192-byte USB windows with a 16 KB receiver buffer. New FRAME3/PATCH2 commands leave FRAME2/PATCH1 and the exact legacy CAPS response intact.
- Optional lossless RGB565 run/literal compression (RLEF/RLEP), selected only when it saves at least 10%. Both the encoded packet and complete reconstructed image are checked before display.
- Dirty-rectangle display writes for small changes, retaining the verified Canvas rotation-3 mapping. Large changes use the full-frame path. Rotation is copied in tiles instead of going through individual virtual drawing calls.
- Existing QSPI driver/DMA and 40 MHz bus settings remain unchanged. No speculative clock increase or new display driver is used.
- Compose and physical display-transfer timings added to actual device statistics. Replacement counters are relative to each selected design; GIF metadata now includes source FPS.
- Longer initial INFO timeout prevents a slow display boot from being prematurely identified as legacy firmware.
- The final rc.2 replaces Arduino HWCDC's per-byte FreeRTOS RX queue with the ESP-IDF USB Serial/JTAG buffered block API. The hardware USB interface, pins, descriptors and command protocol remain the same. This driver is isolated in bulk-usb.h.

## Verification and limits

Software tests cover legacy transfers, negotiated 8 KB transfers, compressed frame reconstruction, randomized run/literal round trips, final checksums, rejection recovery, old project formats, page/undo/package persistence and the monotonic compositor. Firmware compiles with the installed ESP-IDF/Arduino toolchain.

The first firmware candidate (rc.1, before the final USB block-driver replacement) completed all six 45-second physical cases. Approximate ACK rates: small character 8.35 FPS, full background 4.13 FPS, background plus live widgets 3.73 FPS, multiple sprites 9.05 FPS, moving loop 14.50 FPS. The static layout also passed. All animated source frames remain governed by their actual durations; the small character source itself is only 8.33 FPS.

In that rc.1 run, the character update draw time fell from about 64 ms to 5.17 ms and its payload from roughly 26 KB to 6.8 KB. Full-screen movement remained limited. Sensor snapshots continued in every case, and free device memory was reported from the hardware. The original scheduled monitor was restored after the suite. These results do not constitute a controlled proof of visual smoothness for every imported GIF.

The final USB block-driver firmware (rc.2) was written and verified in app0 using the reviewed local full-flash recovery backup. The next Windows elevation prompt for the benchmark was canceled. The user then explicitly chose **build only, no further hardware test**. Therefore the rc.2 block-driver's performance and long-running playback have not been measured. Do not reuse rc.1 results as proof of rc.2 throughput or claim smooth full-screen playback is verified.

Only app0 was written. Bootloader, partition table and user data were preserved. Recovery backup remains local under installer/tool-cache/smooth-firmware-backup-252 and is excluded from Git. The new installer contains the rc.2 app and firmware; building it does not flash or install anything.
