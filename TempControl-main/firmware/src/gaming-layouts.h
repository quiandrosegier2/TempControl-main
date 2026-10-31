#pragma once
// Originele, code-native game-art. Alle spelanimaties zijn decoratief;
// de HUD en hittebalken gebruiken uitsluitend echte sensorgegevens.
static const RGB GOLD = {245, 184, 65};
static const RGB ORANGE = {255, 105, 25};
static const RGB TACTICAL = {120, 235, 95};

static void combatHud(const char *title, RGB accent, bool live) {
  for (int side = 0; side < 2; ++side) for (int row = 0; row < 2; ++row) {
    int x = side ? 343 : 69, y = row ? 318 : 116;
    gfx->drawFastHLine(side ? x-18 : x, y, 18, to565(accent, .6f));
    gfx->drawFastVLine(x, row ? y-18 : y, 18, to565(accent, .6f));
  }
  gfx->fillRoundRect(95, 29, 222, 72, 12, RGB565_BLACK);
  gfx->fillRoundRect(124, 123, 164, 112, 14, RGB565_BLACK);
  themeHeader(title, live);
  themeFps(live, 166);
  themeTemperatures(live, 346);
  for (int i = 0; i < 2; ++i) {
    float value = i == 0 ? cpuTemp : gpuTemp;
    int left = i == 0 ? 74 : 228;
    gfx->fillRect(left, 363, 110, 3, to565(DIVIDER));
    if (live && !isnan(value)) {
      int width = constrain((int)(value * 110 / 120), 0, 110);
      if (width) gfx->fillRect(left, 363, width, 3, to565(temperatureColor(value, live)));
    }
  }
}

static void rifleSilhouette(int x, int y, RGB color, bool flash) {
  uint16_t c = to565(color);
  gfx->fillTriangle(x, y+9, x+46, y+3, x+46, y+29, c);
  gfx->fillRoundRect(x+43, y, 92, 27, 4, c);
  gfx->fillRect(x+130, y+6, 83, 9, c);
  gfx->fillRect(x+210, y+3, 13, 15, c);
  gfx->fillRect(x+73, y-10, 45, 7, c);
  gfx->fillRect(x+81, y-3, 5, 8, c);
  gfx->fillTriangle(x+98, y+25, x+118, y+25, x+106, y+59, c);
  gfx->fillTriangle(x+98, y+25, x+106, y+59, x+87, y+53, c);
  gfx->fillTriangle(x+59, y+25, x+75, y+25, x+63, y+47, c);
  gfx->drawFastHLine(x+52, y+8, 68, to565(GOLD, .7f));
  if (flash) {
    gfx->fillTriangle(x+224,y+10,x+245,y-1,x+240,y+21,to565(ORANGE));
    gfx->fillTriangle(x+225,y+10,x+235,y+5,x+235,y+15,to565(GOLD));
  }
}

