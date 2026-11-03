# Qscreen Studio 2.0 — testversie

Deze uitbreiding bouwt voort op de bestaande Windows-app, ontwerper en 31 ingebouwde layouts. De eerste uitgave is **2.0.0-rc.1**: een publieke testversie. Versie 1.6.0 blijft de stabiele download.

## Beginnen

Installeer `Qscreen-2.0.0-rc.1.exe` en open **Qscreen** via Start. Open **Profielen & apparaten** voor het nieuwe Studio-beheer. De achtergrondmeting blijft actief als je het venster sluit. Ontwerpen en instellingen staan op de pc; de USB-verbinding blijft nodig.

Voor touchscreenbediening, helderheid en de energiebesparende verbinding is de meegeleverde firmware 2.0 nodig. Open **Firmware installeren**, kies bewust de COM-poort van een Waveshare ESP32-S3-Touch-LCD-1.46 en bevestig de upload. De app kiest geen poort voor je. Uploaden vervangt de firmware op dat apparaat. Bestaande firmware 1.5.1 blijft eenvoudige eigen ontwerpen ontvangen; de app toont welke geavanceerde functies ontbreken.

## Profielen en automatisch wisselen

Maak, dupliceer, importeer en bewerk profielen op het tabblad **Profielen**. Importeren maakt een nieuw profiel. Kies een standaardprofiel. Onder **Automatisch wisselen** voeg je regels toe voor een voorgrondprogramma, een actief proces of inactiviteit. Gebruik een uitvoerbare bestandsnaam, bijvoorbeeld `game.exe`, en kies profiel en prioriteit. Een instelbare wachttijd voorkomt snel heen en weer schakelen. Als het programma stopt, keert de selectie na de wachttijd terug naar de standaard.

Handmatig activeren blijft gelden totdat je **Automatisch hervatten** kiest. Een bestaand ontwerp of een ingebouwde layout toepassen activeert eveneens een handmatige keuze. Klik na het wijzigen van regels op **Instellingen bewaren**.

## Pagina's en beweging

Open **Pagina's** in de ontwerper. Voeg pagina's toe, dupliceer ze, wijzig hun naam, bewerk hun inhoud en verander de volgorde. Miniaturen tonen iedere pagina. Stel automatische rotatie, interval en overgang in. De eerste pagina blijft compatibel met bestaande projecten; de profielnaam en paginanaam zijn afzonderlijk.

Iedere pagina kan een eigen achtergrond en vectoranimatie hebben. Achtergrondimport ondersteunt ook begrensde GIF-animaties. De pc tekent steeds het actuele beeld. De USB-overdracht bepaalt de uiteindelijke snelheid: dit is geen videospeler. Overgangen bestaan uit een tussenbeeld en het doelbeeld, zodat een overgang de verbinding niet met een lange beeldreeks blokkeert.

## Touchscreen

Veeg voor vorige/volgende pagina. Lang indrukken opent een tijdelijk snelmenu op het scherm. Via **Aanraakzones** voeg je rechthoekige tikgebieden toe voor navigatie, een specifieke pagina of dimmen. Paginanummers in deze editor beginnen bij nul. Doelpagina's worden aangepast als je pagina's herschikt of verwijdert.

