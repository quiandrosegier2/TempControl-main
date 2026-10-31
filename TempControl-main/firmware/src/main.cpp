// TempControl - rond PC-systeemmonitor
// Bord: Waveshare ESP32-S3-Touch-LCD-1.46 (SPD2010, 412x412)
//
// De pc stuurt elke seconde een regel over USB-C, bijvoorbeeld:
//   CPU=42.5;GPU=48.0;FPS=165\n
// Temperaturen boven HOT_THRESHOLD kleuren rood, daaronder blauw.

#include <Arduino.h>
#include <Wire.h>
#include <U8g2lib.h>
#include <Arduino_GFX_Library.h>
#include <Preferences.h>
#include "display-layout.h"

// ---------- Instellingen ----------
static const char *BRAND = "QPRINTS";
static const float HOT_THRESHOLD = 50.0f;      // > 50 °C = rood, anders blauw
static const uint32_t DATA_TIMEOUT_MS = 3000;  // geen data -> "wachten op pc"
static const uint8_t ROTATION = 3;             // 0..3, 3 = 270 graden met de klok mee

// ---------- Hardware (Waveshare ESP32-S3-Touch-LCD-1.46) ----------
#define PIN_LCD_BL 5
#define PIN_I2C_SDA 11
#define PIN_I2C_SCL 10
#define TCA9554_ADDR 0x20  // IO-expander: EXIO1 = touch reset, EXIO2 = LCD reset

Arduino_DataBus *bus = new Arduino_ESP32QSPI(
    21 /* CS */, 40 /* SCK */, 46 /* D0 */, 45 /* D1 */, 42 /* D2 */, 41 /* D3 */);
Arduino_GFX *panel = new Arduino_SPD2010(bus, GFX_NOT_DEFINED /* RST via TCA9554 */);
Arduino_Canvas *gfx = new Arduino_Canvas(412, 412, panel, 0, 0, ROTATION);

static const int W = 412;
static const int CX = W / 2;
static const int CY = W / 2;

// ---------- Kleuren ----------
struct RGB { uint8_t r, g, b; };

static const RGB VALUE_COOL = {60, 190, 255};
static const RGB VALUE_HOT = {255, 70, 60};
static const RGB VALUE_WHITE = {255, 255, 255};
static const RGB BRAND_RED = {230, 35, 40};
static const RGB GREY = {150, 150, 160};
static const RGB DIVIDER = {55, 55, 65};
static const RGB OFFLINE = {100, 100, 110};

static uint16_t to565(RGB c, float k = 1.0f) {
  uint8_t r = c.r * k, g = c.g * k, b = c.b * k;
  return ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
}

static bool isHot(float t) { return !isnan(t) && t > HOT_THRESHOLD; }

// ---------- Data ----------
static float cpuTemp = NAN;
static float gpuTemp = NAN;
static float fps = NAN;
static uint32_t lastRx = 0;
static bool everReceived = false;
static bool dirty = true;
static DisplayLayout displayLayout = DisplayLayout::Classic;
static Preferences layoutPreferences;
static bool preferencesReady = false;

static float parseValue(const char *s) {
  char *end;
  float v = strtof(s, &end);
  return (end == s) ? NAN : v;
}

static void handleLine(char *line) {
  if (strcmp(line, "PING") == 0) {
    Serial.println("TEMPMON");
    return;
  }

  float cpu = NAN, gpu = NAN, f = NAN;
  char *save;
  for (char *tok = strtok_r(line, ";", &save); tok; tok = strtok_r(nullptr, ";", &save)) {
    if (strncmp(tok, "CPU=", 4) == 0) cpu = parseValue(tok + 4);
    else if (strncmp(tok, "GPU=", 4) == 0) gpu = parseValue(tok + 4);
    else if (strncmp(tok, "FPS=", 4) == 0) f = parseValue(tok + 4);
    else if (strncmp(tok, "LAYOUT=", 7) == 0) {
      DisplayLayout requested;
      if (parseLayout(tok + 7, requested) && requested != displayLayout) {
        displayLayout = requested;
        if (preferencesReady) layoutPreferences.putString("layout", tok + 7);
        dirty = true;
      }
    }
  }
  if (isnan(cpu) && isnan(gpu) && isnan(f)) return;

  cpuTemp = cpu;
  gpuTemp = gpu;
  fps = f;
  lastRx = millis();
  everReceived = true;
  dirty = true;
}

