#pragma once
#include <stdint.h>
#include <stddef.h>

namespace CustomFrame {
constexpr size_t Bytes = 412u * 412u * 2u;
constexpr uint32_t StartHash = 2166136261u;
constexpr uint32_t hashByte(uint32_t hash, uint8_t value) { return (hash ^ value) * 16777619u; }
}
