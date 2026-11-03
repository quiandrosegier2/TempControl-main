#pragma once
#include <stdint.h>
namespace TouchGesture {
enum class Type { None, Tap, Next, Previous, Menu };
struct Options { int swipe=60, hold=800, rotation=3; bool menu=true; };
struct Event { Type type=Type::None; int x=0,y=0; };
constexpr int distance(int a) { return a<0?-a:a; }
struct Tracker {
  bool down=false,menuSent=false,hasEvent=false;
  int startX=0,startY=0,lastX=0,lastY=0;
  uint32_t started=0,lastEvent=0;
  constexpr Event update(bool pressed,int x,int y,uint32_t now,const Options &options) {
    int tx=x,ty=y;
    switch(options.rotation){case 1:tx=411-y;ty=x;break;case 2:tx=411-x;ty=411-y;break;case 3:tx=y;ty=411-x;break;default:break;}
    if(pressed&&!down){down=true;menuSent=false;startX=lastX=tx;startY=lastY=ty;started=now;}
    if(pressed||down){lastX=tx;lastY=ty;}
    Event result;
    bool ready=!hasEvent||static_cast<uint32_t>(now-lastEvent)>=250;
    int dx=lastX-startX,dy=lastY-startY;
    if(pressed&&down&&options.menu&&!menuSent&&now-started>=static_cast<uint32_t>(options.hold)&&distance(dx)<20&&distance(dy)<20&&ready){result={Type::Menu,lastX,lastY};menuSent=true;}
    if(!pressed&&down){
      down=false;
      if(!menuSent&&ready&&now-started>=50){
        if(distance(dx)>=options.swipe&&distance(dx)>distance(dy)*3/2)result={dx<0?Type::Next:Type::Previous,lastX,lastY};
        else if(now-started<static_cast<uint32_t>(options.hold)&&distance(dx)<20&&distance(dy)<20)result={Type::Tap,lastX,lastY};
      }
    }
    if(result.type!=Type::None){lastEvent=now;hasEvent=true;}return result;
  }
};
}
