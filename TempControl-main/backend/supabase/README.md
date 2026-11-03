# Qscreen Supabase-project

Doelproject: `ybmesonqfclarqigwxfe`.

De eigenaar heeft de [dashboardlink](https://supabase.com/dashboard/project/ybmesonqfclarqigwxfe) aangeleverd. Inmiddels is de aangemelde, projectgebonden alleen-lezen MCP-verbinding daadwerkelijk getest. De API-origin in `project.json` is via het project bevestigd. Het bestand bevat geen API-key of geheim en activeert geen login in de Windows-app. Zie [de eerste voorbereide migratie](migrations/0001_account_foundation.sql). Inspectieresultaten en opgehaalde clientkeys worden niet als openbaar database-exportbestand gepubliceerd.

## Volgende uitvoering na koppeling

1. Inspecteer uitsluitend dit project: projectstatus, bestaande schema's, migrations, Auth-instellingen en Storage. Controleer eerst of er al andere gegevens of toepassingen zijn.
2. Bevestig de API-origin en openbare publishable key. Bewaar backendsecret keys uitsluitend als serversecrets. De desktopapp ontvangt geen secret/service-role key, databasewachtwoord of providerwachtwoord.
3. Bereid de account-/RLS-migratie uit `ACCOUNTS-ARCHITECTURE.md` voor tegen de werkelijk aanwezige databaseversie en schemastructuur. Gebruik geïsoleerde Qscreen-namen, een transactie en expliciete grants. Laat een eerdere of conflicterende migratie veilig weigeren.
4. Configureer e-mailbevestiging, rate limits, de echte voorwaarden/privacy-URL's en exacte bevestigings-/herstelredirects. Verzin geen werkende SMTP-configuratie of live domeinen.
5. Voer de misbruiktests uit `ACCOUNTS-SECURITY-TESTS.md` uit, zowel via de Qscreen-API als rechtstreeks via data- en Storage-API's. Het project is pas gereed wanneer die controles echt slagen.
6. Laat de eigenaar `Qprints@outlook.be` bevestigen en MFA instellen. Koppel daarna gecontroleerd de geverifieerde Auth-UUID aan de singleton-beheeridentiteit. Publieke registratie blijft altijd een gebruikersaccount opleveren.
7. Sluit pas de desktop-accountschermen en werkruimtemigratie aan op de geverifieerde configuratie; de bestaande hardwaremonitoring blijft functioneren tijdens de overgang.

Huidige status: verbinding geverifieerd; eerste accountmigratie voorbereid maar nog niet toegepast. Er is geen beheerder aangemaakt of verificatiemail verzonden. Een openbare project-URL of publishable key geeft geen databasebeheerrechten.

Referenties: [accountarchitectuur](../../ACCOUNTS-ARCHITECTURE.md), [beveiligingstests](../../ACCOUNTS-SECURITY-TESTS.md), [officiële Supabase API-keydocumentatie](https://supabase.com/docs/guides/getting-started/api-keys).
