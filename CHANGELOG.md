# Changelog

## [Unreleased] – M1 Schritt 2: Straßenbau und Felder
- Straßen werden geplant und Feld für Feld von Baumeistern gebaut (hell karierte Vorschau → braune Straße).
- Neuer Beruf Bauer, neues Gebäude Feld (Taste F): Wachstum im Frühling/Sommer, Ernte ab Herbst, Farbe braun → gold.
- KI-Dörfer legen selbst Felder an; Inspektor und Statistik zeigen Feldertrag und offene Straßenfelder.
- Save-Format v3 mit Migration von v2 und Regressionstest.

## M1 Schritt 1: Siedlung wächst
- Berufe (Sammler, Holzfäller, Bergarbeiter, Baumeister, Träger, Kind) mit täglicher Zuteilung nach Bedarf.
- Lebenszyklus: Geburt, Auszug in neue Haushalte, Tod durch Alter/Hunger, leere Häuser werden wieder bezogen.
- KI-Siedlungen bauen bei vollem Lager selbst ein Lager; Nahrungsziel wächst mit der Bevölkerung.
- Lage-/Wachstumsbegründung je Siedlung in Inspektor, Statistik-Panel (T) und Protokoll.
- Save-Format v2 mit Migration von v1 und Regressionstest.

## M0 (abgenommen 2026-11-04)
- Neues Repository mit Assembly-Grenzen Core / Content / Systems / Persistence / Presentation / Editor / Tests.
- Seed-Karte 128×128 (Value-Noise, Biome, Ressourcen), Kalender mit Jahreszeiten.
- Drei Zivilisationen mit je Siedlung, 2 Haushalten, 6 Personen, Ortszentrum mit Startlager.
- Personen sammeln, liefern ab, tragen Material zu Baustellen und bauen; Haushalte beauftragen Häuser und ziehen ein.
- Spielerbefehle: Lager bauen, Straße bauen, Zivilisation/Beobachter wählen; Validierung mit Begründung.
- Save/Load: Binärformat v1 mit SHA-256, Slots, Autosave-Rotation, beschädigte Dateien werden nicht überschrieben.
- Unity-Platzhalterdarstellung, Kamera, Inspektor, Zeitsteuerung, HUD, Smoke-Test.
- Unity-Lauf auf 6000.6.0f1: Paketversionen angepasst, von Unity erzeugte Projektdateien ergänzt, Auswahl-Collider werden jedes Frame nachgezogen (Details in Docs/CURRENT_STATE.md).
