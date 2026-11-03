#pragma once
// Arduino/Wire adaptation of the register protocol in Waveshare's official SPD2010 driver.
// Source revision fda89ff2ff32eb1bce561901225c6a36fa684237:
// https://github.com/waveshareteam/ESP32-S3-Touch-LCD-1.46/tree/main/example/Arduino-3.1.1/examples/LVGL_Arduino
// Pin 4 is TP_INT; address 0x53. Register address writes deliberately end with STOP.
#include <Wire.h>
namespace WaveshareTouch {
inline bool healthy=true;
struct Point { bool down=false; int x=0,y=0,weight=0; };
inline bool read(uint16_t reg,uint8_t *bytes,size_t count) {
  if(count==0||count>64)return false;
  Wire.beginTransmission(0x53);Wire.write(static_cast<uint8_t>(reg>>8));Wire.write(static_cast<uint8_t>(reg));
  if(Wire.endTransmission(true)!=0)return false;
  if(Wire.requestFrom(static_cast<uint8_t>(0x53),count)!=count){while(Wire.available())Wire.read();return false;}
  for(size_t i=0;i<count;i++)bytes[i]=Wire.read();return true;
}
inline void command(uint16_t reg,uint16_t value) {
  Wire.beginTransmission(0x53);Wire.write(static_cast<uint8_t>(reg>>8));Wire.write(static_cast<uint8_t>(reg));Wire.write(static_cast<uint8_t>(value));Wire.write(static_cast<uint8_t>(value>>8));Wire.endTransmission(true);
}
inline bool present(){Wire.beginTransmission(0x53);return Wire.endTransmission(true)==0;}
inline bool poll(Point &point,int minimumWeight) {
  uint8_t status[4];if(!read(0x2000,status,4)){healthy=false;return false;}healthy=true;
  if(status[1]&0x40){command(0x0200,1);command(0x0400,1);return false;}
  if(status[1]&0x20){command(0x5000,0);command(0x4600,0);command(0x0200,1);return false;}
  size_t length=status[2]|(static_cast<size_t>(status[3])<<8);
  if((status[0]&3)&&length>=10&&length<=64){
    uint8_t packet[64];if(!read(0x0003,packet,length)){healthy=false;return false;}
    if(packet[4]<=10&&(status[0]&1)){point.x=((packet[7]&0xf0)<<4)|packet[5];point.y=((packet[7]&15)<<8)|packet[6];point.weight=packet[8];point.down=point.weight>=minimumWeight;}
    else point.down=false;
    // Complete HDP reads with bounded retries and bounded remain-data lengths.
    for(int attempt=0;attempt<3;attempt++){
      uint8_t hdp[8];if(!read(0xFC02,hdp,8))break;
      if(hdp[5]==0x82){command(0x0200,1);break;}
      size_t remaining=hdp[2]|(static_cast<size_t>(hdp[3])<<8);
      if(hdp[5]!=0||remaining==0||remaining>64)break;
      uint8_t discard[64];if(!read(0x0003,discard,remaining))break;
    }
    point.x=constrain(point.x,0,411);point.y=constrain(point.y,0,411);return true;
  }
  if(status[1]&8){command(0x0200,1);}
  return false;
}
}
