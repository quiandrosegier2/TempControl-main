# TempControl

Rond systeemmonitor-scherm voor de pc. Een **Waveshare ESP32-S3-Touch-LCD-1.46** toont **CPU-temperatuur, GPU-temperatuur en FPS**, die via **USB-C** van Windows binnenkomen.

- **Boven 50 °C kleuren de waarde en de gloeiende ring rood**
- **Op of onder 50 °C blijven ze blauw**
- Bovenaan staan `QPRINTS //` en `SYSTEM MONITOR`. Krijgt het scherm geen data, dan staat daar `WACHTEN OP PC`.

```
firmware/          ESP32-S3 firmware (PlatformIO + Arduino_GFX)
pc-app/            Windows-app (.NET 10 + LibreHardwareMonitor + PresentMon)
pc-app/installer/  Inno Setup-installer
```

## 1. Firmware op het scherm zetten

### Voor gebruikers: via de Windows-installer (geen ontwikkeltools nodig)

Download het installatiebestand via [GitHub Releases](https://github.com/quiandrosegier2/TempControl-main/releases/latest).

1. Start `Qprints esp installer-1.4.0.exe`, geef toestemming voor beheerdersrechten en kies een schermlayout. Het bewegende demovoorbeeld speelt op de layoutpagina; met **Pauzeren/Afspelen** stop of start je de animatie.
2. Sluit een **Waveshare ESP32-S3-Touch-LCD-1.46 (16 MB)** met een USB-C-datakabel aan.
3. Laat aan het einde van Setup de optie om firmware te installeren aangevinkt.
4. Kies de COM-poort van het scherm en klik op **Firmware installeren**. Dit vervangt de bestaande firmware.
5. Laat de kabel aangesloten tot de upload klaar is. Druk zo nodig op RESET of sluit het scherm opnieuw aan.

Geen poort of geen verbinding? Houd BOOT ingedrukt, druk op RESET, laat BOOT los en klik op **Vernieuwen**. Kies daarna de nieuwe COM-poort en probeer opnieuw.

Opnieuw installeren kan via **Start > TempMonitor > Firmware installeren**. De monitor-app wordt tijdens de upload gepauzeerd en daarna hervat. Uploadlogs staan in `C:\ProgramData\TempMonitor`.

De app, .NET, firmware, het uploadprogramma en de optionele PawnIO-driver zitten in de installer. Gebruikers hoeven geen VS Code, Python, PlatformIO, internetverbinding of winget te hebben. Windows kan na de driverinstallatie om een herstart vragen.

### Blijvend programma: Layout Studio

Na installatie kun je **Qprints esp installer** altijd opnieuw openen via Start of het bureaublad. Het donkere Layout Studio-venster toont alle 31 ontwerpen, inclusief bewegende offline voorbeelden en Pauzeren/Afspelen. Je kunt een layout toepassen, firmware uploaden, de achtergrondmeting starten en het logboek openen. Het venster sluiten laat de achtergrondmeting doorwerken. Je kunt het venster ook starten met `TempMonitor.exe --settings`.

### Schermlayouts

- **Classic**: de originele QPRINTS-weergave met drie rijen.
- **Gauges**: ronde CPU- en GPU-temperatuurmeters met FPS eronder.
- **Minimal**: grote FPS-weergave met compacte CPU- en GPU-temperaturen.
- **Orbit**: lichtpunten draaien in banen rond de FPS-weergave.
- **Radar**: draaiende radarsweep achter drie meetpanelen.
- **Neon Pulse**: pulserende neonranden en meetbalken.
- **Reactor**: tegengesteld draaiende segmentringen en een pulserende kern.
- **Waveform**: echte temperatuurhistorie met bewegende scanlijnen.
- **Matrix**: vallende cijferkolommen achter donkere meetpanelen.
- **Starfield**: sterren bewegen vanuit het midden naar buiten.
- **Speedometer**: drie onafhankelijke meters: FPS groot bovenaan (0-240), CPU en GPU kleiner eronder (0-120 C). Elke naald volgt zijn eigen meetwaarde met een soepele beweging. Ontbrekende meetwaarden verbergen alleen de betreffende naald; de digitale getallen tonen de echte meetwaarden, ook boven het bereik van de meter. Voor deze indeling moet de firmware eenmaal naar 1.2.2 worden bijgewerkt.
- **Aurora**: zacht golvende lichtgordijnen.
- **Hex Grid**: verlicht honingraatraster met drie zeshoekige meetpanelen.
- **Arsenal**: bewegend geweersilhouet met terugslag, mondingsflitsen en hulzen.
- **Bullet Storm**: bewegende gouden patronen en lichtsporen.
- **Inferno**: geanimeerde vlammen en opstijgende vonken.
- **Warzone City**: verschuivende gebouwen, brandhaarden en lichtsporen.
- **Sniper Scope**: groene viziermarkeringen en een bewegend doelkader.
- **Cyber Ops**: neoncircuits met reizende datapulsen.
- **Plasma Core**: pulserende energiekern met elektrische bogen.
- **Drone Swarm**: bewegende droneformaties.
- **Lightning Strike**: geanimeerde rode bliksem.
- **Space Fleet**: bewegende ruimteschepen met motorsporen.
- **Race Grid**: rollend perspectiefraster.
- **Portal Rift**: draaiende, golvende portaalfragmenten.
- **Tactical Steel** (statisch): stalen paneel met klinknagels.
- **Ammo Vault** (statisch): patronenrek.
- **Samurai Crest** (statisch): helm-embleem en gekruiste zwaarden.
- **Retro Arcade** (statisch): pixeltegels en arcadeframe.
- **Stealth Map** (statisch): tactische kaart en vizier.
- **Boss Fight**: bewegende pixelbaas, laseraccenten en echte temperatuur-hittebalken.

Setup toont schematische demo's met testwaarden en bewaart de keuze in `layout.txt` naast de Windows-app. De 23 bewegende layouts hebben een automatisch afspelend voorbeeld in een lus van vier seconden, met een knop **Pauzeren/Afspelen**. Classic, Gauges, Minimal en de vijf nieuwe statische gamingthema's tonen een stilstaand voorbeeld. Hun echte meetwaarden blijven wel bijwerken. Een scherm of internetverbinding is niet nodig om de voorbeelden vooraf te bekijken. De app stuurt de gekozen layout via USB; het scherm onthoudt die keuze. Installeer de meegeleverde firmware eenmaal om alle eenendertig layouts en de actuele Speedometer-indeling te krijgen. Werk de firmware eenmaal bij naar 1.4.0. Daarna open je **Qprints esp installer** via het bureaublad of Start, kies je een ontwerp en klik je op **Layout toepassen**. De achtergrondmeting leest de keuze binnen een seconde opnieuw in, zonder herstart of opnieuw flashen.

De 23 bewegende layouts blijven bewegen tussen USB-updates, met maximaal 20 animatiebeelden per seconde. Dit staat los van de getoonde game-FPS. Waveform bewaart de laatste 60 echte CPU- en GPU-samples in RAM; ontbrekende sensoren leveren gaten op, geen verzonnen meetwaarden. Na een herstart begint deze historie opnieuw. Alle layouts tonen ontbrekende meetwaarden als `--` en temperaturen boven 50 graden in rood.

[Bekijk alle layouts](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.4.0/Layout-Overview-1.4.0.png) of [de acht gamingthema's](https://github.com/quiandrosegier2/TempControl-main/releases/download/v1.3.0/Gaming-Layouts-1.3.0.png).

De gaming-art wordt in code getekend. Geweren, patronen, vuur en de pixelbaas zijn decoratieve animaties; CPU, GPU, FPS en de hittebalken gebruiken echte meetgegevens. Werk de firmware eenmaal bij naar 1.3.0 voor deze acht thema's.

Voor stille installaties kan `/LAYOUT=` worden gevolgd door `classic`, `gauges`, `minimal`, `orbit`, `radar`, `neon-pulse`, `reactor`, `waveform`, `matrix`, `starfield`, `speedometer`, `aurora`, `hex-grid`, `arsenal`, `bullet-storm`, `inferno`, `warzone-city`, `sniper-scope`, `cyber-ops`, `plasma-core` `boss-fight`, `drone-swarm`, `lightning-strike`, `space-fleet`, `race-grid`, `portal-rift`, `tactical-steel`, `ammo-vault`, `samurai-crest`, `retro-arcade` of `stealth-map`. Zonder deze parameter blijft een bestaande keuze behouden.

### Firmware zelf bouwen met VS Code

1. Installeer **Visual Studio Code** en daarin de extensie **PlatformIO IDE**.
2. Open in VS Code de map `firmware/` (*File → Open Folder*).
3. Sluit het scherm met een USB-C-datakabel aan op de pc.
4. Klik onderaan in de blauwe balk op **→ (Upload)**. De eerste keer downloadt PlatformIO de ESP32-toolchain, dat duurt enkele minuten.
5. Op het scherm verschijnt `WACHTEN OP PC`.

> Lukt het uploaden niet? Hou de **BOOT**-knop ingedrukt, druk kort op **RESET**, laat BOOT los en upload opnieuw.

## 2. Windows-app installeren

Voer `Qprints esp installer-1.4.0.exe` uit. De donkere installer en het firmwarevenster tonen het QPRINTS //-beeldmerk. De installer:

- installeert de app in `C:\Program Files\TempMonitor`; .NET zit erin, dus dat hoef je niet apart te installeren
- installeert desgewenst de **PawnIO**-driver (vinkje). Die is nodig om de CPU-temperatuur van een Ryzen te lezen.
- zorgt dat de app onzichtbaar en met beheerdersrechten start bij elke aanmelding
- start de app meteen

Verwijderen gaat via *Instellingen → Apps → Qprints esp installer*.

**Werkt er iets niet?** Kijk in het logbestand `C:\ProgramData\TempMonitor\TempMonitor.log`. Daar staat of het scherm gevonden is en welke sensoren ontbreken.

### Installer zelf bouwen

Installeer ook de pioarduino CLI (`pip install pioarduino==6.2.0`). Het buildscript bouwt de firmware en bundelt de volledige factory-image en de Windows-versie van esptool 5.1.0. De esptool-download wordt met de SHA256 uit de officiele GitHub-release gecontroleerd. Voor het bouwen is internet nodig.

Met `-PlatformIO <pad-naar-pio.exe>` en `-InnoCompiler <pad-naar-ISCC.exe>` kun je eigen buildtools kiezen. De lokale `.platformio/penv/Scripts/pio.exe` wordt automatisch gebruikt als die aanwezig is.

De ESP32-SDK bevat lange bestandsnamen. Zorg op de Windows-build-pc voor ondersteuning voor lange bestandspaden of gebruik een korte PlatformIO-map. Dit geldt alleen voor ontwikkelaars die de installer bouwen.

Vereisten: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) en Inno Setup 6.6 of nieuwer binnen de 6.x-serie (`winget install JRSoftware.InnoSetup`). De previewtimer gebruikt de 32-bit wizard-ABI van Inno Setup 6; dit staat los van de x64 Windows-app die wordt meegeleverd.

```powershell
cd pc-app
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

Het resultaat komt in `dist\Qprints esp installer-<versie>.exe`. De versie haalt het script uit `TempMonitor.csproj`. Met `-OutputDir E:\` komt de setup op een andere plek terecht, bijvoorbeeld rechtstreeks op een USB-stick. `build-branding.ps1` genereert het woordmerk, pictogram en de welkomstafbeelding; `build-layout-previews.ps1` genereert de 31 donkere layoutvoorbeelden. `build-animated-previews.ps1` genereert daarnaast 40 beelden voor elk van de 23 bewegende layouts. De installer speelt deze lokaal af op de layoutpagina en stopt de timer bij pauzeren, verlaten van de pagina en afsluiten.

> **Verkenner loopt vast of de setup is "geblokkeerd"?** Op beheerde pc's met Microsoft Defender for Endpoint blokkeert de ASR-regel *"Block executable files from running unless they meet a prevalence, age, or trusted list criterion"* (`01443614-cd74-433a-b99e-2ecdc07bfc25`) zelfgebouwde, niet-ondertekende exe-bestanden. Installeer dan op een pc zonder die regel, of voeg een ASR-uitzondering of een code-signing-certificaat toe.

### Ontwikkelen / testen zonder installer

De installer-opstarttest draait zonder software of drivers te installeren en controleert alle 31 layoutvoorbeelden, de standaardkeuze, een bestaande keuze en een expliciete override:

```powershell
powershell -ExecutionPolicy Bypass -File tests\Test-InstallerStartup.ps1 -InnoCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Voer dit uit vanuit de projectmap met `firmware/`, `pc-app/` en `tests/`, nadat de installer-payload en layoutvoorbeelden gebouwd zijn.

`powershell -ExecutionPolicy Bypass -File tests\Test-Autostart.ps1` controleert taakregistratie, starten, verwijderen en foutafhandeling met mocks; er worden geen echte taken gewijzigd. Registratiefouten tijdens Setup worden gemeld en staan in `C:\ProgramData\TempMonitor\task-error.log`.

`tests/speedometer/needle-tests.cpp` controleert met compile-time asserts de FPS- en temperatuurschalen, onafhankelijke naalden, vloeiende overgangen, ontbrekende waarden en het omslaan van de apparaattimer. Compileer dit bestand met een C++17-compiler, bijvoorbeeld `g++ -std=c++17 -fsyntax-only tests/speedometer/needle-tests.cpp`.

`powershell -ExecutionPolicy Bypass -File tests\Test-PreviewFrames.ps1` controleert de 720 animatiebeelden op volledigheid, afmetingen en beeldvariatie. De installer-opstarttest controleert ook echte Windows-timerberichten, framewissels, pauzeren/hervatten en het stoppen bij het verlaten van de layoutpagina.

In een terminal **als administrator**, in de map `pc-app/`:

```powershell
dotnet run -- --list        # toont alle temperatuursensoren
dotnet run                  # zoekt het scherm automatisch (USB VID 303A) en stuurt elke seconde data
dotnet run -- --port COM5   # als automatisch zoeken niet lukt
```

Voor FPS moet `PresentMon.exe` naast `TempMonitor.exe` staan. Zonder dat bestand toont het scherm `--` bij FPS.

## Hoe de FPS gemeten wordt

De app start [PresentMon](https://github.com/GameTechDev/PresentMon) (Intel, open source) op de achtergrond en telt elke seconde de frames van het venster **op de voorgrond**. Speel je een game, dan zie je dus de FPS van die game. Tekent het voorgrondvenster niets (bijvoorbeeld het bureaublad), dan staat er `--`.

## Protocol

De pc stuurt elke seconde één tekstregel (115200 baud):

```
CPU=42.5;GPU=48.0;FPS=165
```

Een onbekende waarde wordt `nan`. Stuur je `PING`, dan antwoordt het scherm met `TEMPMON`.

Optioneel kan `;LAYOUT=` met een van de eenentwintig layoutnamen worden toegevoegd, bijvoorbeeld `;LAYOUT=arsenal`. Een afzonderlijke `LAYOUT=arsenal`-regel werkt ook, zonder de meetwaarden te veranderen. Onbekende layoutnamen worden genegeerd. Het scherm schrijft de keuze alleen naar het geheugen wanneer deze verandert.

## Aanpassen

Bovenaan `firmware/src/main.cpp`:

| Instelling | Standaard | Betekenis |
|---|---|---|
| `BRAND` | `"QPRINTS"` | merknaam bovenaan |
| `HOT_THRESHOLD` | `50.0` | grens tussen blauw en rood |
| `ROTATION` | `0` | beeld draaien (0–3) als de USB-C-poort anders ligt |
| `DATA_TIMEOUT_MS` | `3000` | na zoveel ms zonder data toont het scherm "verbinding verloren" |

Windows-venster testen: `dotnet run --project tests/SettingsWindow.Tests -c Release -- pc-app/installer` (eerst voorbeelden en branding genereren). Dit controleert selectie, opslaan, timer, pauzeren en hervatten zonder echte hardware of installatie te wijzigen.