static void readSerial() {
  static char buf[128];
  static size_t len = 0;
  while (Serial.available()) {
    char c = Serial.read();
    if (c == '\n' || c == '\r') {
      if (len) {
        buf[len] = 0;
        handleLine(buf);
        len = 0;
      }
    } else if (len < sizeof(buf) - 1) {
      buf[len++] = c;
    }
  }
}

// ---------- Tekenen ----------
enum Align { LEFT, CENTER, RIGHT };

// Tekent tekst verticaal gecentreerd op cy; geeft de breedte terug.
static int drawText(const char *s, int x, int cy, Align align) {
  int16_t x1, y1;
  uint16_t w, h;
  gfx->getTextBounds(s, 0, 100, &x1, &y1, &w, &h);
  int left = align == LEFT ? x : align == CENTER ? x - w / 2 : x - w;
  gfx->setCursor(left - x1, cy - h / 2 - (y1 - 100));
  gfx->print(s);
  return w;
}

static int textWidth(const char *s) {
  int16_t x1, y1;
  uint16_t w, h;
  gfx->getTextBounds(s, 0, 100, &x1, &y1, &w, &h);
  return w;
}

// Tekst met extra ruimte tussen de letters ("S Y S T E M").
static void drawSpaced(const char *s, int cx, int cy, int spacing) {
  char ch[2] = {0, 0};
  int total = 0;
  for (const char *p = s; *p; p++) {
    ch[0] = *p;
    total += (*p == ' ' ? 6 : textWidth(ch)) + spacing;
  }
  int x = cx - (total - spacing) / 2;
  for (const char *p = s; *p; p++) {
    ch[0] = *p;
    if (*p != ' ') drawText(ch, x, cy, LEFT);
    x += (*p == ' ' ? 6 : textWidth(ch)) + spacing;
  }
}

// --- iconen (lijntekeningen, 2 px dik) ---
static void thickRoundRect(int x, int y, int w, int h, int r, uint16_t c) {
  gfx->drawRoundRect(x, y, w, h, r, c);
  gfx->drawRoundRect(x + 1, y + 1, w - 2, h - 2, r > 0 ? r - 1 : 0, c);
}

static void iconCpu(int cx, int cy, uint16_t c) {
  thickRoundRect(cx - 12, cy - 12, 24, 24, 3, c);
  gfx->drawRect(cx - 5, cy - 5, 10, 10, c);
  for (int i = 0; i < 4; i++) {
    int o = -8 + i * 5;
    gfx->drawFastVLine(cx + o, cy - 17, 5, c);  // boven
    gfx->drawFastVLine(cx + o, cy + 12, 5, c);  // onder
    gfx->drawFastHLine(cx - 17, cy + o, 5, c);  // links
    gfx->drawFastHLine(cx + 12, cy + o, 5, c);  // rechts
  }
}

static void iconGpu(int cx, int cy, uint16_t c) {
  thickRoundRect(cx - 16, cy - 11, 34, 22, 4, c);
  gfx->drawFastVLine(cx - 20, cy - 11, 26, c);  // slotbeugel
  gfx->drawFastVLine(cx - 19, cy - 11, 26, c);
  gfx->drawFastHLine(cx - 16, cy + 14, 14, c);  // PCIe-connector
  for (int fx : {cx - 6, cx + 9}) {
    gfx->drawCircle(fx, cy, 7, c);
    gfx->drawCircle(fx, cy, 6, c);
    gfx->fillCircle(fx, cy, 2, c);
    gfx->drawLine(fx - 4, cy - 4, fx + 4, cy + 4, c);
    gfx->drawLine(fx - 4, cy + 4, fx + 4, cy - 4, c);
  }
}

static void iconMonitor(int cx, int cy, uint16_t c) {
  thickRoundRect(cx - 16, cy - 13, 32, 21, 3, c);
  gfx->fillRect(cx - 1, cy + 8, 3, 5, c);
  gfx->fillRect(cx - 8, cy + 12, 17, 2, c);
}

static const int VALUE_RIGHT = 350;  // rechterrand van de getallen

static void drawRowLabel(void (*icon)(int, int, uint16_t), const char *label, int cy) {
  icon(102, cy, to565({235, 235, 240}));
  gfx->setFont(u8g2_font_fub35_tr);
  gfx->setTextColor(to565({240, 240, 245}));
  drawText(label, 136, cy, LEFT);
}

