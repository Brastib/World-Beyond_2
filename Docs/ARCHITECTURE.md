# Architektur

Abhängigkeiten zeigen nur nach innen. Jede Schicht ist eine eigene Assembly (Unity-`.asmdef`) und, soweit Unity-frei, zusätzlich ein `.csproj` unter `DotNet/`, das dieselben Quelldateien kompiliert. So werden die Grenzen auch ohne Unity geprüft.

```
Presentation.Unity ──► Systems ──► Content ──► Core
        │                 │                     ▲
        └──► Persistence ─┴─────────────────────┘
Editor (nur Editor) ──► Presentation, Persistence, URP
Tests.Shared (EditMode + dotnet) ──► Core, Content, Systems, Persistence
Tests.PlayMode ──► Presentation
```

| Assembly | Ordner | Inhalt | Unity? |
|---|---|---|---|
| WorldBeyond.Simulation.Core | `Assets/WorldBeyond/Simulation/Core` | WorldState, WorldMap, Entitäten, Kalender, Zufall, Befehle, Definitionstypen, WorldValidator | nein (`noEngineReferences`) |
| WorldBeyond.Content | `Assets/WorldBeyond/Content` | DefaultContent: Gebäude-, Epochendefinitionen | nein |
| WorldBeyond.Simulation.Systems | `Assets/WorldBeyond/Simulation/Systems` | WorldSimulation (Tick), WorldGenerator, ScenarioFactory, Pathfinder, Economy, BuildRules, CommandProcessor, PersonSystem, DailySystems, Construction | nein |
| WorldBeyond.Simulation.Persistence | `Assets/WorldBeyond/Simulation/Persistence` | WorldSerializer (Schema v1), SaveCodec (Header, SHA-256), SaveStore (Slots, Autosave, atomisches Schreiben) | nein |
| WorldBeyond.Presentation.Unity | `Assets/WorldBeyond/Presentation` | GameBootstrap, GameSession (Adapter), MapView, EntityViews, CameraRig, HudGui, InputAdapter, PlaceholderStyle, SmokeRunner | ja |
| WorldBeyond.Editor | `Assets/WorldBeyond/Editor` | ProjectSetup (URP, Materialien, Szene), BuildSmoke | Editor |

## Datenflüsse

- **Tick** (`WorldSimulation.Step`): 1. ausstehende Befehle ausführen und in die Historie schreiben → 2. `Tick++` → 3. bei Stunde 0: Bedürfnisse, Natur, Haushaltsentscheidungen → 4. Personen in Id-Reihenfolge.
- **Spieleraktion:** UI → `GameSession.Issue` → `Preview` (Validierung, Grund als Text) → `Submit` (Warteschlange) → nächster Tick → `CommandProcessor.Execute` → Ereignisprotokoll.
- **Darstellung:** liest nach jedem Tick (`EntityViews.Sync`, `MapView.Refresh`), schreibt nie in den Zustand. Personen werden zwischen Ticks interpoliert.
- **Speichern:** `WorldState` → `WorldSerializer` (explizite Feldreihenfolge) → `SaveCodec` (Magic, Formatversion, lesbarer Header, Nutzdaten, SHA-256) → `SaveStore` (Temp-Datei, Gegenprobe, Ersetzen). Laden prüft Prüfsumme und `WorldValidator`-Invarianten.

## Determinismus-Regeln

Keine Gleitkommazahlen in der Simulation, keine `Dictionary`-Iteration im Simulationspfad, eigener SplitMix64-Generator (Zustand wird gespeichert), Entitätslisten nach Id sortiert, A* mit Gleichstand nach Zellindex. Gleicher Seed + gleiche Befehlshistorie ⇒ gleicher Zustands-Hash (Test `CommandReplay_ReproducesState`).

## Erweiterungshaken

Neue Ressourcen: `ResourceType` anhängen + Save-Format erhöhen. Neue Gebäude: nur `DefaultContent` + `PlaceholderStyle`. Neue Befehle: `CommandType` + `CommandProcessor`. Neue Epochen: `EpochDef.Enabled`. Neue persistente Felder: siehe ADR-005.