Per apparaat stel je inschakeling, veegafstand, indruktijd, gevoeligheid, rotatie en visuele feedback in. Het eerste tikje vanuit slaapstand wekt het scherm. Touch vereist een gedetecteerde controller en nieuwe firmware. De registerafhandeling is gebaseerd op het [officiële Waveshare-voorbeeld](https://github.com/waveshareteam/ESP32-S3-Touch-LCD-1.46/tree/fda89ff2ff32eb1bce561901225c6a36fa684237/example/Arduino-3.1.1/examples/LVGL_Arduino).

## Sensorhistorie

Het tabblad **Historie** toont CPU/GPU-temperatuur, belasting en kloksnelheid, RAM en FPS over 1, 5, 15 of 60 minuten, met minimum, maximum en gemiddelde. De achtergrondmeting verzamelt één gedeelde reeks met maximaal 3.601 metingen. Stel het meetinterval in; ontbrekende waarden blijven gaten in de grafiek. Historie wordt in geheugen bewaard en begint opnieuw als de achtergrondmeting herstart.

Voeg een **HistoryGraph**-element toe aan een ontwerp en kies meetwaarde en tijdvenster. De voorbeeldweergave gebruikt testwaarden; op het aangesloten scherm worden echte beschikbare sensoren gebruikt. Sensorbeschikbaarheid hangt af van hardware, drivers en PresentMon.

## Lokale DesignHub

De DesignHub bevat lokale voorbeelden in Gaming, Minimal, Cyberpunk, Productivity, Hardware en Clock. Zoek, filter en sorteer ontwerpen; selecteer eerst een ontwerp voor de volledige preview. Importeer het vervolgens als nieuw profiel. Exporteer een draagbaar `.qscreenpack` met ontwerp, afbeeldingen, metadata en preview.

Import controleert bestandsnamen, grootte, doelmodel en versie en toont een bevestigingspreview. Pakketten worden niet uitgepakt of uitgevoerd. Lettertypen worden niet gebundeld: op een andere pc kan een vervangend lettertype verschijnen. Dit is een lokale bibliotheek; er is geen online account, uploadserver of communitydienst aangesloten. De code bevat een interface voor een latere dienst.

## Helderheid en slaapstand

Selecteer een apparaat onder **Apparaten**. Gebruik de schuifregelaar en optioneel direct toepassen. In de eigenschappen stel je dag/nachturen, helderheid, minimum/maximum, dimmen na inactiviteit en slaapstand in. Pc- of aanraakactiviteit herstelt de ingestelde helderheid. De firmware gebruikt de echte achtergrondverlichting via PWM. Er wordt geen omgevingslichtsensor verondersteld.

## Waarschuwingen

Voeg onder **Waarschuwingen** regels toe voor hoge/lage waarden, ontbrekende sensoren of een verbroken verbinding. Stel drempel, hysterese, prioriteit, duur, cooldown, kleur, pulseren, tijdelijke schermoverlay en Windows-melding in. De CPU-voorbeelden zijn aanpasbaar. Meldingen veranderen het opgeslagen ontwerp niet. Een verbroken scherm kan alleen op de pc worden gemeld. Dit zijn informatieve meldingen; Qscreen bestuurt geen hardwarebeveiliging.

## Meerdere schermen

Ieder herkend scherm krijgt een onafhankelijke verbinding, naam, profiel, paginakeuze, touchinstellingen en helderheidsbeleid. Een vaste profielkeuze per apparaat kan automatisch meewisselen uitschakelen. USB-identiteit en, met nieuwe firmware, de chipidentiteit vervangen vluchtige COM-nummers als herkenning. Een trage of losgekoppelde display blokkeert de overige displays niet. Opnieuw verbinden wordt automatisch geprobeerd.

Het bestaande hoofdvenster toont de levering aan het primaire scherm. Gebruik **Apparaten** voor de afzonderlijke status van alle schermen. Firmware uploaden blijft een expliciete handeling voor één geselecteerde poort.

## Bestanden, herstel en teststatus

Bestaande `.qscreen`-projecten blijven leesbaar. Profielen staan afzonderlijk in `profiles`, instellingen in `studio-settings.json`, lokale pakketten in de lokale bibliotheek. Bij de eerste overschrijving van een bestaand `custom-design.json` of `custom-draft.json` maakt de app een `.pre-2.0.bak`. Bewaar daarnaast een eigen export van belangrijke ontwerpen. De bestaande installatiemap en achtergrondtaak blijven behouden voor upgrades.

De softwaretests controleren profielkeuze, paginasortering, geschiedenis, waarschuwingen, pakketimport, GIF-weergave, onafhankelijke gesimuleerde apparaten, reconnects en Windows-vensters. Firmware en installer worden gebouwd en gecontroleerd. **Touchcoördinaten, aanrakingen en PWM op een fysiek scherm zijn nog niet geverifieerd.** Gebruik deze testrelease om dat met het juiste apparaat te beoordelen. Installeer bij problemen de [stabiele 1.6.0-release](https://github.com/quiandrosegier2/TempControl-main/releases/tag/v1.6.0) opnieuw; bewaar vooraf je exports.