static void drawValue(const char *val, RGB color, int cy, bool unit) {
  gfx->setFont(strlen(val) > 3 ? u8g2_font_fub35_tn : u8g2_font_fub42_tn);
  gfx->setTextColor(to565(color));
  drawText(val, VALUE_RIGHT, cy, RIGHT);

  if (unit) {  // °C, bovenaan uitgelijnd zoals in het ontwerp
    uint16_t c = to565(color);
    gfx->drawCircle(VALUE_RIGHT + 8, cy - 14, 3, c);
    gfx->drawCircle(VALUE_RIGHT + 8, cy - 14, 2, c);
    gfx->setFont(u8g2_font_fub17_tr);
    gfx->setTextColor(c);
    drawText("C", VALUE_RIGHT + 13, cy - 6, LEFT);
  }
}

static void drawTemp(void (*icon)(int, int, uint16_t), const char *label, float t, bool live, int cy) {
  drawRowLabel(icon, label, cy);
  char val[8];
  if (live && !isnan(t)) snprintf(val, sizeof(val), "%ld", lroundf(t));
  else strcpy(val, "--");
  drawValue(val, live ? (isHot(t) ? VALUE_HOT : VALUE_COOL) : OFFLINE, cy, true);
}

static void renderClassic(bool live) {
  gfx->fillScreen(RGB565_BLACK);

  // Merk: "QPRINTS //"
  gfx->setFont(u8g2_font_fub30_tr);
  const char *slashes = " //";
  int wBrand = textWidth(BRAND), wSlash = textWidth(slashes);
  int x = CX - (wBrand + wSlash) / 2;
  gfx->setTextColor(to565({245, 245, 250}));
  drawText(BRAND, x, 90, LEFT);
  gfx->setTextColor(to565(BRAND_RED));
  drawText(slashes, x + wBrand, 90, LEFT);

  gfx->setFont(u8g2_font_fur20_tr);
  gfx->setTextColor(to565(GREY));
  if (live) drawSpaced("SYSTEM MONITOR", CX, 127, 4);
  else drawSpaced(everReceived ? "VERBINDING VERLOREN" : "WACHTEN OP PC", CX, 127, 3);

  // Rijen
  drawTemp(iconCpu, "CPU", cpuTemp, live, 176);
  gfx->drawFastHLine(84, 213, 244, to565(DIVIDER));
  drawTemp(iconGpu, "GPU", gpuTemp, live, 250);
  gfx->drawFastHLine(84, 287, 244, to565(DIVIDER));

  drawRowLabel(iconMonitor, "FPS", 324);
  char val[8];
  if (live && !isnan(fps)) snprintf(val, sizeof(val), "%ld", min(lroundf(fps), 9999L));
  else strcpy(val, "--");
  drawValue(val, live ? VALUE_WHITE : OFFLINE, 324, false);

  gfx->flush();
}

static void compactHeader(bool live) {
  gfx->setFont(u8g2_font_fub30_tr);
  gfx->setTextColor(to565(VALUE_WHITE));
  drawText(BRAND, CX, 77, CENTER);
  gfx->setFont(u8g2_font_fub17_tr);
  gfx->setTextColor(to565(GREY));
  drawText(live ? "SYSTEM MONITOR" : (everReceived ? "VERBINDING VERLOREN" : "WACHTEN OP PC"), CX, 116, CENTER);
}

static void formatReading(char *buffer, size_t size, float value, bool live) {
  if (live && !isnan(value)) snprintf(buffer, size, "%ld", min(lroundf(value), 9999L));
  else snprintf(buffer, size, "--");
}

static void drawGauge(const char *label, float value, bool live, int x) {
  const int y = 212, radius = 64;
  RGB color = !live || isnan(value) ? OFFLINE : isHot(value) ? VALUE_HOT : VALUE_COOL;
  const int filled = live && !isnan(value) ? constrain((int)lroundf(value * 2.7f), 0, 270) : 0;
  for (int angle = 0; angle <= 270; angle += 2) {
    const float radians = (135 + angle) * PI / 180.0f;
    gfx->drawLine(x + cosf(radians) * (radius - 7), y + sinf(radians) * (radius - 7),
                  x + cosf(radians) * radius, y + sinf(radians) * radius,
                  to565(angle < filled ? color : DIVIDER));
  }
  gfx->setFont(u8g2_font_fub17_tr);
  gfx->setTextColor(to565(GREY));
  drawText(label, x, 186, CENTER);
  char valueText[12];
  formatReading(valueText, sizeof(valueText), value, live);
  gfx->setFont(u8g2_font_fub35_tn);
  gfx->setTextColor(to565(color));
  drawText(valueText, x, 225, CENTER);
  gfx->setFont(u8g2_font_fub17_tr);
  drawText("C", x, 267, CENTER);
}

