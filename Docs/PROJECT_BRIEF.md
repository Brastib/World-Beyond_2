# Projektbrief

**Ziel:** Eine lebendige, speicherbare und testbare Weltsimulation. Personen und Haushalte bilden Siedlungen und Zivilisationen, wirtschaften, bauen, forschen, handeln und können Konflikte austragen. Der Spieler beobachtet, lenkt und greift ein.

**Profil:** Unity 6 + C# + URP · Windows-PC, Maus/Tastatur, 1080p-Referenz · schräg-isometrische Strategiekamera · deterministischer Seed, quadratisches Raster in 32er-Chunks · 1 Tick = 1 Stunde, 1 Tag = kleinste Wirtschaftseinheit · Pause/1×/2×/4×/8×.

**Start:** gemäßigte Seed-Karte, drei kleine Zivilisationen mit je einer Siedlung, zwei Haushalten (6 Personen), Ortszentrum mit Nahrung/Holz/Stein. Spieler startet als Aurer, kann zu anderen Zivilisationen oder in den Beobachtermodus wechseln.

**Erstes Produktionsziel:** spielbare Frühzeit/Mittelalter-Vertikalscheibe. Die übrigen Epochen sind nur Daten-/Architekturraum (deaktivierte Definitionen).

**Arbeitsprinzip:** Alle Systeme zuerst mit eindeutigen Platzhaltern. Finale Grafik nur über einen späteren separaten Asset-Auftrag.

**Meilensteine:** M0 Fundament · M1 Siedlung wächst nachvollziehbar · M2 Mehrere Zivilisationen und Beziehungen · M3 Tiefe und Langzeitspiel · M4 Epochengerüst. Abnahmekriterien siehe Master-Prompt; die M0-Kriterien stehen in [TEST_PLAN.md](TEST_PLAN.md).

**Aktuelle Grenzen (M0):** ein Beruf (Allrounder-Sammler), keine Geburt/Tod, keine Forschung, kein Territorium, keine Diplomatie, Straßen werden sofort gebaut, IMGUI-Platzhalter-UI, keine Lokalisierungsdateien (Deutsch im Code).

**Nicht-Ziele:** finale Kunst, Industrie-Epoche, breite Kriegssimulation, Kauf-/Cloud-Abhängigkeiten.
