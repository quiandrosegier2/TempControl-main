# Qscreen Studio — Premium 100 / Expansion Pack 2

Version **2.4.0-rc.1** adds layouts **51–100** to the existing Neon 01–50 collection: **38 animated and 12 static additions**, for **68 animated and 32 static premium layouts** in total. The home gallery displays all 100. The original 21 Studio Originals and 31 firmware layouts are preserved. Sign-in is optional; account/website work remains deferred.

Only the text brief for Expansion Pack 2 was received. No poster for 51–100 was attached or found with the earlier desktop poster. The new original illustrations follow the written themes; a comparison to an unseen poster is not claimed.

## Collection and editing

| Batch | New layouts |
|---|---|
| 51–60 | Dragon Flame; Samurai Spirit; Kawaii Cat; Cherry Blossom; Neon Tokyo; Pirate Anime; Monster Capsule; Saiyan Energy; Ninja Shadow; Forest Spirit |
| 61–70 | Moon Guardian; Neon Ronin; Kawaii Bunny; Demon Mask; Ramen Time; Kitsune Fox; Anime Swordsman; Happy Anime; Neon Panda; Koi Pond |
| 71–80 | Horror Night; Zombie Mode; Halloween; Vampire Night; Ghost Rider; Cyber Skull; Biohazard; Secret Laboratory; Alien Invasion; Desert Mystery |
| 81–90 | Stealth Assassin; Viking Storm; Gladiator; Pirate Ship; Wild West; Knight Armor; Egyptian Gold; Ancient Temple; Jungle Explorer; Dino World |
| 91–100 | Underwater Reef; Shark Attack; Frozen Ice; Volcano Core; Desert Mirage; Lightning Storm; Earth From Space; Black Hole; Quantum Core; RGB Dream |

Every expansion layout includes separate editable clock, date, CPU/GPU temperature, CPU/GPU load bars, RAM, FPS and decorative objects. The illustrations are backgrounds; replacing one preserves the widgets. Existing fonts, colors, positions, sizing, import/export, custom images/animations, personal copies and device application remain available.

47 original scene illustrations are optimized to 412 × 412. Biohazard, Quantum Core and RGB Dream use native graphics. The infinity ribbon and pulsing katana glow are separate editable widgets. New bounded effects include petals, hearts, lightning, spectral fire and water ripples; existing smoke, stars, snow, bubbles and embers are reused. Disable animations for a static alternative.

Use **Premium 100**, **Neon 01–50**, **Expansion 51–100**, **Studio Originals · 21**, local designs or all collections. Search/category/favorite/animated/static filters and cached circular thumbnails are retained. The pink **+ Eigen ontwerp** action is anchored at the top right. Gallery, large preview and designer use the existing single main window.

Back navigation retains unsaved work. Use **Meer → Verder met open ontwerp** to return to that same editor from the home gallery. Creating/opening another design or closing protects unsaved changes.

## Shared live readings

The background monitor samples hardware once and atomically writes a small local snapshot, `live-telemetry.json`, without history or account data. PC Monitor, the designer and the large preview read those same sensor values. The physical frame worker receives the same central sample through the existing monitoring pipeline.

The editor and large preview default to **LIVE**. **Demowaarden** is an explicit, labeled option; gallery thumbnails still use labeled sample values. A missing, stale, future-dated, corrupt or non-finite measurement becomes unavailable, rather than silently falling back to sample readings. Without a recent snapshot, start the background measurement using the updated installed version. An older monitor does not produce the new snapshot.

Pausing a large preview stops decorative animation while its clock and live values continue updating. Static previews refresh at a lower rate; animation and interpolating gauges use a bounded faster timer. No second hardware sampler is started by the UI.

## Compatibility and permissions

The existing acknowledged FRAME2 USB protocol and firmware are unchanged. New graphics are rendered on the PC. No automatic firmware flashing occurs. Touch/brightness support still depends on the installed firmware's capabilities.

All existing shipped identifiers and personal designs are preserved. Edits create/save personal copies. Local author strings or copied IDs do not make a template an official shipped entry. Existing backend authorization is unchanged; no cloud publishing endpoint, administrator provisioning or mandatory login was added in this release.

## Verification

Each batch is checked for complete required bindings, unique identifiers/appearances, 412-pixel artwork, circular clipping, editable bounds, serialization, package portability, copy isolation and moving/static rendering. Contact sheets are visually reviewed. Integration checks cover all 100 premium entries plus 21 originals, search/collection/category/favorite filters, internal editor/preview navigation and unsaved work. Central snapshot tests verify consistent real-mode values, explicit demo mode, stale/corrupt/non-finite data handling and updates while a preview is paused.

These checks are software tests. **The 50 new layouts have not been physically cycled through or visually verified on the ESP32.** Renderer timing and animation flags do not establish physical display frame rate. No comparison to the missing reference poster is claimed.

Source: `pc-app/ExpansionTemplates.cs`, `ExpansionArtwork.cs`, `OfficialTemplates.cs`, `LiveTelemetry.cs`. Artwork and final generation prompts: `pc-app/assets/expansion50/`. Plan: `docs/EXPANSION-100-IMPLEMENTATION.md`.
