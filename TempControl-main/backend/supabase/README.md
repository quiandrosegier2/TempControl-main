# Qscreen Supabase-project

Doelproject: `ybmesonqfclarqigwxfe`.

De eigenaar heeft de [dashboardlink](https://supabase.com/dashboard/project/ybmesonqfclarqigwxfe) aangeleverd. De projectgebonden MCP-verbinding is daadwerkelijk getest en na expliciete toestemming uitgebreid naar database lezen/schrijven. De OAuth-aanvraag is beperkt tot `projects:read`, `database:read` en `database:write`; er is geen functie- of opslagbeheer aangevraagd. De API-origin in `project.json` is via het project bevestigd. Het bestand bevat geen API-key of geheim en activeert geen login in de Windows-app. Zie [de eerste migratie](migrations/0001_account_foundation.sql). Inspectieresultaten en opgehaalde clientkeys worden niet als openbaar database-exportbestand gepubliceerd.

## Volgende uitvoering na koppeling

1. Inspecteer uitsluitend dit project: projectstatus, bestaande schema's, migrations, Auth-instellingen en Storage. Controleer eerst of er al andere gegevens of toepassingen zijn.
2. Bevestig de API-origin en openbare publishable key. Bewaar backendsecret keys uitsluitend als serversecrets. De desktopapp ontvangt geen secret/service-role key, databasewachtwoord of providerwachtwoord.
3. Bereid de account-/RLS-migratie uit `ACCOUNTS-ARCHITECTURE.md` voor tegen de werkelijk aanwezige databaseversie en schemastructuur. Gebruik geïsoleerde Qscreen-namen, een transactie en expliciete grants. Laat een eerdere of conflicterende migratie veilig weigeren.
4. Configureer e-mailbevestiging, rate limits, de echte voorwaarden/privacy-URL's en exacte bevestigings-/herstelredirects. Verzin geen werkende SMTP-configuratie of live domeinen.
5. Voer de misbruiktests uit `ACCOUNTS-SECURITY-TESTS.md` uit, zowel via de Qscreen-API als rechtstreeks via data- en Storage-API's. Het project is pas gereed wanneer die controles echt slagen.
6. Laat de eigenaar `Qprints@outlook.be` bevestigen en MFA instellen. Koppel daarna gecontroleerd de geverifieerde Auth-UUID aan de singleton-beheeridentiteit. Publieke registratie blijft altijd een gebruikersaccount opleveren.
7. Sluit pas de desktop-accountschermen en werkruimtemigratie aan op de geverifieerde configuratie; de bestaande hardwaremonitoring blijft functioneren tijdens de overgang.

Huidige status: verbinding geverifieerd; eerste accountmigratie toegepast als `20261103174355_qscreen_account_foundation`. De [database-autorisatietests](tests/0001_account_authorization.sql) zijn tegen het project uitgevoerd en geslaagd. De test gebruikt tijdelijke synthetische identiteiten, gesimuleerde providerclaims en een volledig teruggedraaide transactie. Dit bewijst de geteste databaseregels, niet de werkelijke e-mail-/MFA- of JWT-validatieflow bij de provider.

Gecontroleerd: alle vier accounttabellen hebben RLS; registratiegegevens geven geen beheerrechten; alleen de eigen bevestigde account kan het eigen profiel lezen/bewerken; blokkeren werkt; private status/rollen/audit zijn niet door gewone gebruikers te wijzigen; de vertrouwde bootstrap weigert ontbrekende bevestiging/MFA; de database staat maximaal één beheerder toe; de beheercontrole vereist de juiste gekoppelde identiteit en AAL2. Na de tests is bevestigd dat er geen testprofielen of beheerder zijn achtergebleven.

Er is geen echte beheerder aangemaakt of verificatiemail verzonden. Voor de echte beheeridentiteit zijn nog registratie, mailboxbevestiging en daadwerkelijke TOTP-verificatie nodig. E-mailinstellingen, voorwaarden/privacy-URL's, desktoplogin, veilige werkruimtes, officiële bibliotheek en beheer-API moeten nog worden aangesloten en apart getest. Een openbare project-URL of publishable key geeft geen databasebeheerrechten.

Referenties: [accountarchitectuur](../../ACCOUNTS-ARCHITECTURE.md), [beveiligingstests](../../ACCOUNTS-SECURITY-TESTS.md), [officiële Supabase API-keydocumentatie](https://supabase.com/docs/guides/getting-started/api-keys).
