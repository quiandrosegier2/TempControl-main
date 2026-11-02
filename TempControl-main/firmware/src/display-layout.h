#pragma once
#include <cstring>

enum class DisplayLayout {
  Classic, Gauges, Minimal, Orbit, Radar, NeonPulse, Reactor,
  Waveform, Matrix, Starfield, Speedometer, Aurora, HexGrid,
  Arsenal, BulletStorm, Inferno, WarzoneCity, SniperScope, CyberOps, PlasmaCore, BossFight, DroneSwarm, LightningStrike, SpaceFleet, RaceGrid, PortalRift, TacticalSteel, AmmoVault, SamuraiCrest, RetroArcade, StealthMap
};

inline bool isAnimatedLayout(DisplayLayout layout) {
  return layout >= DisplayLayout::Orbit && layout < DisplayLayout::TacticalSteel;
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
  else if (strcmp(name, "arsenal") == 0) layout = DisplayLayout::Arsenal;
  else if (strcmp(name, "bullet-storm") == 0) layout = DisplayLayout::BulletStorm;
  else if (strcmp(name, "inferno") == 0) layout = DisplayLayout::Inferno;
  else if (strcmp(name, "warzone-city") == 0) layout = DisplayLayout::WarzoneCity;
  else if (strcmp(name, "sniper-scope") == 0) layout = DisplayLayout::SniperScope;
  else if (strcmp(name, "cyber-ops") == 0) layout = DisplayLayout::CyberOps;
  else if (strcmp(name, "plasma-core") == 0) layout = DisplayLayout::PlasmaCore;
  else if (strcmp(name, "boss-fight") == 0) layout = DisplayLayout::BossFight;
  else if (strcmp(name, "drone-swarm") == 0) layout = DisplayLayout::DroneSwarm;
  else if (strcmp(name, "lightning-strike") == 0) layout = DisplayLayout::LightningStrike;
  else if (strcmp(name, "space-fleet") == 0) layout = DisplayLayout::SpaceFleet;
  else if (strcmp(name, "race-grid") == 0) layout = DisplayLayout::RaceGrid;
  else if (strcmp(name, "portal-rift") == 0) layout = DisplayLayout::PortalRift;
  else if (strcmp(name, "tactical-steel") == 0) layout = DisplayLayout::TacticalSteel;
  else if (strcmp(name, "ammo-vault") == 0) layout = DisplayLayout::AmmoVault;
  else if (strcmp(name, "samurai-crest") == 0) layout = DisplayLayout::SamuraiCrest;
  else if (strcmp(name, "retro-arcade") == 0) layout = DisplayLayout::RetroArcade;
  else if (strcmp(name, "stealth-map") == 0) layout = DisplayLayout::StealthMap;
  else return false;
  return true;
}
