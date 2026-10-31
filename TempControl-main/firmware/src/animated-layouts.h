#pragma once
// Code-native animaties: geen videobestanden, downloads of extra assets nodig.

static const RGB MINT = {50, 235, 175};
static const RGB PURPLE = {175, 100, 255};

static RGB temperatureColor(float value, bool live) {
  return !live || isnan(value) ? OFFLINE : isHot(value) ? VALUE_HOT : VALUE_COOL;
}

static void themeText(const char *text, int x, int y, RGB color, bool large = false) {
  gfx->setFont(large ? u8g2_font_fub35_tn : u8g2_font_fub17_tr);
  gfx->setTextColor(to565(color));
  drawText(text, x, y, CENTER);
}

static void themeHeader(const char *name, bool live) {
  themeText(BRAND, CX, 55, VALUE_WHITE);
  themeText(live ? name : (everReceived ? "VERBINDING VERLOREN" : "WACHTEN OP PC"), CX, 88, GREY);
}

static void themeReading(const char *label, float value, bool live, int x, int y, RGB color, bool unit) {
  char text[16];
  formatReading(text, sizeof(text), value, live);
  themeText(label, x, y - 29, GREY);
  themeText(text, x, y + 6, !live || isnan(value) ? OFFLINE : color, true);
  if (unit) themeText("C", x, y + 42, !live || isnan(value) ? OFFLINE : color);
}

static void themeFps(bool live, int y = 201) {
  char text[16];
  formatReading(text, sizeof(text), fps, live);
  gfx->setFont(u8g2_font_fub42_tn);
  gfx->setTextColor(to565(live && !isnan(fps) ? VALUE_WHITE : OFFLINE));
  drawText(text, CX, y, CENTER);
  themeText("FPS", CX, y + 49, GREY);
}

static void themeTemperatures(bool live, int y = 325) {
  char text[18], number[12];
  for (int i = 0; i < 2; ++i) {
    float value = i == 0 ? cpuTemp : gpuTemp;
    formatReading(number, sizeof(number), value, live);
    snprintf(text, sizeof(text), "%s %s C", i == 0 ? "CPU" : "GPU", number);
    gfx->fillRoundRect((i == 0 ? 125 : 287) - 74, y - 16, 148, 32, 8, RGB565_BLACK);
    themeText(text, i == 0 ? 125 : 287, y, temperatureColor(value, live));
  }
}

static void polarLine(int x, int y, float inner, float outer, float degrees, RGB color, float brightness = 1) {
  float angle = degrees * PI / 180.0f;
  gfx->drawLine(x + cosf(angle) * inner, y + sinf(angle) * inner,
                x + cosf(angle) * outer, y + sinf(angle) * outer, to565(color, brightness));
}

static void hexagon(int x, int y, int radius, RGB color, float brightness = 1) {
  for (int i = 0; i < 6; ++i) {
    float a = (30 + i * 60) * PI / 180.0f;
    float b = (90 + i * 60) * PI / 180.0f;
    gfx->drawLine(x + cosf(a) * radius, y + sinf(a) * radius,
                  x + cosf(b) * radius, y + sinf(b) * radius, to565(color, brightness));
  }
}

static void animatedRows(bool live, float phase, bool pulse) {
  float values[] = {cpuTemp, gpuTemp, fps};
  const char *labels[] = {"CPU", "GPU", "FPS"};
  for (int i = 0; i < 3; ++i) {
    int y = 153 + i * 80;
    RGB color = i < 2 ? temperatureColor(values[i], live) : PURPLE;
    gfx->fillRoundRect(73, y - 32, 266, 65, 13, RGB565_BLACK);
    float brightness = pulse ? .45f + .35f * (1 + sinf(phase * 2 - i)) / 2 : .45f;
    gfx->drawRoundRect(73, y - 32, 266, 65, 13, to565(color, brightness));
    themeText(labels[i], 119, y, GREY);
    char text[16];
    formatReading(text, sizeof(text), values[i], live);
    themeText(text, 260, y, !live || isnan(values[i]) ? OFFLINE : color, true);
    if (i < 2) themeText("C", 323, y, !live || isnan(values[i]) ? OFFLINE : color);
    if (pulse && live && !isnan(values[i])) {
      int width = constrain((int)(values[i] * (i == 2 ? .75f : 1.8f)), 0, 186);
      if (width > 0) gfx->fillRoundRect(113, y + 23, width, 3, 1, to565(color));
    }
  }
}

