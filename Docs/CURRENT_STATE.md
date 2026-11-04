# Ist-Stand (nur belegte Fakten)

**Datum:** 2026-11-04 · **Meilenstein:** M0 abgenommen (2026-11-04, manuelle Abnahme laut Nutzer)

## Tatsächlich geprüft
- `dotnet test` (Linux-Cloud, .NET 8.0.133): **24/24 Tests bestanden**. Unity-freie Assemblies bauen mit `TreatWarningsAsErrors` gegen netstandard2.1/C# 9 (wie Unity).
- Szenario Seed 12345, 1 Jahr: alle drei Siedlungen bauen 2 Häuser, niemand hungert, Lager laufen bei ~294/300 voll (Wachstum stagniert ohne Spieler-Lager → so gewollt, Inspektor zeigt Hinweis).
- Unity 6000.6.0f1 auf dem Windows-PC des Nutzers, `Tools\run-unity-tests.ps1` (2026-11-04, 15:32, mit schlankem Paketmanifest nach ADR-011): Einrichtung, EditMode, PlayMode, Windows-Build und Build-Smoke jeweils **Exitcode 0**.
  - EditMode **24/24**, PlayMode **3/3** bestanden.
  - Build-Smoke (Seed 12345): Lager nach 30 Tagen fertig, Haushalt im Haus, Zustands-Hash vor Speichern und nach Laden gleich (`30b39c7cdcde05b1`), Screenshot erzeugt.
- Erster Lauf davor: `Selection_RaycastHitsPersonPlaceholder` schlug fehl. Nach `Physics.SyncTransforms()` in `EntityViews.Animate` bestanden; die Ursache (Collider nicht nachgezogen) ist daraus abgeleitet, nicht einzeln nachgemessen.

## Manuell (Aussagen des Nutzers)
- 2026-11-04 15:34: `WorldBeyond.exe` läuft auf dem PC des Nutzers, Nutzer meldet „Es geht“. Welche Einzelschritte geprüft wurden, ist nicht protokolliert.
- 2026-11-04 15:39: Nutzer meldet „Habe es getestet geht“, wieder ohne Einzelangaben. Auf der Platte belegt: `slot1.wbsave` um 15:38 im laufenden Spiel gespeichert, keine Exception im Spiel-Log bis 15:39.
- 2026-11-04 17:57: Nach der Anleitung (in slot1 speichern, Spiel schließen, neu starten, slot1 laden, Datum/Bestände vergleichen) meldet der Nutzer „Ja klappt“. Damit gilt Speichern → Neustart → Laden als manuell geprüft. Belege: Aussage des Nutzers; kein Screenshot, keine Werteliste.

## Bekannte Risiken
- `BillingMode.json` (Purchasing-Überbleibsel) entfernt in 1827cd4; danach kein neuer Unity-Lauf (reine Löschung einer unreferenzierten Datei).
- Aktiv ist das alte Input-System (`activeInputHandler: 0`), obwohl das Input-System-Paket installiert ist.
- M0-Vereinfachungen siehe DECISIONS ADR-009.

## Nächster Schritt
M1 „Siedlung wächst nachvollziehbar“: Berufe, Produktionsketten, Bauaufträge mit Arbeit für Straßen, Parzellen, Bedürfnisse, saisonale Erträge, Baupriorität, einfache Forschung, Statistik. Vorher: Branch `unity-fixes-m0` nach `main` mergen (Pull Request, Entscheidung des Nutzers).
