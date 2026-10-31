#include "../../firmware/src/dial-needle.h"

using Speedometer::Needle;
using Speedometer::angleFor;
using Speedometer::smoothReading;

static_assert(angleFor(0, 240) == 135, "FPS zero must point at the start");
static_assert(angleFor(120, 240) == 270, "FPS midpoint must point up");
static_assert(angleFor(240, 240) == 405, "FPS maximum must point at the end");
static_assert(angleFor(60, 120) == 270, "CPU/GPU midpoint uses their own temperature scale");
static_assert(angleFor(9999, 240) == 405 && angleFor(-10, 120) == 135, "Needles stay within the dial scale");
static_assert(smoothReading(40, 80, 250) == 60, "A reading change must ease toward the new value");
static_assert(smoothReading(40, 80, 0) == 40, "No time elapsed means no needle movement");

constexpr bool independentNeedles() {
  Needle fps, cpu, gpu;
  fps.update(165, 240, true, 1000);
  cpu.update(42, 120, true, 1000);
  gpu.update(58, 120, true, 1000);
  fps.update(210, 240, true, 1050);
  if (!(fps.value > 165 && fps.value < 210 && cpu.value == 42 && gpu.value == 58)) return false;
  cpu.update(80, 120, true, 1050);
  return cpu.value > 42 && cpu.value < 80 && gpu.value == 58;
}
static_assert(independentNeedles(), "FPS, CPU and GPU must never share needle state");

constexpr bool missingReadings() {
  Needle needle;
  needle.update(65, 120, true, 100);
  if (needle.update(std::numeric_limits<float>::quiet_NaN(), 120, true, 150) || needle.valid) return false;
  if (!needle.update(42, 120, true, 200) || needle.value != 42) return false;
  if (needle.update(42, 120, false, 250) || needle.valid) return false;
  return !needle.update(std::numeric_limits<float>::infinity(), 120, true, 300);
}
static_assert(missingReadings(), "Missing/offline readings hide the needle and recover without a stale value");

constexpr bool clockWrap() {
  Needle needle;
  needle.update(0, 120, true, 0xfffffff0u);
  needle.update(120, 120, true, 34u);
  return needle.value > 19.99f && needle.value < 20.01f;
}
static_assert(clockWrap(), "Smoothing must survive the device timer wrapping around");
