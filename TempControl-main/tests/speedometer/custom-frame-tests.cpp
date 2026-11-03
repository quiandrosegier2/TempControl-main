#include "../../firmware/src/custom-frame.h"
constexpr uint32_t hashHello() {
  uint32_t hash = CustomFrame::StartHash;
  for (char c : "hello") { if (c) hash = CustomFrame::hashByte(hash, static_cast<uint8_t>(c)); }
  return hash;
}
static_assert(CustomFrame::Bytes == 339488, "The frame must exactly match the 412x412 RGB565 canvas");
static_assert(hashHello() == 0x4f9f2cabu, "Receiver checksum must match the Windows sender and known FNV-1a vector");