static void renderGaming(bool live, float phase) {
  switch (displayLayout) {
    case DisplayLayout::Arsenal: {
      for (int y = 115; y < 320; y += 20) gfx->drawFastHLine(75,y,262,to565(TACTICAL,.08f));
      bool flash = fmodf(phase, 1.6f) < .13f;
      int recoil = flash ? -3 : 0;
      rifleSilhouette(76+recoil, 251, {108,125,108}, flash);
      for (int shell=0;shell<5;++shell) {
        float t=fmodf(phase*.7f+shell*.2f,1.0f);
        int x=176+t*115, y=244-65*sinf(t*PI);
        gfx->fillRect(x,y,8,3,to565(GOLD,.6f*(1-t)));
      }
      combatHud("ARSENAL",TACTICAL,live);
      break;
    }
    case DisplayLayout::BulletStorm: {
      for(int bullet=0;bullet<18;++bullet) {
        float t=fmodf(phase*(.13f+bullet%3*.025f)+bullet*.137f,1.0f);
        int x=35+(bullet*53)%340, y=425-t*470;
        gfx->drawLine(x+6,y+35,x+17,y+68,to565(GOLD,.13f));
        gfx->fillTriangle(x,y,x+6,y-11,x+12,y,to565(GOLD,.85f));
        gfx->fillRect(x,y,12,24,to565(GOLD,.5f));
        gfx->drawFastHLine(x,y+20,12,to565(ORANGE,.8f));
      }
      combatHud("BULLET STORM",GOLD,live);
      break;
    }
    case DisplayLayout::Inferno: {
      for(int x=14;x<398;x+=9) {
        int height=90+30*sinf(x*.08f+phase*2)+24*sinf(x*.17f-phase*3);
        gfx->fillTriangle(x-8,399,x+6,399,x+2,399-height,to565({155,27,12}));
        gfx->fillTriangle(x-5,400,x+6,400,x+1,419-height,to565(ORANGE,.75f));
        gfx->fillTriangle(x-3,402,x+5,402,x+1,446-height,to565(GOLD,.8f));
      }
      for(int spark=0;spark<30;++spark) {
        float t=fmodf(phase*.22f+spark*.081f,1.0f);
        int x=35+(spark*67)%340+8*sinf(phase+spark),y=400-t*270;
        gfx->fillCircle(x,y,spark%4==0?2:1,to565(GOLD,(1-t)*.8f));
      }
      combatHud("INFERNO",ORANGE,live);
      break;
    }
    case DisplayLayout::WarzoneCity: {
      for(int layer=0;layer<2;++layer) for(int b=0;b<18;++b) {
        int x=(b*29+430-(int)(phase*(layer?7:3))%430)%430-20;
        int height=45+(b*31+layer*47)%115, width=layer?25:28;
        int base=layer?320:289;
        gfx->fillRect(x,base-height,width,height,to565(layer?RGB{34,42,49}:RGB{17,23,30}));
        for(int wy=base-height+9;wy<base-5;wy+=14) for(int wx=x+5;wx<x+width-3;wx+=9)
          if((wx+wy+b)%3) gfx->fillRect(wx,wy,3,5,to565(b%4?VALUE_COOL:GOLD,.35f));
        if(layer&&b%5==0) {
          int flare=10+6*sinf(phase*4+b);
          gfx->fillTriangle(x+4,base,x+22,base,x+13,base-flare-13,to565(ORANGE,.8f));
        }
      }
      for(int tracer=0;tracer<4;++tracer) {
        float t=fmodf(phase*.45f+tracer*.25f,1.0f);
        int x=40+t*340,y=305-t*165;
        gfx->drawLine(x,y,x+22,y-11,to565(GOLD,.55f));
      }
      combatHud("WARZONE CITY",ORANGE,live);
      break;
    }
    case DisplayLayout::SniperScope: {
      gfx->drawCircle(CX,CY,185,to565(TACTICAL,.5f));
      gfx->drawCircle(CX,CY,153,to565(TACTICAL,.17f));
      gfx->drawFastHLine(22,CY,368,to565(TACTICAL,.4f));
      gfx->drawFastVLine(CX,22,368,to565(TACTICAL,.4f));
      for(int offset=-150;offset<=150;offset+=15) {
        gfx->drawFastVLine(CX+offset,CY-4,9,to565(TACTICAL,.6f));
        gfx->drawFastHLine(CX-4,CY+offset,9,to565(TACTICAL,.6f));
      }
      int x=125+sinf(phase*.7f)*34,y=271+cosf(phase*.9f)*13;
      gfx->drawRect(x-17,y-17,34,34,to565(TACTICAL,.75f));
      gfx->drawCircle(x,y,3,to565(TACTICAL));
      combatHud("SNIPER SCOPE",TACTICAL,live);
      break;
    }
    case DisplayLayout::CyberOps: {
      for(int wire=0;wire<14;++wire) {
        int y=114+wire*15,x=35+(wire%4)*11,turn=104+(wire*31)%193;
        RGB color=wire%2?PURPLE:VALUE_COOL;
        gfx->drawLine(x,y,turn,y,to565(color,.22f));
        gfx->drawLine(turn,y,turn+26,y+18,to565(color,.22f));
        gfx->drawLine(turn+26,y+18,379,y+18,to565(color,.22f));
        float t=fmodf(phase*.22f+wire*.13f,1.0f);
        int px=x+t*(379-x);
        int py=px<turn?y:px>turn+26?y+18:y+(px-turn)*18/26;
        gfx->fillCircle(px,py,2,to565(color,.9f));
      }
      combatHud("CYBER OPS",VALUE_COOL,live);
      break;
    }
    case DisplayLayout::PlasmaCore: {
      for(int bolt=0;bolt<18;++bolt) {
        float angle=bolt*20+phase*18;
        float r=72+14*sinf(phase*4+bolt);
        int x=206+cosf(angle*PI/180)*r,y=270+sinf(angle*PI/180)*r;
        int mx=(206+x)/2+7*sinf(phase*7+bolt),my=(270+y)/2;
        gfx->drawLine(206,270,mx,my,to565(PURPLE,.45f));
        gfx->drawLine(mx,my,x,y,to565(VALUE_COOL,.7f));
      }
      for(int radius=43;radius>0;radius-=4)
        gfx->fillCircle(206,270,radius,to565(radius>31?PURPLE:VALUE_COOL,.12f+(43-radius)*.012f));
      gfx->drawCircle(206,270,43+3*sinf(phase*3),to565(PURPLE,.8f));
      combatHud("PLASMA CORE",PURPLE,live);
      break;
    }
    case DisplayLayout::BossFight: {
      const char *sprite[]={"001101100","011111110","110111011","111111111","001111100","010010010","100000001"};
      int x=156+sinf(phase*1.8f)*16,y=237+sinf(phase*2.7f)*5;
      for(int row=0;row<7;++row) for(int column=0;column<9;++column)
        if(sprite[row][column]=='1') gfx->fillRect(x+column*11,y+row*11,10,10,to565(row<3?PURPLE:VALUE_HOT,.9f));
      float beam=(1+sinf(phase*3))/2;
      gfx->drawLine(x+22,y+25,61,322,to565(VALUE_HOT,.2f+beam*.45f));
      gfx->drawLine(x+77,y+25,351,322,to565(VALUE_HOT,.2f+beam*.45f));
      combatHud("BOSS FIGHT",VALUE_HOT,live);
      break;
    }
    default: break;
  }
}
