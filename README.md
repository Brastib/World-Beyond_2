# World Beyond: Reworked

Zivilisations-, Aufbau- und Weltsimulation für Windows-PC (Unity 6, C#, URP). Neues, eigenständiges Repository; kein Code aus dem alten World-Beyond-Projekt.

**Stand:** M0 (Fundament) in Arbeit. Was wirklich getestet ist, steht ausschließlich in [Docs/CURRENT_STATE.md](Docs/CURRENT_STATE.md).

## Schnellstart

- Unity-freie Tests: `Tools/run-tests.sh` (oder `cd DotNet && dotnet test WorldBeyond.Tests`)
- Unity (Windows): Ordner im Unity Hub öffnen (Unity 6), Menü **World Beyond → Projekt einrichten**, Szene `Assets/WorldBeyond/Scenes/Main.unity` starten.
- Vollständige Unity-Prüfung: `powershell -ExecutionPolicy Bypass -File Tools\run-unity-tests.ps1`

## Dokumente

[Projektbrief](Docs/PROJECT_BRIEF.md) · [Architektur](Docs/ARCHITECTURE.md) · [Entscheidungen](Docs/DECISIONS.md) · [Ist-Stand](Docs/CURRENT_STATE.md) · [Testplan](Docs/TEST_PLAN.md) · [Assets](Docs/ASSET_REGISTRY.md) · [Changelog](CHANGELOG.md)
