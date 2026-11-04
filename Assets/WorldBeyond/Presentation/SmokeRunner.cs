using System;
using System.Collections;
using System.IO;
using UnityEngine;
using WorldBeyond.Content;
using WorldBeyond.Simulation.Core;

namespace WorldBeyond.Presentation
{
    /// <summary>
    /// Build-/Play-Smoke-Test (Startparameter -wb-smoke): simuliert, baut ein Lager, speichert, lädt,
    /// vergleicht den Zustands-Hash, macht einen Screenshot und schreibt einen Bericht. Beendet die Anwendung danach.
    /// </summary>
    public sealed class SmokeRunner : MonoBehaviour
    {
        public string ReportPath;
        public bool Done, Passed;

        IEnumerator Start()
        {
            var g = GetComponent<GameBootstrap>();
            var s = g.Session;
            var log = new System.Text.StringBuilder();
            void L(string x) { log.AppendLine(x); Debug.Log("[Smoke] " + x); }
            string dir = Path.Combine(Application.persistentDataPath, "Smoke");
            Directory.CreateDirectory(dir);
            ReportPath = Path.Combine(dir, "smoke_report.txt");
            try
            {
                L($"Unity {Application.unityVersion}, Plattform {Application.platform}, Seed {s.World.Seed}");
                L(PlaceholderMaterials.ShaderInfo);
            }
            catch (Exception ex) { L("Fehler: " + ex); }
            bool ok = true;
            yield return null;
            s.Paused = true;
            int cell = -1;
            var w = s.World;
            var center = w.SettlementById(w.Civ(w.PlayerCivId).CapitalSettlementId).CenterCell;
            for (int d = 3; d <= 12 && cell < 0; d++)
                for (int i = 0; i < w.Map.CellCount && cell < 0; i++)
                    if (w.Map.Manhattan(i, center) == d && s.Sim.Preview(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, i)) == null) cell = i;
            L("Lager-Bauplatz: " + cell);
            ok &= cell >= 0 && s.Issue(GameCommand.PlaceBuilding(w.PlayerCivId, DefaultContent.Storage, cell)) == null;
            for (int i = 0; i < 24 * 30; i++) { s.StepOnce(); if (i % 48 == 0) { g.Entities.Sync(s.World); yield return null; } }
            g.Entities.Sync(s.World); g.Map.Refresh(s.World.Map);
            bool storageDone = false, houseMoved = false;
            foreach (var b in s.World.Buildings) if (b.DefId == DefaultContent.Storage && b.Complete) storageDone = true;
            foreach (var h in s.World.Households) if (h.HomeBuildingId != 0) houseMoved = true;
            L($"Nach 30 Tagen: Lager fertig={storageDone}, Haushalt im Haus={houseMoved}, {Calendar.Format(s.World.Tick)}");
            ok &= storageDone && houseMoved;
            string before = s.StateHash();
            ok &= s.Save("smoke");
            s.NewWorld(999);
            ok &= s.Load("smoke");
            string after = s.StateHash();
            L($"Hash vor Speichern {before.Substring(0, 16)}, nach Laden {after.Substring(0, 16)}");
            ok &= before == after;
            yield return null; yield return null;
            string shot = Path.Combine(dir, "smoke_screenshot.png");
            ScreenCapture.CaptureScreenshot(shot);
            yield return new WaitForSeconds(1f);
            L("Screenshot: " + shot + (File.Exists(shot) ? "" : " (noch nicht geschrieben)"));
            Passed = ok;
            L(ok ? "SMOKE BESTANDEN" : "SMOKE FEHLGESCHLAGEN");
            File.WriteAllText(ReportPath, log.ToString());
            Done = true;
            if (!Application.isEditor) Application.Quit(ok ? 0 : 1);
        }
    }
}
