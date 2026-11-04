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

## M1 Schritt 1 (Branch m1-wachstum) – Stand 2026-11-04
- `dotnet test`: **30/30 bestanden** (24 alte + 6 neue M1-Tests). Szenario Seed 12345, 3 Jahre: 18 → 30 Personen, 6 → 12 Haushalte, 12 Häuser, 12 Geburten, 0 Tode, alle drei Siedlungen „Wächst“.
- Unity 6000.6.0f1 auf dem PC des Nutzers (2026-11-04 19:12, Ausgabe vom Nutzer kopiert): Einrichtung, EditMode, PlayMode, Build, Build-Smoke jeweils Exitcode 0; **EditMode 30/30, PlayMode 3/3**.
- Nicht geprüft: das neue Statistik-Panel und die Berufsanzeige im echten Spiel (kein manueller Test, kein Screenshot ausgewertet).
- Die `.meta`-Dateien der neuen Skripte (Jobs, Lifecycle, SettlementPlanner, SaveFixtures, M1Tests) hat Unity auf dem PC erzeugt; sie sind noch nicht im Repo.

## M1 Schritt 2 (Branch m1-strassen-felder) – Stand 2026-11-04
- `dotnet test`: **33/33 bestanden** (3 neue: v2-Migration, Feldzyklus mit Save/Load in der Saison, KI-Felder; Straßentest auf Bau mit Arbeit umgestellt). Feld des Spielers erreicht zum Herbstbeginn das Maximum 600 und wird danach geerntet.
- Unity: **noch nicht geprüft** (neue UI: Feld-Knopf/F, Feldfarben, geplante Straßen).

## M1 Schritt 3 (Branch m1-zufriedenheit) – Stand 2026-11-04
- `dotnet test`: **39/39 bestanden** (6 neue: v3-Migration, Zielwert mit Gründen und 5-Punkte-Schritt, Unzufriedenheit verhindert Geburten, Unzufriedenheit bremst Arbeit, Vorrang-Baustelle zuerst fertig plus Replay und Ablehnungen, Monatsverlauf mit Obergrenze und Save/Load). Szenario Seed 12345: Ø Zufriedenheit im ersten Jahr 75–80.
- Unity: **noch nicht geprüft**. Die neue Anzeige (Zufriedenheit im Inspektor, Vorrang-Knopf, Verlaufsdiagramm) wurde nur geschrieben, nicht in Unity kompiliert.

## M1 Schritt 4 (Branch claude/project-thread-0zbl5x, baut auf m1-zufriedenheit auf) – Stand 2026-11-04
- `dotnet test`: **44/44 bestanden** (5 neue: v4-Migration mit echter v4-Datei, Werkstatt macht Werkzeug ohne Waren zu erzeugen, Werkzeug verdoppelt Arbeit und nutzt sich ab, Speichern mitten im Werkstück, KI baut Werkstatt). Seed 12345 nach 2 Jahren: beide KI-Dörfer haben eine Werkstatt und 5–6 Werkzeug auf Lager.
- Unity: **noch nicht geprüft** (neuer Knopf/Taste H, Werkstatt-Quader, Inspektortexte).
- Beobachtung (gilt schon für Schritt 3, gleicher Verlauf mit altem Code gemessen): Seed 12345 wächst von 6 auf 8 → 10 → 10 Einwohner je Dorf in 3 Jahren, der Lagetext sagt aber „Wächst“. Ursache vermutlich: volle Haushalte (2 Erwachsene + 2 Kinder) dürfen erst wieder wachsen, wenn Kinder mit 14 erwachsen sind. Der Lagetext erklärt das nicht → muss vor der M1-Abnahme korrigiert werden.

## Nächster Schritt
M1 „Siedlung wächst nachvollziehbar“: Berufe, Produktionsketten, Bauaufträge mit Arbeit für Straßen, Parzellen, Bedürfnisse, saisonale Erträge, Baupriorität, einfache Forschung, Statistik. Vorher: Branch `unity-fixes-m0` nach `main` mergen (Pull Request, Entscheidung des Nutzers).