static void renderGauges(bool live) {
  gfx->fillScreen(RGB565_BLACK);
  compactHeader(live);
  drawGauge("CPU", cpuTemp, live, 126);
  drawGauge("GPU", gpuTemp, live, 286);
  char value[12];
  formatReading(value, sizeof(value), fps, live);
  gfx->setFont(u8g2_font_fub35_tn);
  gfx->setTextColor(to565(live ? VALUE_WHITE : OFFLINE));
  drawText(value, CX, 326, CENTER);
  gfx->setFont(u8g2_font_fub17_tr);
  gfx->setTextColor(to565(GREY));
  drawText("FPS", CX, 367, CENTER);
  gfx->flush();
}

static void renderMinimal(bool live) {
  gfx->fillScreen(RGB565_BLACK);
  compactHeader(live);
  char value[12];
  formatReading(value, sizeof(value), fps, live);
  gfx->setFont(u8g2_font_fub42_tn);
  gfx->setTextSize(2);
  gfx->setTextColor(to565(live ? VALUE_WHITE : OFFLINE));
  drawText(value, CX, 198, CENTER);
  gfx->setTextSize(1);
  gfx->setFont(u8g2_font_fub17_tr);
  gfx->setTextColor(to565(GREY));
  drawText("FPS", CX, 262, CENTER);
  gfx->drawFastHLine(83, 287, 246, to565(DIVIDER));
  for (int i = 0; i < 2; ++i) {
    int x = i == 0 ? 133 : 279;
    float temperature = i == 0 ? cpuTemp : gpuTemp;
    gfx->setFont(u8g2_font_fub17_tr);
    gfx->setTextColor(to565(GREY));
    drawText(i == 0 ? "CPU" : "GPU", x, 313, CENTER);
    formatReading(value, sizeof(value), temperature, live);
    strncat(value, " C", sizeof(value) - strlen(value) - 1);
    gfx->setTextColor(to565(!live || isnan(temperature) ? OFFLINE : isHot(temperature) ? VALUE_HOT : VALUE_COOL));
    drawText(value, x, 347, CENTER);
  }
  gfx->flush();
}

static void render(bool live) {
  switch (displayLayout) {
    case DisplayLayout::Gauges: renderGauges(live); break;
    case DisplayLayout::Minimal: renderMinimal(live); break;
    default: renderClassic(live); break;
  }
}

// Reset LCD + touch via de TCA9554 IO-expander (het bord heeft geen directe RST-pin).
static void resetPanel() {
  Wire.begin(PIN_I2C_SDA, PIN_I2C_SCL);
  auto writeReg = [](uint8_t reg, uint8_t val) {
    Wire.beginTransmission(TCA9554_ADDR);
    Wire.write(reg);
    Wire.write(val);
    Wire.endTransmission();
  };
  writeReg(0x03, 0xFC);  // EXIO1 + EXIO2 als uitgang
  writeReg(0x01, 0x00);  // laag = reset
  delay(20);
  writeReg(0x01, 0x03);  // hoog = vrijgeven
  delay(120);
}

void setup() {
  Serial.begin(115200);
  preferencesReady = layoutPreferences.begin("tempcontrol", false);
  if (preferencesReady) {
    String stored = layoutPreferences.getString("layout", "classic");
    parseLayout(stored.c_str(), displayLayout);
  }

  resetPanel();

  if (!gfx->begin()) {
    Serial.println("Display init mislukt");
  }
  pinMode(PIN_LCD_BL, OUTPUT);
  digitalWrite(PIN_LCD_BL, HIGH);
}

void loop() {
  readSerial();

  static uint32_t lastFrame = 0;
  static bool wasLive = false;
  if (millis() - lastFrame < 40) return;  // max ~25 fps
  lastFrame = millis();

  const bool live = everReceived && (millis() - lastRx < DATA_TIMEOUT_MS);
  if (live != wasLive) {
    wasLive = live;
    dirty = true;
  }

  if (dirty) {
    dirty = false;
    render(live);
  }
}
