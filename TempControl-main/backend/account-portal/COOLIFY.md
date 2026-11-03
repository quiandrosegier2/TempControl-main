# Qscreen accountpagina op autoslice.be (Coolify)

Dit is een voorbereid statisch callback-/herstelportaal, nog niet gepubliceerd. Het is een toevoeging aan de bestaande AutoSlice Next.js-frontend, geen vervanging van de AutoSlice-login of backend. De geraadpleegde publieke bron is `quiandrosegier96-coder/autoslice`; bevestig vóór plaatsing dat Coolify werkelijk die repository en branch deployt.

## Bestanden plaatsen

De concrete patch `autoslice-qscreen-pages.patch` bevat de nieuwe bestanden en de beveiligingsheaders. Pas deze toe op een schone checkout van de gecontroleerde AutoSlice-bron met `git apply --check` en daarna `git apply`. Controleer de verschillen vóór de normale Coolify-deployment. De patch is lokaal in omgekeerde richting gecontroleerd tegen de voorbereidende checkout; er is niet naar de AutoSlice-repository gepusht.

Kopieer de inhoud van `public/qscreen/account/` naar de gelijknamige map onder `frontend/public/` van AutoSlice. `config.js` bevat uitsluitend de openbare Qscreen-projectorigin en publishable key. Het Windows-accountprogramma gebruikt hetzelfde aparte Supabase-project; AutoSlice-accounts blijven apart.

De uiteindelijke callback wordt `https://www.autoslice.be/qscreen/account/index.html`. De huidige AutoSlice Dockerfile kopieert `public/` al naar de frontendcontainer. Een normale Coolify-rebuild kan de bestanden dus publiceren; de bestaande backendomgeving en data hoeven hiervoor niet te worden aangepast.

Voeg aan de bestaande `headers()`-lijst in `frontend/next.config.ts` een regel voor `/qscreen/account/:path*` toe met:

- `Cache-Control: no-store`
- `Referrer-Policy: no-referrer`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy: default-src 'none'; script-src 'self'; style-src 'self'; connect-src https://ybmesonqfclarqigwxfe.supabase.co; base-uri 'none'; form-action 'none'; frame-ancestors 'none'`

De HTML bevat ook een beperkende CSP, maar `frame-ancestors` vereist een echte HTTP-header. Plaats geen analytics of derde-partijscripts op deze route: tijdelijke sessietokens uit de e-maillink worden direct uit de zichtbare URL verwijderd en blijven alleen in geheugen. Verificatie gebeurt via de Auth-provider, nooit alleen door tekst of claims in de URL te vertrouwen.

## Pas activeren na echte controles

1. Verifieer HTTPS, inhoud en beveiligingsheaders op de daadwerkelijk gedeployde route.
2. Voeg uitsluitend deze exacte callback aan Supabase Auth Redirect URLs toe; controleer ook Site URL, verificatie-e-mail en herstelmail. Geen wildcardredirects.
3. Controleer of de mailprovider echte gebruikers kan bereiken; de openbare Auth-instellingen bewijzen geen werkende SMTP-bezorging.
4. Publiceer door de eigenaar vastgestelde Qscreen-voorwaarden en privacy-informatie onder echte URL's. De huidige AutoSlice-footerlink is geen bewijs dat Qscreen-voorwaarden bestaan.
5. Sla instemming serverzijdig met beleidsversies op voordat registratie voor publieke gebruikers wordt ingeschakeld.
6. Voer een echte registratie, verificatie en herstel uit met de eigenaar. Pas daarna `RegistrationEnabled`, `TermsUrl`, `PrivacyUrl` en `RedirectUrl` in de desktopconfiguratie aan. Tot dan blijven registratie en herstel eerlijk uitgeschakeld.

De pagina kan e-mailbevestiging afronden en een herstelwachtwoord bij de provider zetten. Zij verleent geen beheerrechten en bewaart geen tokens in localStorage, cookies of logs. Browser-, provider- en productietests staan nog open totdat de route is gedeployd.
