using System;
using System.Diagnostics;
using System.IO;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Persistence;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Presentation
{
    /// <summary>
    /// Adapter zwischen Unity und Simulation: Zeitsteuerung, Befehle, Speichern/Laden.
    /// Kein MonoBehaviour und kein Singleton; der Bootstrap besitzt genau eine Instanz.
    /// </summary>
    public sealed class GameSession
    {
        public const float TicksPerSecondAt1x = 4f;
        public const int MaxTicksPerFrame = 64;
        public static readonly int[] Speeds = { 1, 2, 4, 8 };

        public WorldSimulation Sim { get; private set; }
        public WorldState World => Sim.World;
        public readonly ContentCatalog Content = DefaultContent.Create();
        public readonly SaveStore Saves;
        public bool Paused;
        public int SpeedIndex;
        public string Message = "";
        public double LastTickMs, AvgTickMs;
        public long Generation;  // erhöht sich bei Neu/Laden → Views bauen sich neu auf
        float _acc;
        long _lastAutosaveMonth = -1;

        public event Action WorldReplaced;

        public GameSession(string saveDirectory, ulong seed)
        {
            Saves = new SaveStore(saveDirectory);
            NewWorld(seed);
        }

        public void NewWorld(ulong seed)
        {
            Sim = new WorldSimulation(ScenarioFactory.CreateDefault(seed, Content), Content);
            Paused = false; _acc = 0; _lastAutosaveMonth = 0;
            Message = $"Neue Welt mit Seed {seed}.";
            Generation++;
            WorldReplaced?.Invoke();
        }

        /// <summary>Gibt die Anzahl ausgeführter Ticks zurück.</summary>
        public int Update(float deltaSeconds)
        {
            if (Paused) return 0;
            _acc += deltaSeconds * TicksPerSecondAt1x * Speeds[SpeedIndex];
            int n = 0;
            while (_acc >= 1f && n < MaxTicksPerFrame) { StepOnce(); _acc -= 1f; n++; }
            if (n == MaxTicksPerFrame) _acc = 0; // Rückstand verwerfen statt Spirale; Debug-Overlay zeigt echte Tickdauer.
            return n;
        }

        public void StepOnce()
        {
            var sw = Stopwatch.StartNew();
            Sim.Step();
            sw.Stop();
            LastTickMs = sw.Elapsed.TotalMilliseconds;
            AvgTickMs = AvgTickMs * 0.95 + LastTickMs * 0.05;
            long month = Calendar.Day(World.Tick) / Calendar.DaysPerMonth;
            if (month != _lastAutosaveMonth && World.Tick % Calendar.TicksPerDay == 0)
            {
                _lastAutosaveMonth = month;
                if (month > 0) TryAutosave();
            }
        }

        void TryAutosave()
        {
            try { var info = Saves.Autosave(World, DateTime.UtcNow); Message = "Autosave: " + info.Slot; }
            catch (Exception ex) { Message = "Autosave fehlgeschlagen: " + ex.Message; UnityEngine.Debug.LogException(ex); }
        }

        public void SetSpeed(int index) { SpeedIndex = Math.Max(0, Math.Min(Speeds.Length - 1, index)); Paused = false; }

        /// <summary>Prüft sofort (Vorschau) und reiht bei Erfolg ein. Rückgabe: Fehlergrund oder null.</summary>
        public string Issue(GameCommand cmd)
        {
            string reason = Sim.Preview(cmd);
            if (reason != null) { Message = reason; return reason; }
            Sim.Submit(cmd);
            Message = "Befehl eingereiht.";
            return null;
        }

        public bool Save(string slot)
        {
            try
            {
                var info = Saves.Save(slot, World, $"{World.Civ(World.Civilizations[0].Id).Name}-Welt {World.Seed}", DateTime.UtcNow);
                Message = $"Gespeichert in {slot} ({Calendar.Format(World.Tick)}).";
                return info.Error == null;
            }
            catch (Exception ex) { Message = "Speichern fehlgeschlagen: " + ex.Message; UnityEngine.Debug.LogException(ex); return false; }
        }

        public bool Load(string slot)
        {
            try
            {
                var w = Saves.Load(slot, out var header);
                Sim = new WorldSimulation(w, Content);
                _acc = 0; _lastAutosaveMonth = Calendar.Day(w.Tick) / Calendar.DaysPerMonth;
                Message = $"Geladen: {header.SaveName}, {Calendar.Format(w.Tick)}.";
                Generation++;
                WorldReplaced?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                // Spielstand bleibt unangetastet; laufende Welt bleibt erhalten.
                Message = "Laden fehlgeschlagen: " + ex.Message;
                UnityEngine.Debug.LogWarning("[WorldBeyond] " + Message);
                return false;
            }
        }

        public string StateHash() => SaveCodec.StateHash(World);

        public static string DefaultSaveDirectory(string persistentDataPath) => Path.Combine(persistentDataPath, "Saves");
    }
}
