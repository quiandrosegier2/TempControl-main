# Qscreen 2.2 — accountvenster in testversie

Deze testversie voegt een afzonderlijk **Qscreen Account**-programma toe. Het bestaande Qscreen, de 21 templates, eigen ontwerpen en de USB-monitor blijven bruikbaar. Aanmelden is nog geen verplichte toegangspoort tot de bestaande lokale Studio. Dit is een tussenstap naar de architectuur in `ACCOUNTS-ARCHITECTURE.md`, geen voltooide publieke accountrelease.

## Wat je kunt bekijken

Open **Account** rechtsboven in Qscreen, of **Qscreen Account** via Start. Het venster bevat aanmelden, een registratieformulier, eigen profielnaam, uitloggen en authenticator-instellingen. Identiteit en rol worden opgehaald via de geteste Supabase-RPC; een ingevuld beheeradres of een geverifieerde authenticator maakt iemand niet vanzelf beheerder.

De accountclient kan bestaande bevestigde accounts aanmelden en hun eigen naam wijzigen, sessies vernieuwen, bij de provider uitloggen en TOTP instellen/bevestigen. De client- en UI-tests gebruiken gesimuleerde providerantwoorden; deze functies zijn nog niet met een echte gebruiker doorlopen. In de database is nog geen echte beheeridentiteit gekoppeld. Een volledige cloudbibliotheek en beheerinterface zijn nog niet toegevoegd.

**Registratie en wachtwoordherstel zijn in deze build bewust uitgeschakeld.** De werkelijke Qscreen-bevestigings-/herstelpagina en de vastgestelde voorwaarden/privacy-informatie op autoslice.be zijn nog niet geplaatst en gekoppeld. Bovendien moet instemming nog met beleidsversies aan de backend worden verbonden. Alleen de openbare Auth-instellingen zijn gecontroleerd: e-mailaanmelding staat aan en automatische e-mailbevestiging uit. Dat bewijst geen werkende SMTP-bezorging.

## Procesgrens en sessies

Het accountprogramma heeft een eigen `asInvoker`-manifest en weigert wachtwoorden te verwerken als het toch met beheerdersrechten wordt gestart. Het bestaande monitorprogramma behoudt zijn sensorrechten. De Account-knop probeert via de Windows-desktopshell te openen in de gebruikerscontext. Als dat in een bijzondere Windows-configuratie toch verhoogd gebeurt, open dan Qscreen Account via Start zonder 'Als administrator uitvoeren'.

Zonder 'Sessie onthouden' blijven sessies in geheugen. Met deze optie wordt de sessie door Windows DPAPI voor de huidige Windows-gebruiker versleuteld opgeslagen onder `%LOCALAPPDATA%\QPRINTS\Qscreen\Accounts\session.dpapi`. Er worden geen plaintext tokens of wachtwoorden naar configuratie, logs, de monitor of firmware gestuurd. Uitloggen wist lokale sessiegegevens ook als een netwerkfout online intrekking verhindert. Rotatie wordt door één refreshverzoek tegelijk afgehandeld.

HTTP-aanvragen gaan uitsluitend naar het vastgelegde Qscreen-project, volgen geen redirects met credentials en hebben een tijd- en responslimiet. De verzonden appconfiguratie bevat uitsluitend een openbare `sb_publishable_`-key. Er worden geen geheime backendkeys meegeleverd.

Deze accountclient introduceert nog geen cloudsync, scheiding van alle bestaande projectbestanden of offline accounttoestemming. De huidige persoonlijke ontwerpen blijven in hun bestaande opslag; de latere niet-destructieve werkruimtemigratie blijft een afzonderlijke stap.

## Coolify en autoslice.be

De openbare AutoSlice-broncode is gecontroleerd en een beperkte patch staat onder `backend/account-portal/`. Zij voegt alleen de Qscreen-callback-/herstelpagina met beveiligingsheaders toe aan de bestaande Next.js-frontend. De bestaande AutoSlice-login en backend blijven apart. De statische pagina bewaart tokens alleen tijdelijk in geheugen, verwijdert URL-fragmenten direct en verifieert de gebruiker bij Supabase voordat zij succes toont of een wachtwoordwijziging toestaat.

Mijn huidige GitHub-aanmelding heeft geen schrijfrechten op `quiandrosegier96-coder/autoslice`; er is ook geen Coolify-verbinding beschikbaar. Er is daarom niet naar die repository gepusht en er is geen live website gewijzigd. Voor plaatsing is toegang tot de juiste AutoSlice-repository of de Coolify-deployment nodig. Zie [de precieze plaatsingsstappen](backend/account-portal/COOLIFY.md). De patch bevat geen databasewachtwoorden of geprivilegieerde keys.

## Uitgevoerde controles

- Clienttests: RPC-identiteit, geen beheerrecht door e-mailtekst of MFA alleen, ontbrekende callback/beleidsconfiguratie, foutafhandeling, gelijktijdige refresh, logout en sessieverval.
- Windows-tests: daadwerkelijke DPAPI-encryptie/teruglezen, afwijzen van een beschadigde sessie en rendering van het accountvenster. DPAPI is onder het echte Windows-profiel getest omdat de geïsoleerde sandbox geen geladen profielsleutel heeft.
- Webtests: ontbrekende/verlopen tokens, onbetrouwbare configuratie, onbevestigde gebruiker, echte providercontrole vóór succes, wachtwoordbevestiging, tokenopruiming en begrensde antwoorden. Dit zijn gesimuleerde browser-/providercontroles, geen live herstelmail.
- Build-/pakketcontroles: aparte self-contained processen, daadwerkelijke uitvoerbare manifests en registratiegating. Bestaande taak- en uploadercontroles blijven van toepassing.

Werkelijke e-mailverificatie, herstel, MFA met de eigenaar, de website-deployment en de hele accountflow moeten nog worden getest voordat registratie publiek kan worden ingeschakeld. Firmware opnieuw uploaden is voor dit accountvenster niet nodig.
