#pragma once
#include <cstring>

enum class DisplayLayout {
  Classic, Gauges, Minimal, Orbit, Radar, NeonPulse, Reactor,
  Waveform, Matrix, Starfield, Speedometer, Aurora, HexGrid
};

inline bool isAnimatedLayout(DisplayLayout layout) {
  return layout >= DisplayLayout::Orbit;
}

inline bool parseLayout(const char *name, DisplayLayout &layout) {
  if (strcmp(name, "classic") == 0) layout = DisplayLayout::Classic;
  else if (strcmp(name, "gauges") == 0) layout = DisplayLayout::Gauges;
  else if (strcmp(name, "minimal") == 0) layout = DisplayLayout::Minimal;
  else if (strcmp(name, "orbit") == 0) layout = DisplayLayout::Orbit;
  else if (strcmp(name, "radar") == 0) layout = DisplayLayout::Radar;
  else if (strcmp(name, "neon-pulse") == 0) layout = DisplayLayout::NeonPulse;
  else if (strcmp(name, "reactor") == 0) layout = DisplayLayout::Reactor;
  else if (strcmp(name, "waveform") == 0) layout = DisplayLayout::Waveform;
  else if (strcmp(name, "matrix") == 0) layout = DisplayLayout::Matrix;
  else if (strcmp(name, "starfield") == 0) layout = DisplayLayout::Starfield;
  else if (strcmp(name, "speedometer") == 0) layout = DisplayLayout::Speedometer;
  else if (strcmp(name, "aurora") == 0) layout = DisplayLayout::Aurora;
  else if (strcmp(name, "hex-grid") == 0) layout = DisplayLayout::HexGrid;
  else return false;
  return true;
}
