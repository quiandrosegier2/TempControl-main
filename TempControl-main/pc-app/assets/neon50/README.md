# Neon 50 artwork

36 original scene/character backgrounds generated with the built-in image_gen tool through the imagegen skill. Final production PNGs are 412 × 412; all clocks, readings, labels, bars and meters are separate editable widgets. The final prompt for every generated asset and original generated filename are recorded in `provenance.json`. Originals remain in the tool's generated-images directory; the application only consumes assets embedded from this project.

`installer/prepare-neon-artwork.ps1` performs deterministic production resizing. Native graphics for black/metal/weather, Nixie tubes, HUD/reactor machinery, geometric outlines, racing decoration, circuits and branding are rendered by `NeonArtwork.cs`. Tactical Ops reuses the existing documented original `../tactical-background.png`.

`qprints-brand-reference.png` is the user's supplied QPRINTS poster. Only the header logo is sampled for branding; the poster's 50 dashboard screenshots are never used as production template backgrounds. Dark surrounding pixels are made transparent in the native logo renderer. All visible live values remain separate widgets. No new QPRINTS logo was generated.

No illustration assets are missing from this collection. Physical ESP32 visual verification remains outstanding; see `../../../NEON-50.md`.
