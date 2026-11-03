# Qscreen Studio — Neon 50 preview

Version: **2.3.0-rc.1**. No mandatory login. Account/website development remains deferred; existing backend authorization is unchanged. This release ships an embedded catalog and does not introduce cloud publishing.

The home gallery displays **50 new numbered designs: 30 animated and 20 static**, in the order of the supplied poster. Use the collection selector for **Studio Originals · 21**, local designs or all collections. The original stable template identifiers, personal profiles and 31 firmware layouts remain intact.

- Search, category, favorites and animated/static filters; cached 190-pixel thumbnails.
- Internal previews and editable personal copies; originals are never overwritten by the editor.
- Separate live clock, date, FPS, temperatures, load, RAM, gauges and progress bars. No production sample readings are stored in the artwork.
- New original scene/character illustrations, optimized to 412 × 412. Weather uses the existing optional provider and shows unavailable states when not configured.
- Full rainbow ring; animated bubbles, embers, steam, scanlines and snow; existing stars, waves and aurora. Disable animations in the editor for a static alternative.
- Angry CPU's separate face widget responds to CPU temperature; neutral unavailable state. Nixie clock digits are independently editable.
- The supplied poster's QPRINTS mark is used for Signature, Circuit Neon and the app header. No replacement brand was invented.
- Back navigation retains the editor, including pending name edits. Opening another design or closing asks before unsaved changes are discarded. Replaced editors dispose their timers, images and renderers.
- Apply uses the existing USB path and waits for fresh revision-specific device acknowledgement before reporting success.

## Collection

| Batch | Templates |
|---|---|
| 01–10 | Tactical Ops; Neon Racer; Cyberpunk City; Minimal Pro; RGB Pulse; Frost Glass; Space Explorer; Retro Pixel; Anime Style; System Rings |
| 11–20 | Matrix Code; Aurora; Weather + System; Car Dashboard; Hardware Pro; Game HUD; Lava Monster; Ocean Depths; Forest Nature; Zen Minimal |
| 21–30 | Skull Fury; Robot Buddy; Cat Gamer; Panda Chill; Doge Vibes; Angry CPU; Coffee Mode; Pizza Power; Sleep Mode; Toilet Break |
| 31–40 | Rocket Launch; Moon Base; Mecha Core; Hologram AI; Hacker Terminal; Vintage Nixie; Steampunk; Neon Skull; Liquid RGB; Geometry |
| 41–50 | Minecraft Vibes; Survival Camp; Racing HUD; Winter Snow; Summer Beach; Galaxy Core; Energy Core; Circuit Neon; Abstract Waves; QPRINTS Signature |

Sleep Mode and Toilet Break may be assigned to existing idle rules; installing this release does not silently add rules. Animation uses the existing PC-rendered frame protocol. No firmware changes or automatic flashing are required by these templates. Existing firmware support still determines touch/brightness availability.

## Validation and limits

Each batch and the complete catalog are checked for identity, distinct 412-pixel renderings, circular clipping, editable widget bounds, live/unavailable sensor output, serialization, portable packages, personal copy isolation, animation movement and static fallback. Contact sheets are visually reviewed. Existing renderer/editor, original template, profile/worker/reconnect and legacy preview tests are retained.

The gallery integration checks 50 default cards, all 71 shipped templates, search, categories, animated/static filters and access to the originals. Shell checks cover internal preview/editor, back navigation, unsaved edits and personal copy isolation.

The connected screen was detected on COM3 using legacy firmware capabilities. **These 50 templates have not been physically cycled through or visually verified on that ESP32.** Frame acknowledgement protocol regression tests are simulated. Measured Windows render timings do not establish physical display frame rate. No device firmware was flashed.

Code and assets: `pc-app/NeonTemplates.cs`, `pc-app/NeonArtwork.cs`, `pc-app/assets/neon50/`. Generation prompts/provenance are included with the assets. The implementation plan is in `docs/NEON-50-IMPLEMENTATION.md`.
