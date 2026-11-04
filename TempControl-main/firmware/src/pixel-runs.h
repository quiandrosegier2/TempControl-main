#pragma once
#include <stdint.h>
#include <stddef.h>
#include <string.h>
namespace PixelRuns {
inline unsigned word(const uint8_t *p){return unsigned(p[0])|(unsigned(p[1])<<8);}
inline bool decode(const uint8_t *source,size_t size,uint8_t *target,size_t capacity,size_t &written){
  size_t cursor=0;written=0;
  while(cursor<size){
    if(cursor+2>size)return false;unsigned control=word(source+cursor),count=control&32767;cursor+=2;
    if(count==0||size_t(count)*2>capacity-written)return false;
    if(control&32768){if(cursor+2>size)return false;uint8_t lo=source[cursor],hi=source[cursor+1];cursor+=2;for(unsigned i=0;i<count;i++){target[written++]=lo;target[written++]=hi;}}
    else{size_t bytes=size_t(count)*2;if(bytes>size-cursor)return false;memcpy(target+written,source+cursor,bytes);written+=bytes;cursor+=bytes;}
  }
  return written>0;
}
}
