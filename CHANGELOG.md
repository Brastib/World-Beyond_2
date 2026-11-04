# Changelog

## [Unreleased] – M1 Schritt 4: Produktionskette Werkzeug
- Alleinstehende gründen Familien (abstrakt); der Lagetext nennt den echten Grund für Wachstum oder Stillstand statt immer „Wächst“.
- Neues Gebäude Werkstatt (Taste H, 20 Holz + 10 Stein) und neuer Beruf Handwerker: 2 Holz + 1 Stein + 6 Std. Arbeit → 1 Werkzeug.
- Holzfäller, Bergarbeiter und Baumeister nehmen Werkzeug im Lager mit und arbeiten damit doppelt so schnell; es nutzt sich nach 50 Einsätzen ab.
- KI-Dörfer bauen ab 8 Einwohnern selbst eine Werkstatt. Inspektor zeigt Rezept, Werkstück-Fortschritt und Werkzeug der Person.
- Save-Format v5 mit Migration von v4 und Regressionstest mit echter v4-Datei.

## M1 Schritt 3: Zufriedenheit, Baupriorität, Verlauf
- Zufriedenheit je Person (0..100) mit sichtbaren Gründen (satt, Haus, Enge, krank, Straßenanschluss); bewegt sich höchstens 5 Punkte pro Tag.
- Wirkung: Geburten erst ab Ø 50 in der Siedlung, unter 25 arbeiten Personen langsamer; Lagetext nennt Unzufriedenheit.
- Baupriorität (Knopf im Inspektor oder Taste P) als echter Befehl: Vorrang-Baustellen werden zuerst beliefert und gebaut, Replay-fähig.
- Monatlicher Verlauf je Siedlung (Einwohner, Bestände, Ø Zufriedenheit, höchstens 120 Monate) als Mini-Diagramm in der Statistik (T).
- Save-Format v4 mit Migration von v3 und Regressionstest mit echter v3-Datei.

## M1 Schritt 2: Straßenbau und Felder
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
