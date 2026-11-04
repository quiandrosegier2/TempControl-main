#pragma once
#include "custom-frame.h"
#include <string.h>
namespace FramePatch {
inline uint16_t u16(const uint8_t *p){return uint16_t(p[0])|(uint16_t(p[1])<<8);}
// Validate all rectangles before writing. The caller uses a staging frame and verifies its hash.
inline bool apply(const uint8_t *payload,size_t length,uint8_t *candidate){
  if(length<2)return false;
  unsigned count=u16(payload);if(count<1||count>64)return false;
  size_t offset=2;
  for(unsigned i=0;i<count;i++){
    if(offset+8>length)return false;
    unsigned x=u16(payload+offset),y=u16(payload+offset+2),w=u16(payload+offset+4),h=u16(payload+offset+6);offset+=8;
    if(w<1||h<1||x+w>412||y+h>412||offset+size_t(w)*h*2>length)return false;
    offset+=size_t(w)*h*2;
  }
  if(offset!=length)return false;
  offset=2;
  for(unsigned i=0;i<count;i++){
    unsigned x=u16(payload+offset),y=u16(payload+offset+2),w=u16(payload+offset+4),h=u16(payload+offset+6);offset+=8;
    for(unsigned row=0;row<h;row++){memcpy(candidate+((y+row)*412+x)*2,payload+offset,w*2);offset+=w*2;}
  }
  return true;
}
}
