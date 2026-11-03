# Qscreen Studio 2.1 — Premium Templates

De nieuwe sectie **Templates** bevat 21 originele QPRINTS-ontwerpen voor het ronde WS146-scherm van 412 × 412. De bestaande 31 firmwarelayouts, ontwerper, DesignHub en USB-overdracht blijven beschikbaar. De eerste uitgave is een testrelease: `2.1.0-rc.1`.

## Kiezen en aanpassen

Vanaf rc.2 toont de installer geen layoutkeuzestap meer. Kies na installatie je ontwerp in Qscreen. Een app-update bewaart de bestaande layoutinstelling; alleen een nieuwe installatie zonder instelling krijgt de standaardlayout.

Open Qscreen via Start en kies **Templates**. Zoek op naam, filter op categorie of toon alleen favorieten/geanimeerde ontwerpen. Iedere kaart heeft een rond voorbeeld en knoppen voor **Voorbeeld**, **Toepassen**, **Dupliceren** en **Favoriet**. Favorieten worden lokaal bewaard. Open een bewegend voorbeeld om de animatie vooraf te bekijken of te pauzeren. De preview gebruikt expliciet gelabelde demowaarden; het fysieke scherm gebruikt de bestaande sensormeting.

**Toepassen** opent eerst het voorbeeld. Na bevestiging maakt de app een nieuw profiel aan en activeert het handmatig. Het huidige projectbestand wordt niet overschreven. Apparaten die vast aan een ander profiel zijn gekoppeld blijven dat profiel gebruiken; controleer **Profielen & apparaten** voor de actuele ontvangst per scherm.

Kies **Bewerken als kopie** in het voorbeeld om een nieuw profiel in de ontwerper te openen. Alle teksten, sensoren, panelen, meters en klokken zijn afzonderlijke elementen. Verplaats, vergroot/verklein, verwijder of voeg elementen toe. Lettertype, kleur, transparantie, meterbron, maximum, boog, lijndikte, segmenten en wijzer zijn instelbaar in de eigenschappen. Fonts worden niet gebundeld: Windows gebruikt een vervanger als een gekozen font ontbreekt.

Met **Als template** exporteer je een `.qscreenpack` en bewaar je dezelfde aangepaste versie in de lokale bibliotheek. **Dupliceren** maakt rechtstreeks een nieuwe lokale template. Deze lokale kopieën verschijnen ook in Templates. Originele QPRINTS-templates worden nooit aangepast.

## De collectie

| # | Template | Eigen kenmerken |
|---|---|---|
| 01 | Gaming Core | Roze/cyaan perimeter, grote FPS, sensorkaarten |
| 02 | Cyberpunk City | Eigen neonstad, skyline, digitale klok |
| 03 | Minimal Pro | Twee meetbogen, zwart, wit en rustige typografie |
| 04 | System Rings | Drie concentrische meters voor CPU/GPU/RAM |
| 05 | Frost Glass | Blauwe berglagen en transparante panelen |
| 06 | Animated Waves | Vloeiende blauwe/paarse golven |
| 07 | Hardware Pro | GPU-ventilatoren, temperatuur, load en kloksnelheden |
| 08 | Retro Pixel | Pixelzon, berglandschap, monospace tekst |
| 09 | Analog Hybrid | Analoge wijzerplaat met roze secondewijzer |
| 10 | Anime Style | Originele anonieme reiziger in een zonsondergang |
| 11 | Space Explorer | Planeten, sterren en een astronauthelm |
| 12 | Matrix Terminal | Digitale regen en groen terminalvenster |
| 13 | Performance Gauge | Halve FPS-meter, segmenten en wijzer |
| 14 | Minimal Clock | Monochrome bergkam, klok en datum |
| 15 | RGB Pulse | Verlopende kleurrand en echte FPS-historiegrafiek |
| 16 | Car Dashboard | Rode toerentellerschaal en centrale FPS |
| 17 | Weather + System | Configureerbaar echt weer en dag/nachtweergave |
| 18 | Game HUD | Hoekige cyaanpanelen en bewegende scanboog |
| 19 | Aurora | Noorderlicht, dennen en donkere horizon |
| 20 | Clean Dashboard | Drie even grote ronde systeemmeters |
| 21 | Tactical FPS | Origineel pantsermotief, rook en rode accenten |

De decoratieve illustraties zijn origineel: procedureel getekend, met een afzonderlijke AI-gegenereerde achtergrond voor Tactical FPS. Er zijn geen officiële gamepersonages, logo's of gekopieerde referentiebeelden opgenomen. Functionele elementen zijn niet in achtergronden ingebakken. Achtergronden kun je vervangen of verwijderen. De gebruikte tool en volledige prompt staan bij de [tactische asset](pc-app/assets/ARTWORK.md).

## Animatie en kleurregels

