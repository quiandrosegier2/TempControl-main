# Qscreen accountbeveiliging — geplande acceptatietests

Status: testmatrix, nog niet uitgevoerd tegen een accountbackend. Geen productiebeveiliging als geslaagd markeren zonder de werkelijke testresultaten.

Gebruik een apart stagingproject, twee gewone accounts A/B, de bevestigde beheereigenaar en een onbevestigd account. Gebruik expliciete testgegevens; geen echte privéprojecten of gebruikerswachtwoorden in logs/fixtures.

| Onderdeel | Te testen aanval of fout | Vereist resultaat |
|---|---|---|
| Registratie | Zelfde e-mail met andere hoofdletters/spaties, parallelle aanvragen | Providerhandhaving, geen dubbele effectieve identiteit |
| Rol bij registratie | `role=admin`, `is_admin=true`, admin-e-mail in metadata | Geen beheerrechten, ook buiten de app om |
| Beheerprovisionering | Onbevestigd adres, verkeerde UUID/e-mail, geen geverifieerde MFA | Geen singleton-beheerder aangemaakt |
| Eén beheerder | Twee gelijktijdige provisioneringspogingen | Database staat maximaal één identiteit toe |
| E-mailbevestiging | Onbevestigde JWT direct tegen API en PostgREST/Storage | Geen volledige toegang |
| Voorwaarden | Rechtstreekse signup zonder actueel toestemmingsrecord | Accountvoltooiing/toegang volgens vastgelegd beleid geweigerd |
| Login/herstel | Onjuist wachtwoord, herhaalde aanvragen, onbekend adres | Begrensd, geen secrets of onnodige accountonthulling |
| Herstelcallback | Vervallen/hergebruikte link, afwijkende redirect-origin | Geen accountovername of open redirect |
| MFA | Admin met alleen wachtwoord/AAL1, verwijderde factor | Beheer geweigerd |
| MFA-versheid | Alleen refresh vernieuwt token-iat, oude AAL2-sessie | Gevoelige actie vraagt echte nieuwe MFA |
| Tokenvalidatie | Vervalste/verlopen JWT, verkeerde issuer/audience, ontbrekend subject | Geweigerd vóór gegevensactie |
| Sessies | Parallelle refresh, ingetrokken refresh token, logout, sessieverval | Geen onbeveiligde fallback; tokenmateriaal verwijderd/atomisch vervangen |
| Tokenopslag | JSON/config/log/package/IPC/installer inspecteren | Geen plaintext wachtwoorden, tokens of serversecrets |
| Windows-opslag | Andere Windows-gebruiker leest sessiebestand | ACL/DPAPI blokkeren; limiet binnen dezelfde Windows-login vastgelegd |
| Beheer-API | User A roept CRUD/publish/delete/userbeheer/audit direct aan | 403/veilige afwijzing, geen wijziging |
| RLS-omweg | Zelfde aanvallen rechtstreeks via publieke data-/Storage-API | Dezelfde afwijzing als via de app-API |
| Rol-/statusupdate | PATCH eigen profiel met role/disabled/owner_id | Verboden kolommen geweigerd; privileges ongewijzigd |
| Accountblokkade | Geldig oud access token na blokkeren van A | Volgende online aanvraag geweigerd |
| Beheer-e-mailwijziging | Admin-e-mail veranderd of onbevestigd | Geen beheeractie tot gecontroleerd herstel |
| IDOR | A leest/wijzigt/wist project, asset, apparaat of upload van B | Geen toegang, ook met bekende UUID/objectkey |
| Privacy | Beheerder vraagt privéproject van gewone gebruiker op | Geen impliciete toegang tot inhoud |
| Officiële concepten | User leest ongepubliceerde versie, thumbnail of objectkey | Geen metadata-/assetlek |
| Upload | ZIP-slip, extra entries, dubbelen, zipbom, te veel werkelijk gelezen bytes | Afwijzing, geen extractie/uitvoering/restbestanden |
| Afbeeldingen | Valse extensie/MIME, extreme dimensies, excessieve GIF-frames, corrupte bytes | Server en client wijzen af binnen resourcegrenzen |
| Referenties | Externe URL, padtraversal, object van een andere eigenaar | Afwijzing, geen SSRF of willekeurige bestandstoegang |
| Publicatie | Gelijktijdige versies/overschrijven, falende opslag/database | Geen half-gepubliceerde versie; consistente audit |
| Intrekken | Unpublish/delete met bestaande cache of signed URL | Nieuwe downloads geblokkeerd; reeds verkregen kopieën/URL-verval eerlijk beschreven |
| Integriteit | Package of cache veranderd na vertrouwde metadata | Hash/schema faalt; bestaande goede cache blijft behouden |
| Audit | User schrijft events, admin probeert te wissen, secrets in payload | Geen frontendmutatie; secrets geredigeerd |
| Statistieken | Geen analytics-toestemming of registratie | Geen automatische hardware/FPS/proces-/MAC-upload |
| Offline | Netwerkverlies, oude gecachte adminrol, verlopen toestemming, klokterugzetting | Alleen toegestaan lokaal gedrag; nooit offline publiceren |
| Migratie | Annuleren, ruimtegebrek, corrupt project, tweede account | Bron behouden, geen stilzwijgende toewijzing of upload |
| Hardwarehelper | Andere SID, te groot frame, shelltekst, bestandspad via IPC | Afwijzing; geen algemene verhoogde uitvoer- of schrijffunctie |
| Windows-sessies | Wisselen gebruiker, UAC, standaardgebruiker, dubbele companion | Duidelijke eigenaar en geen verkeerde werkruimte of COM-conflict |
| Regressie | UI sluiten, pc herstarten, profielen/pagina's, import/export, unplug/replug | Bestaande monitoring, ontwerpen en FRAME2 blijven werken |
| Installer | Upgrade met bestaande ontwerpen, startupregistratie en starten van app/helper | Geen gegevensverlies of privilegefout; juiste procesgrenzen |

Resultaten krijgen een vastlegging met build/migratieversie, omgeving, verwachte en werkelijke uitkomst. Testartefacten bevatten geen sessiesleutels. Vrijgave vereist ook echte verificatie-/herstelmail en MFA met de eigenaar; providerfixtures bewijzen dat niet.
