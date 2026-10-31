#pragma once
#include <stdint.h>
#include <limits>

namespace Speedometer {
constexpr float clampReading(float value, float maximum) {
  return value < 0 ? 0 : value > maximum ? maximum : value;
}

constexpr float angleFor(float value, float maximum) {
  return 135.0f + clampReading(value, maximum) * 270.0f / maximum;
}

constexpr float smoothReading(float current, float target, uint32_t elapsedMs) {
  return current + (target - current) * (elapsedMs / (250.0f + elapsedMs));
}

struct Needle {
  float value = 0;
  bool valid = false;
  uint32_t updatedAt = 0;

  constexpr bool update(float reading, float maximum, bool live, uint32_t now) {
    const float limit = std::numeric_limits<float>::max();
    if (!live || !(reading >= -limit && reading <= limit)) {
      valid = false;
      updatedAt = now;
      return false;
    }
    float target = clampReading(reading, maximum);
    value = valid ? smoothReading(value, target, now - updatedAt) : target;
    valid = true;
    updatedAt = now;
    return true;
  }
};
}
