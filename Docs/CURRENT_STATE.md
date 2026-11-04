# Ist-Stand (nur belegte Fakten)

**Datum:** 2026-11-04 · **Meilenstein:** M0 abgeschlossen (Übergabe zur Prüfung)

## Tatsächlich geprüft
- `dotnet test` (Linux-Cloud, .NET 8.0.133): **24/24 Tests bestanden**. Unity-freie Assemblies bauen mit `TreatWarningsAsErrors` gegen netstandard2.1/C# 9 (wie Unity).
- Szenario Seed 12345, 1 Jahr: alle drei Siedlungen bauen 2 Häuser, niemand hungert, Lager laufen bei ~294/300 voll (Wachstum stagniert ohne Spieler-Lager → so gewollt, Inspektor zeigt Hinweis).
- Unity 6000.6.0f1 auf dem Windows-PC des Nutzers, `Tools\run-unity-tests.ps1` (2026-11-04, 15:32, mit schlankem Paketmanifest nach ADR-011): Einrichtung, EditMode, PlayMode, Windows-Build und Build-Smoke jeweils **Exitcode 0**.
  - EditMode **24/24**, PlayMode **3/3** bestanden.
  - Build-Smoke (Seed 12345): Lager nach 30 Tagen fertig, Haushalt im Haus, Zustands-Hash vor Speichern und nach Laden gleich (`30b39c7cdcde05b1`), Screenshot erzeugt.
- Erster Lauf davor: `Selection_RaycastHitsPersonPlaceholder` schlug fehl. Nach `Physics.SyncTransforms()` in `EntityViews.Animate` bestanden; die Ursache (Collider nicht nachgezogen) ist daraus abgeleitet, nicht einzeln nachgemessen.

## Manuell geprüft (Aussage des Nutzers)
- 2026-11-04, `Builds\Windows\WorldBeyond.exe`: Nutzer hat die vier Schritte (Person anklicken → Inspektor, B → Lager bauen, Tempo 1–4/Pause, F5 → Neustart → F9) ausgeführt und meldet „Es geht“. Einzelergebnisse je Schritt wurden nicht separat protokolliert; kein Screenshot davon.

## Bekannte Risiken
- `BillingMode.json` (Purchasing-Überbleibsel) entfernt in 1827cd4; danach kein neuer Unity-Lauf (reine Löschung einer unreferenzierten Datei).
- Aktiv ist das alte Input-System (`activeInputHandler: 0`), obwohl das Input-System-Paket installiert ist.
- M0-Vereinfachungen siehe DECISIONS ADR-009.

## Nächster Schritt
M1 „Siedlung wächst nachvollziehbar“: Berufe, Produktionsketten, Bauaufträge mit Arbeit für Straßen, Parzellen, Bedürfnisse, saisonale Erträge, Baupriorität, einfache Forschung, Statistik. Vorher: Branch `unity-fixes-m0` nach `main` mergen.
