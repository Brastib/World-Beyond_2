# Testplan (kanonisch)

## Automatisiert ohne Unity — `Tools/run-tests.sh`
Quelle: `Assets/WorldBeyond/Tests/Shared` (laufen auch als Unity-EditMode-Tests).

| Bereich | Test |
|---|---|
| Determinismus | `SameSeedSameTicks_SameStateHash`, `DifferentSeed_DifferentMapAndState`, `KnownSeed_MapFingerprintIsStable`, `CommandReplay_ReproducesState` |
| Save/Load | `Roundtrip_IsByteIdentical_AndContinuesIdentically`, `SaveDuringActiveConstructionAndHauling_…`, `PendingCommand_IsSavedAndExecutedAfterLoad`, `FlippedByte_…`, `TruncatedFile_…`, `FutureFormatVersion_…`, `Header_IsReadableWithoutWorld`, `SaveStore_SlotsAutosaveAndCorruptFilesAreKept` |
| Gameplay M0 | Startzustand, Haushalt sammelt/baut/bezieht Haus, Spieler-Lager wird fertig, ungültige Befehle mit Grund, Straße kostet Stein und beschleunigt, Beobachtermodus |
| M1 | `V1Save_IsMigrated_AndKeepsRunning`, `Roles_AreAssignedByNeed_…`, `Population_GrowsOverYears_…` (3 Jahre, keine Siedlung stirbt aus), `AiSettlement_BuildsStorage_WhenFull`, `RemovingHauler_ReleasesReservations`, `LastMemberDies_…` |
| M1 Schritt 2 | `V2Save_IsMigrated_…`, `Road_CostsStone_AndIsFasterToWalk` (Straße entsteht erst durch Arbeit), `Field_GrowsInSpringSummer_HarvestedInAutumn_SaveLoadMidSeason`, `AiSettlements_PlantFields` |
| Invarianten | `ManySeeds_…` (20 Seeds × 600 Ticks), `LongRun_…` (120 Tage), `OneYear_ReportAndBasicHealth`: keine negativen Mengen, keine doppelten Ids, Reservierung = laufende Abholungen, Zulauf = Träger unterwegs, keine kaputten Referenzen |

## Unity — `Tools\run-unity-tests.ps1` (nur Windows-PC des Nutzers)
1. Einrichtung (URP-Asset, Materialien, Szene) im Batchmodus
2. EditMode: alle Shared-Tests in Unity
3. PlayMode: `Bootstrap_BuildsViews_AndTimeAdvances`, `Selection_RaycastHitsPersonPlaceholder`, `Smoke_BuildStorage_SaveLoad_SameHash`
4. Windows-Build
5. Build-Smoke: `WorldBeyond.exe -wb-smoke` → Bericht + Screenshot nach `TestResults/`

## Manuelle M0-Abnahme (Szene `Main`, Play)
1. Karte mit Wasser/Wiese/Wald/Hügel, drei farbige Siedlungen sichtbar; Kamera WASD/Q/E/Mausrad funktioniert.
2. Personen (Kugeln) laufen zu Ressourcen und zurück, Traglast-Würfel sichtbar.
3. Nach ~1 Spieltag erscheint eine graue Baustelle (Haus); Inspektor zeigt Material/Arbeit; nach Fertigstellung Meldung „bezieht sein Haus“.
4. **B** → grüne Vorschau auf Wiese, rot mit Grund auf Wasser → Klick → Lager-Baustelle → wird fertig.
5. **R** → Start und Ende klicken → braune Straße, Stein sinkt um die Felderzahl.
6. Leertaste pausiert, 1–4 ändern Tempo; Datum/Jahreszeit oben.
7. Debug (F3): Zustands-Hash notieren → slot1 Speichern → Spiel beenden → neu starten → slot1 Laden → Hash identisch.
8. Datei `slot1.wbsave` mit Editor beschädigen → Laden zeigt Fehler, Spiel läuft weiter, Datei bleibt erhalten.

## Manuelle M1-Prüfung (Schritt 1)
1. Tempo 8×, ein Spieljahr laufen lassen: T öffnet die Statistik; Einwohner steigen, Geburten > 0, neue Haushalte erscheinen.
2. Kleine Kugeln (Kinder) stehen an Häusern und arbeiten nicht.
3. Eine Person anklicken: Inspektor zeigt den Beruf; „Lage“ der Siedlung erklärt Wachstum oder Stillstand.
4. Bei einer KI-Siedlung erscheint nach einiger Zeit eine Lager-Baustelle (Meldung „beauftragt ein neues Lager“).
5. Einen alten M0-Spielstand (slot1) laden: lädt ohne Fehler und läuft weiter.

## Manuelle M1-Prüfung (Schritt 2)
1. R → Start/Ende klicken: Felder erscheinen hell kariert, Stein sinkt sofort, Baumeister laufen hin, Felder werden nacheinander braun.
2. F → Feld auf Wiese: graue Baustelle, danach braune Platte; im Sommer wird sie gelb/gold und höher; Inspektor zeigt Wachstum und Bauern.
3. Ab Herbst sinkt das Wachstum, Nahrung im Lager steigt; Statistik (T) zeigt Felder und erwarteten Ertrag.
4. Bei KI-Dörfern entstehen Felder von selbst.

## Manuelle M1-Prüfung (Schritt 3)
1. Person anklicken: Inspektor zeigt „Zufriedenheit X → Ziel Y (+15 satt, +15 eigenes Haus …)“.
2. Zwei Felder (F) setzen, das zweite auswählen, „Vorrang geben (P)“ drücken: Träger und Baumeister gehen zuerst dorthin; Inspektor zeigt „Priorität: Vorrang“.
3. Statistik (T): nach dem ersten Monatswechsel erscheinen Balken (Einwohner) und weiße Punkte (Ø Zufriedenheit).
4. Speichern, Laden: Vorrang, Zufriedenheit und Verlauf sind noch da.