static void drawHistory(const float *history, int top, RGB color, bool live, float phase) {
  const int left = 83, width = 246, height = 40;
  for (int i = 0; i <= 2; ++i) gfx->drawFastHLine(left, top + i * height / 2, width, to565(DIVIDER, .6f));
  // Alleen echte ontvangen samples tekenen; gaten blijven zichtbaar bij ontbrekende sensoren.
  if (live) for (int i = 1; i < historyCount; ++i) {
    int first = (historyNext + 60 - historyCount + i - 1) % 60;
    int second = (first + 1) % 60;
    if (isnan(history[first]) || isnan(history[second])) continue;
    int x1 = left + (60 - historyCount + i - 1) * width / 59;
    int x2 = left + (60 - historyCount + i) * width / 59;
    int y1 = top + height - constrain((int)(history[first] * height / 100), 0, height);
    int y2 = top + height - constrain((int)(history[second] * height / 100), 0, height);
    gfx->drawLine(x1, y1, x2, y2, to565(color));
  }
  int scan = left + (int)(phase * 40) % width;
  gfx->drawFastVLine(scan, top, height, to565(color, .25f));
}

static void renderAnimated(bool live) {
  gfx->fillScreen(RGB565_BLACK);
  const float phase = (millis() % 360000u) / 1000.0f;
  switch (displayLayout) {
    case DisplayLayout::Orbit: {
      for (int orbit = 0; orbit < 3; ++orbit) {
        RGB color = orbit == 1 ? PURPLE : VALUE_COOL;
        for (int a = 0; a < 360; a += 4) {
          float angle = a * PI / 180;
          gfx->drawPixel(CX + cosf(angle) * (139 + orbit * 13), 207 + sinf(angle) * (74 + orbit * 20), to565(color, .24f));
        }
        float a = phase * (.7f + orbit * .22f) + orbit * 2;
        gfx->fillCircle(CX + cosf(a) * (139 + orbit * 13), 207 + sinf(a) * (74 + orbit * 20), 4, to565(color));
      }
      themeHeader("ORBIT", live); themeFps(live); themeTemperatures(live, 347);
      break;
    }
    case DisplayLayout::Radar: {
      for (int radius = 47; radius <= 188; radius += 47) gfx->drawCircle(CX, CY, radius, to565(MINT, .16f));
      gfx->drawFastHLine(18, CY, 376, to565(MINT, .12f));
      gfx->drawFastVLine(CX, 18, 376, to565(MINT, .12f));
      for (int tail = 0; tail < 45; tail += 3) polarLine(CX, CY, 30, 187, phase * 45 - tail, MINT, .5f * (45 - tail) / 45);
      gfx->fillCircle(103, 126, 3, to565(temperatureColor(cpuTemp, live), .75f));
      gfx->fillCircle(304, 278, 3, to565(temperatureColor(gpuTemp, live), .75f));
      themeHeader("RADAR", live); animatedRows(live, phase, false);
      break;
    }
    case DisplayLayout::NeonPulse: {
      float glow = .12f + .18f * (1 + sinf(phase * 2)) / 2;
      for (int r = 196; r < 202; ++r) gfx->drawCircle(CX, CY, r, to565(PURPLE, glow));
      themeHeader("NEON PULSE", live); animatedRows(live, phase, true);
      break;
    }
    case DisplayLayout::Reactor: {
      for (int ring = 0; ring < 3; ++ring) for (int segment = 0; segment < 48; ++segment) {
        float angle = segment * 7.5f + phase * (ring % 2 ? -16 : 22);
        RGB color = ring == 1 ? PURPLE : MINT;
        polarLine(CX, CY, 152 + ring * 15, 161 + ring * 15, angle, color, segment % 4 ? .4f : .9f);
      }
      hexagon(CX, CY, 107 + sinf(phase * 2) * 4, MINT, .45f);
      themeHeader("REACTOR", live); themeFps(live); themeTemperatures(live, 341);
      break;
    }
    case DisplayLayout::Waveform: {
      themeHeader("THERMAL HISTORY", live);
      themeFps(live, 132);
      char text[20], number[12];
      formatReading(number, sizeof(number), cpuTemp, live);
      snprintf(text, sizeof(text), "CPU %s C", number);
      themeText(text, CX, 214, temperatureColor(cpuTemp, live));
      drawHistory(cpuHistory, 232, temperatureColor(cpuTemp, live), live, phase);
      formatReading(number, sizeof(number), gpuTemp, live);
      snprintf(text, sizeof(text), "GPU %s C", number);
      themeText(text, CX, 302, temperatureColor(gpuTemp, live));
      drawHistory(gpuHistory, 323, temperatureColor(gpuTemp, live), live, phase);
      break;
    }
    case DisplayLayout::Matrix: {
      gfx->setFont(u8g2_font_fub17_tr);
      for (int column = 0; column < 13; ++column) {
        int x = 26 + column * 30;
        int head = (int)(phase * (32 + column % 4 * 7) + column * 51) % 500 - 40;
        for (int tail = 0; tail < 7; ++tail) {
          int y = head - tail * 24;
          if (y < 20 || y > 392 || (x-CX)*(x-CX)+(y-CY)*(y-CY) > 188*188) continue;
          char digit[] = {(char)('0' + (column * 7 + tail * 3) % 10), 0};
          themeText(digit, x, y, {0, (uint8_t)(120 - tail * 13), 55});
        }
      }
      gfx->fillRect(76, 31, 260, 74, RGB565_BLACK);
      themeHeader("MATRIX", live); animatedRows(live, phase, false);
      break;
    }
    case DisplayLayout::Starfield: {
      for (int star = 0; star < 48; ++star) {
        float angle = star * 2.399963f;
        float depth = fmodf(phase * (.17f + star % 5 * .02f) + star * .037f, 1.0f);
        float radius = 24 + depth * 174;
        int x = CX + cosf(angle) * radius, y = CY + sinf(angle) * radius;
        gfx->fillCircle(x, y, depth > .75f ? 2 : 1, to565(star % 3 ? VALUE_WHITE : VALUE_COOL, .15f + depth * .65f));
      }
      gfx->fillRoundRect(107, 145, 198, 115, 15, RGB565_BLACK);
      themeHeader("STARFIELD", live); themeFps(live); themeTemperatures(live, 335);
      break;
    }
    case DisplayLayout::Speedometer: {
      for (int angle = 135; angle <= 405; angle += 3) polarLine(CX, CY, 135, 143, angle, angle > 330 ? PURPLE : VALUE_COOL, .6f);
      for (int tail = 0; tail < 18; tail += 3)
        polarLine(CX, CY, 146, 151, 135 + fmodf(phase * 35 - tail + 270, 270), PURPLE, .85f * (18-tail) / 18);
      for (int tick = 0; tick <= 8; ++tick) polarLine(CX, CY, 122, 143, 135 + tick * 33.75f, VALUE_WHITE, .65f);
      static float needle = 0;
      float target = live && !isnan(fps) ? constrain(fps, 0.0f, 240.0f) : 0;
      needle += (target - needle) * .14f;
      if (live && !isnan(fps)) {
        polarLine(CX, CY, -13, 111, 135 + needle * 270 / 240, VALUE_HOT);
        gfx->fillCircle(CX, CY, 6, to565(VALUE_WHITE));
      }
      gfx->fillRect(76, 31, 260, 74, RGB565_BLACK);
      themeHeader("SPEEDOMETER", live); themeFps(live, 275); themeTemperatures(live, 355);
      themeText("0", 91, 318, GREY); themeText("240", 321, 318, GREY);
      break;
    }
    case DisplayLayout::Aurora: {
      for (int wave = 0; wave < 4; ++wave) for (int x = 18; x < 394; x += 4) {
        float base = 155 + wave * 38 + sinf(x * .017f + phase * .55f + wave) * 38;
        RGB color = wave % 2 ? PURPLE : MINT;
        for (int tail = 0; tail < 5; ++tail) {
          int y = (int)base + tail * 5;
          if ((x-CX)*(x-CX)+(y-CY)*(y-CY) > 191*191) continue;
          gfx->fillRect(x, y, 4, 5, to565(color, .13f * (5 - tail) / 5));
        }
      }
      gfx->fillRoundRect(114, 143, 184, 120, 14, RGB565_BLACK);
      themeHeader("AURORA", live); themeFps(live); themeTemperatures(live, 343);
      break;
    }
    case DisplayLayout::HexGrid: {
      for (int row = 0; row < 8; ++row) for (int column = 0; column < 8; ++column) {
        int x = 20 + column * 54 + (row % 2) * 27, y = 26 + row * 47;
        if ((x-CX)*(x-CX)+(y-CY)*(y-CY) > 172*172) continue;
        float glow = .08f + .23f * (1 + sinf(phase * 1.5f - column * .7f - row * .35f)) / 2;
        hexagon(x, y, 29, VALUE_COOL, glow);
      }
      themeHeader("HEX GRID", live);
      const int xs[] = {126,286,206}, ys[] = {180,180,304};
      const char *labels[] = {"CPU", "GPU", "FPS"};
      float values[] = {cpuTemp,gpuTemp,fps};
      for (int i = 0; i < 3; ++i) {
        gfx->fillRoundRect(xs[i]-57, ys[i]-53, 114, 107, 20, RGB565_BLACK);
        RGB color = i < 2 ? temperatureColor(values[i], live) : PURPLE;
        hexagon(xs[i],ys[i],68,color,.6f);
        themeReading(labels[i],values[i],live,xs[i],ys[i],color,i < 2);
      }
      break;
    }
    default: break;
  }
  gfx->flush();
}
