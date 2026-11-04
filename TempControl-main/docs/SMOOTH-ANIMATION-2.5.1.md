# Qscreen 2.5.1-rc.1 animation update

This installer includes the Windows pipeline changes. Firmware remains 2.5.0-rc.1; further partial-display/burst-USB optimizations have not been implemented or included. Other displays are deferred at the user's request.

- Dedicated compositor task, separate from USB transport and sensor sampling.
- One pending completed frame; newer frames replace outdated ones, with disposal and a real replacement counter.
- Stopwatch-based playback and absolute render deadlines. Sensor updates retain the playback origin; wall-clock changes do not change the animation timeline. Clock/date widgets continue to use actual wall time.
- Source frame durations and existing project formats remain supported. Maximum remains a compatible legacy preset; Ultra adds the new 30 FPS target. Eco/Balanced/Smooth target 10/15/25 FPS. Targets are not guarantees.
- Static compositions stop repainting until another update arrives; native/sleep modes pause the compositor.
- Diagnostics include ACK rate, lowest observed interval rate, mean/P95 frame interval, interval standard deviation, decode/render/USB time and replaced queued frames. Recommended preset uses measured work and can explicitly report operation below Eco's target.

Validation: Motion, Design and Advanced regression suites pass. Motion tests cover monotonic playback across a sensor update and backward sensor timestamp, bounded replacement under a slow consumer, and shutdown. USB protocols, independent-device tests and project/page/template persistence pass.

Physical COM3 check: latest app with existing 2.5.0-rc.1 firmware; 153 acknowledged images, 14 distinct composite hashes and 44 live sensor snapshots in a 60-second test including startup/reconnection. The original monitoring task resumed afterward. No firmware was flashed. This confirms functioning playback and telemetry, not a claimed speedup: installer compression was running concurrently, and the earlier six-case baseline run was interrupted before completion. A controlled before/after comparison and further firmware optimization remain outstanding.
