#pragma once
#include <Arduino.h>
#include <driver/usb_serial_jtag.h>
#include <string.h>

// Same ESP32-S3 hardware USB Serial/JTAG interface and descriptors. Receive
// blocks with the IDF ring buffer, avoiding Arduino HWCDC's per-byte RTOS queue.
class QscreenBulkUsb : public Stream {
  uint8_t pending[8192];
  size_t cursor=0,count=0,rxSize=16384;
  bool ready=false;
public:
  using Print::write;
  size_t setRxBufferSize(size_t value){rxSize=value;return value;}
  void begin(unsigned long){
    if(usb_serial_jtag_is_driver_installed()){ready=true;return;}
    usb_serial_jtag_driver_config_t config={};config.rx_buffer_size=rxSize;config.tx_buffer_size=256;
    ready=usb_serial_jtag_driver_install(&config)==ESP_OK;
  }
  int available() override {
    if(!ready)return 0;
    if(cursor==count){int result=usb_serial_jtag_read_bytes(pending,sizeof(pending),0);cursor=0;count=result>0?result:0;}
    return count-cursor;
  }
  int peek() override{return available()?pending[cursor]:-1;}
  int read() override{return available()?pending[cursor++]:-1;}
  size_t read(uint8_t *target,size_t size){size_t amount=min(size,static_cast<size_t>(available()));if(amount){memcpy(target,pending+cursor,amount);cursor+=amount;}return amount;}
  size_t write(uint8_t value) override{return write(&value,1);}
  size_t write(const uint8_t *data,size_t size) override {
    if(!ready)return 0;int written=usb_serial_jtag_write_bytes(data,size,pdMS_TO_TICKS(100));return written>0?written:0;
  }
  void flush() override{if(ready)usb_serial_jtag_wait_tx_done(pdMS_TO_TICKS(100));}
};
