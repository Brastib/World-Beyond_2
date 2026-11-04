# Ist-Stand (nur belegte Fakten)

**Datum:** 2026-11-04 · **Meilenstein:** M0 in Arbeit

## Tatsächlich geprüft
- `dotnet test` (Linux-Cloud, .NET 8.0.133): **24/24 Tests bestanden**. Unity-freie Assemblies bauen mit `TreatWarningsAsErrors` gegen netstandard2.1/C# 9 (wie Unity).
- Szenario Seed 12345, 1 Jahr: alle drei Siedlungen bauen 2 Häuser, niemand hungert, Lager laufen bei ~294/300 voll (Wachstum stagniert ohne Spieler-Lager → so gewollt, Inspektor zeigt Hinweis).
- Unity 6000.6.0f1 auf dem Windows-PC des Nutzers, `Tools\run-unity-tests.ps1` (2026-11-04, 15:05): Einrichtung, EditMode, PlayMode, Windows-Build und Build-Smoke jeweils **Exitcode 0**.
  - EditMode **24/24**, PlayMode **3/3** bestanden.
  - Build-Smoke (Seed 12345): Lager nach 30 Tagen fertig, Haushalt im Haus, Zustands-Hash vor Speichern und nach Laden gleich (`30b39c7cdcde05b1`), Screenshot erzeugt.
- Erster Lauf davor: `Selection_RaycastHitsPersonPlaceholder` schlug fehl. Nach `Physics.SyncTransforms()` in `EntityViews.Animate` bestanden; die Ursache (Collider nicht nachgezogen) ist daraus abgeleitet, nicht einzeln nachgemessen.

## NICHT geprüft
- Manuelles Spielen mit Maus und Tastatur: Kamera, Klick-Auswahl im Spiel, Bau-Knöpfe, Tempo-Knöpfe. Der Smoke-Test baut per Code.
- Speichern, Programm neu starten, Laden: Der Smoke-Test lädt im selben Prozess. Die M0-Abnahme verlangt einen echten Neustart.

## Bekannte Risiken
- `Packages/manifest.json` wurde beim ersten Unity-Lauf von Unity um Standardpakete erweitert, darunter `com.unity.purchasing` 4.15.0 (laut Unity veraltet), `com.unity.analytics` und `com.unity.collab-proxy`. Das Spiel braucht sie nicht; Entfernen ist offen (Entscheidung des Nutzers) und muss danach neu getestet werden. `Assets/Resources/BillingMode.json` stammt vermutlich vom Purchasing-Paket.
- Aktiv ist das alte Input-System (`activeInputHandler: 0`), obwohl das Input-System-Paket installiert ist.
- M0-Vereinfachungen siehe DECISIONS ADR-009.

## Nächster Schritt
Pakete klären, dann manuelle M0-Prüfung (Klicks, Speichern → Neustart → Laden) durchführen und hier eintragen.
