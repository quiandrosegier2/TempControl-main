#include "../../firmware/src/touch-gestures.h"
using namespace TouchGesture;
constexpr bool gestures() {
  Options options; options.rotation=0;
  Tracker tap;tap.update(true,200,200,0,options);if(tap.update(false,200,200,60,options).type!=Type::Tap)return false;
  Tracker swipe;swipe.update(true,300,200,0,options);swipe.update(true,150,205,100,options);if(swipe.update(false,150,205,150,options).type!=Type::Next)return false;
  Tracker hold;hold.update(true,200,200,0,options);if(hold.update(true,200,200,801,options).type!=Type::Menu)return false;if(hold.update(true,200,200,1100,options).type!=Type::None)return false;
  Tracker noise;noise.update(true,200,200,0,options);if(noise.update(false,201,200,10,options).type!=Type::None)return false;
  return true;
}
static_assert(gestures(),"Touch debouncing, swipe direction and long-press recognition must remain stable");
