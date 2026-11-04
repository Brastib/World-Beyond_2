# Ist-Stand (nur belegte Fakten)

**Datum:** 2026-11-04 · **Meilenstein:** M0 in Arbeit

## Tatsächlich geprüft
- `dotnet test` (Linux-Cloud, .NET 8.0.133): **24/24 Tests bestanden**. Unity-freie Assemblies bauen mit `TreatWarningsAsErrors` gegen netstandard2.1/C# 9 (wie Unity).
- Szenario Seed 12345, 1 Jahr: alle drei Siedlungen bauen 2 Häuser, niemand hungert, Lager laufen bei ~294/300 voll (Wachstum stagniert ohne Spieler-Lager → so gewollt, Inspektor zeigt Hinweis).

## Geschrieben, aber noch NICHT in Unity geprüft
- Gesamte Unity-Schicht (Presentation, Editor, PlayMode-Tests), `Packages/manifest.json`, `Tools/run-unity-tests.ps1`. Kein Unity-Kompilat, kein Play-Lauf, kein Build bisher.

## Bekannte Risiken
- Paketversionen im Manifest (URP 17.0.4, Input System 1.11.2, Test Framework 1.4.6) passen evtl. nicht zur installierten Unity-6-Version.
- `UniversalRenderPipelineAsset.Create` im Editor-Setup ist nicht in dieser Umgebung verifiziert.
- M0-Vereinfachungen siehe DECISIONS ADR-009.

## Nächster Schritt
Unity-Lauf auf dem PC des Nutzers (Tools\run-unity-tests.ps1), Fehler beheben, Ergebnis hier eintragen.
