# Entscheidungen (ADRs)

Format: Datum · Entscheidung · Grund · Folgen.

**ADR-001 · 2026-11-04 · Neues Repository, kein Altcode.** Grund: Vorgabe des Master-Prompts. Folge: Repo `Brastib/World-Beyond_2`; altes `Brastib/World-Beyond` ist nur Archiv/Inspiration.

**ADR-002 · 2026-11-04 · Quadratisches Raster, 4er-Nachbarschaft, Chunks 32×32, Karte 128×128 Standard.** Grund: einfachste deterministische Wegfindung und Speicherung; Hex-Optik später rein darstellend möglich. Folge: Zellindex = y·Breite+x; Kartengröße muss Vielfaches von 32 sein.

**ADR-003 · 2026-11-04 · Zeitmodell: 1 Tick = 1 Stunde, 24 Ticks/Tag, 30 Tage/Monat, 12 Monate/Jahr, Jahreszeit = Monat/3.** Grund: Bewegung braucht Stundenauflösung, Wirtschaft rechnet täglich. Folge: Tagessysteme laufen bei Stunde 0. 1× = 4 Ticks/s.

**ADR-004 · 2026-11-04 · Feste Tick-Reihenfolge und Ganzzahl-Simulation.** Befehle → Zeit → Tagessysteme → Personen (Id-Reihenfolge). Keine Floats, eigener SplitMix64, keine Dictionary-Iteration. Grund: Determinismus über .NET/Unity/Mono hinweg. Folge: Kartenrauschen ist ganzzahliges Value-Noise; Biom-Schwellen als Perzentile.

**ADR-005 · 2026-11-04 · Eigenes Binärformat statt JSON-Bibliothek.** Datei: `WBRS` | Formatversion | Header (Name, Datum, Spielversion, Seed, Tick, Bevölkerung) | Nutzdaten | SHA-256. Grund: keine Zusatzpakete in Unity, exakte Feldreihenfolge, Byte-identischer Roundtrip testbar. Folgen: Jede neue persistente Eigenschaft erhöht die Formatversion; der Leser bekommt einen Migrationsschritt `vN → vN+1` mit deterministischem Standardwert und einen Regressionstest mit einer gespeicherten Alt-Datei. Neuere Formate werden mit klarer Meldung abgelehnt. Abgeleitete Daten (Gebäudebelegung) werden nicht gespeichert. Das Schema ist zugleich das DTO (keine zweite Klassenhierarchie); MonoBehaviours werden nie serialisiert.

**ADR-006 · 2026-11-04 · Content als C#-Katalog (`DefaultContent`) für M0.** Grund: kleinste funktionierende Lösung, validierbar per Test. Folge: Wechsel auf JSON/ScriptableObject in M1 möglich, ohne Simulationscode zu ändern (Zugriff nur über Def-Ids).

**ADR-007 · 2026-11-04 · Siedlungsplatz-Suche mit dokumentierter Lockerung.** Mindestabstand 40 → 28 → 16 Felder; findet sich auch dann nichts, wird der Seed mit Fehlermeldung abgelehnt (keine Ersatzkarte). Getestet für Seeds 1–20.

**ADR-008 · 2026-11-04 · Platzhalter-UI mit IMGUI.** Grund: null Einrichtung, sofort testbar. Folge: wird beim UI-Pass durch UI Toolkit ersetzt; Inhalte (Texte/Gründe) kommen schon jetzt aus der Simulation.

**ADR-009 · 2026-11-04 · M0-Vereinfachungen (bewusst, sichtbar dokumentiert).** Straßen werden sofort gebaut und kosten 1 Stein/Feld; ein Allrounder-Beruf; Hunger senkt Gesundheit, aber niemand stirbt; Gebäude belegen 1 Zelle und sind begehbar. Folge: in M1 durch Bauaufträge, Berufe und Lebenszyklus ersetzen.

**ADR-010 · 2026-11-04 · Sammelregel gegen volle Lager.** Ressourcen über dem doppelten Zielbestand (Nahrung 200, Holz 160, Stein 80) werden nicht mehr gesammelt. Grund: ohne Regel füllte im Testjahr eine Siedlung ihr Lager mit Holz/Stein und verhungerte im Winter. Beeren wachsen im Frühling/Sommer täglich, im Herbst jeden zweiten Tag, im Winter nicht.