Onder de achtergrondinstellingen kies je de animatie, snelheid (0,1–5) en **Animaties inschakelen**. Per element kun je pulseren, draaien, ademen of kleurwisseling kiezen en het tempo aanpassen. Bij een kleurrand bepalen de voorgrondkleur en tweede kleur het palet. Ronde meters en balken interpoleren nieuwe waarden gedurende maximaal een halve seconde. Ontbrekende metingen geven een lege meter en `--` bij het bijbehorende tekstveld.

Selecteer een element en kies **Kleurregels**. De hoogste bereikte minimumdrempel bepaalt de kleur. Het CPU-voorbeeld gebruikt blauw onder 60 °C, oranje vanaf 60 °C en rood vanaf 80 °C; de hoogste band kan pulseren. Regels werken voor tekst, balken en ronde meters, zowel in de demopreview als bij echte metingen. De afzonderlijke systeemwaarschuwingen hebben voorrang op deze kleuren.

Animaties worden op de pc getekend en met het bestaande gecontroleerde FRAME2-protocol verstuurd. Slechts het huidige frame blijft op de ESP32; er worden geen animatiefilms of volledige templatebibliotheken op opgeslagen. De USB-snelheid bepaalt de daadwerkelijke beeldfrequentie. Een begrensde wachtrij gooit verouderd werk weg; gelijke frames worden niet opnieuw verzonden als de firmware keepalive ondersteunt. Voor rustige weergave schakel je animaties uit: dezelfde template werkt dan met vaste accenten en normale meetwaarde-updates. Qscreen is geen videospeler.

## Weer configureren

Open **Templates > Weer instellen**. Schakel Open-Meteo bewust in en vul latitude/longitude in. Zonder configuratie, bij een fout of te oude gegevens verschijnt `--` en een niet-beschikbaarpictogram. De preview toont altijd deze onbeschikbare weerstatus, geen verzonnen weersverwachting.

De provider krijgt de ingestelde coördinaten via HTTPS. Er is maximaal één verzoek tegelijk, met een tijdslimiet en een interval van tien minuten. De achtergrondmeting wacht niet op internet. De respons levert temperatuur, WMO-weercode en dag/nachtstatus. Het weerpictogram en optionele achtergrondtint passen zich aan dag/nacht aan. Gegevens worden aan hun brontijd getoetst en niet langdurig als actueel gepresenteerd. Bron en gebruiksvoorwaarden: [Open-Meteo](https://open-meteo.com/en/docs), weergegevens onder CC BY 4.0. Controleer de providervoorwaarden voor commercieel gebruik. Er is geen automatische locatiebepaling.

## Opslag en compatibiliteit

Projectschema 1 blijft behouden met optionele nieuwe eigenschappen. Templates gebruiken het bestaande versie-1 `.qscreenpack` met een stabiel template-ID, naam, categorie, beschrijving, canvasmaat, minimale appversie en ingebedde assets. Functionele sensorbindingen, animaties en kleurregels staan in het bewerkbare ontwerp. Nieuwe templates vereisen app 2.1. Oude projecten en pakketten blijven leesbaar.

De 21 ingebouwde ontwerpen worden één keer per app-proces aangemaakt; ze delen hun originele achtergronddata bij het maken van een kopie. De galerij leest voor lokale pakketten alleen metadata en begrensde miniaturen. Pas bij openen wordt het volledige ontwerp geladen. Bestandsnamen, manifest, canvas, assetreferenties, archiefgrootte en ontwerpeigenschappen worden gecontroleerd; pakketten worden niet uitgepakt of uitgevoerd.

Deze uitbreiding verandert de schermfirmware niet. Eenvoudige eigen ontwerpen vereisen FRAME2-firmware 1.5.1 of nieuwer. Voor touch, helderheid en keepalive blijft firmware 2.0 nodig. Een template toepassen vereist geen nieuwe firmware-upload als deze functies al beschikbaar zijn.

## Verificatie

De tests renderen alle 21 ontwerpen, controleren verschillende beelden en animatieframes, ontbrekende sensoren, RGB565, bewerkbare kopieën, kleurregels, transparantie, meterovergangen en pakketexport/import. Er zijn Windows-venstercontroles voor galerij en ontwerper, inclusief favorieten. De weerparser is gecontroleerd met een fixture en een echte providerrespons op testcoördinaten; gebruikersinstellingen werden daarbij niet veranderd. Bestaande ontwerp-, transport-, profiel-, reconnect- en UI-tests blijven van toepassing.

**De nieuwe templates zijn nog niet op het fysieke scherm beoordeeld.** Renders en gesimuleerde USB-controles bewijzen de softwarewerking; ze vervangen geen controle van leesbaarheid, animatiesnelheid en kleurweergave op het apparaat. Daarom blijft deze uitgave een prerelease.
