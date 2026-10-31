#pragma once
#include <cstring>

enum class DisplayLayout { Classic, Gauges, Minimal };

inline bool parseLayout(const char *name, DisplayLayout &layout) {
  if (strcmp(name, "classic") == 0) layout = DisplayLayout::Classic;
  else if (strcmp(name, "gauges") == 0) layout = DisplayLayout::Gauges;
  else if (strcmp(name, "minimal") == 0) layout = DisplayLayout::Minimal;
  else return false;
  return true;
}
