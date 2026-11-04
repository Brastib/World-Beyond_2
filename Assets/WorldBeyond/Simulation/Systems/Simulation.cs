using System;
using System.Collections.Generic;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Simulation.Systems
{
    /// <summary>
    /// Einstiegspunkt der Simulation. Tick-Reihenfolge (ADR-004):
    /// 1. ausstehende Befehle, 2. Zeit +1, 3. Tagessysteme bei Stunde 0, 4. Personen (Id-Reihenfolge).
    /// </summary>
    public sealed class WorldSimulation
    {
        public readonly WorldState World;
        public readonly ContentCatalog Content;
        internal readonly Pathfinder Paths;

        public WorldSimulation(WorldState world, ContentCatalog content)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            if (world.ContentVersion != content.Version)
                throw new InvalidOperationException($"Content-Version passt nicht: Welt {world.ContentVersion}, geladen {content.Version}.");
            Paths = new Pathfinder(world.Map);
        }

        /// <summary>Reiht einen Befehl ein; ausgeführt wird er zu Beginn des nächsten <see cref="Step"/>.</summary>
        public GameCommand Submit(GameCommand cmd)
        {
            var c = cmd.Clone();
            c.Tick = World.Tick; c.Succeeded = false; c.Result = "";
            World.PendingCommands.Add(c);
            return c;
        }

        public string Preview(GameCommand cmd) => CommandProcessor.Validate(this, cmd);

        public void Step()
        {
            var w = World;
            if (w.PendingCommands.Count > 0)
            {
                foreach (var c in w.PendingCommands)
                {
                    CommandProcessor.Execute(this, c);
                    w.CommandHistory.Add(c);
                }
                w.PendingCommands.Clear();
            }
            w.Tick++;
            if (w.Tick % Calendar.TicksPerDay == 0) DailySystems.Run(this);
            PersonSystem.Tick(this);
        }

        public void Run(int ticks) { for (int i = 0; i < ticks; i++) Step(); }

        /// <summary>Spielt eine Befehlshistorie auf einer frischen Welt bis <paramref name="untilTick"/> nach.</summary>
        public static void Replay(WorldSimulation fresh, IList<GameCommand> history, long untilTick)
        {
            int next = 0;
            while (fresh.World.Tick < untilTick)
            {
                while (next < history.Count && history[next].Tick == fresh.World.Tick) fresh.Submit(history[next++]);
                fresh.Step();
            }
        }
    }
}
