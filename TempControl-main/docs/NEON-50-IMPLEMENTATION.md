# QPRINTS Neon 50 implementation

The supplied 50-layout poster is the visual reference, not a source of production backgrounds. Login and further account work remain deferred by the user's explicit instruction. Existing backend authorization is unchanged; this work embeds an application-shipped catalog and does not expose cloud publishing.

## Reused components

- WinForms `StudioShellWindow`: gallery is the home page; preview and designer are internal pages.
- `ScreenDesign` / `ScreenWidget`: portable editable documents, existing sensor bindings, backgrounds, fonts and color rules.
- `DesignRenderer` / `PremiumDrawing`: 412 × 412 circular rendering and bounded procedural animation.
- `ProfileRepository`: fresh personal copies, no overwrite of shipped templates or user projects.
- `AdvancedMonitor` / `ScreenDelivery`: existing FRAME2 USB transfer and device acknowledgements. No firmware change required.
- `QscreenPackage`: portable artwork + editable widgets and thumbnails.

## Implementation sequence

1. Preserve the original 21-template catalog and its stable identifiers. Add a separate numbered Neon 50 collection with independent stable identifiers and a collection filter.
2. Create original decorative artwork per scene. Keep every clock, sensor, caption, meter and progress bar as a separate widget. Embed production-sized assets and retain generation prompts/provenance.
3. Build batches 01–10, 11–20, 21–30, 31–40 and 41–50. Use distinct compositions, palettes and artwork matching the poster.
4. Validate each batch: identity/count, editable bounds and sensor bindings, unavailable states, serialization, package portability, immutable personal copies, animation/static fallback, 412 rendering and image/memory limits. Produce contact sheets for visual review.
5. Integrate the complete collection into the gallery with search, category, favorites and animated/static filters. Keep existing 21 designs accessible.
6. Verify internal navigation and editor preservation, existing transport regression tests, actual acknowledgement feedback and installer payload. Build a reviewable installer.

## Physical verification boundary

Rendering and transport simulation are software checks. They do not prove the physical Waveshare panel's appearance, USB throughput or frame rate. Do not label physical tests as passed without an actual connected-device test. Do not flash firmware automatically.

## Implemented and verified

All five batches and the complete 50-template catalog pass software validation. The collection contains 30 animated and 20 static designs. All 21 existing originals remain available. Gallery filters, internal preview/editor navigation, pending unsaved edits, personal copy isolation, portable artwork, animation/static rendering, existing profiles/worker reconnection and FRAME2 protocol regressions pass. Windows first-render median was approximately 3.6 ms on the development machine, excluding physical transfer. Production backgrounds total approximately 13.3 MB including the reused tactical illustration. No new illustration is missing. COM3 was detected, but the new 50 layouts have not been physically cycled on the panel.
