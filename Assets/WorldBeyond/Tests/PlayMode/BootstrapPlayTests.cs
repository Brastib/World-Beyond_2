using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WorldBeyond.Presentation;

namespace WorldBeyond.Tests.PlayMode
{
    public class BootstrapPlayTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown() { if (_go != null) Object.Destroy(_go); }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsViews_AndTimeAdvances()
        {
            _go = new GameObject("WB-Test");
            var g = _go.AddComponent<GameBootstrap>();
            yield return null;
            Assert.IsNotNull(g.Session);
            Assert.AreEqual(16, g.Map.ChunkCount);
            Assert.AreEqual(18, Object.FindObjectsByType<EntityTag>(FindObjectsSortMode.None).Length - 3, "18 Personen + 3 Ortszentren erwartet");
            long t0 = g.Session.World.Tick;
            g.Session.SetSpeed(3);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.5f) yield return null;
            Assert.Greater(g.Session.World.Tick, t0, "Zeit läuft nicht");
            g.Session.Paused = true;
            long t1 = g.Session.World.Tick;
            yield return null; yield return null;
            Assert.AreEqual(t1, g.Session.World.Tick, "Pause hält die Zeit nicht an");
        }

        [UnityTest]
        public IEnumerator Selection_RaycastHitsPersonPlaceholder()
        {
            _go = new GameObject("WB-Test");
            var g = _go.AddComponent<GameBootstrap>();
            yield return null; yield return null;
            var person = g.Session.World.People[0];
            var pos = g.Entities.PersonPosition(person.Id).Value;
            var cam = g.Rig.Cam;
            var ray = new Ray(cam.transform.position, (pos - cam.transform.position).normalized);
            Assert.IsTrue(Physics.Raycast(ray, out var hit, 1000f));
            Assert.IsTrue(hit.collider.TryGetComponent<EntityTag>(out var tag));
            Assert.AreEqual(EntityKind.Person, tag.Kind);
        }

        [UnityTest]
        public IEnumerator Smoke_BuildStorage_SaveLoad_SameHash()
        {
            _go = new GameObject("WB-Test");
            _go.AddComponent<GameBootstrap>();
            var smoke = _go.AddComponent<SmokeRunner>();
            float start = Time.realtimeSinceStartup;
            while (!smoke.Done && Time.realtimeSinceStartup - start < 120f) yield return null;
            Assert.IsTrue(smoke.Done, "Smoke-Test lief nicht zu Ende");
            Assert.IsTrue(smoke.Passed, "Smoke fehlgeschlagen, Bericht: " + smoke.ReportPath + "\n" + System.IO.File.ReadAllText(smoke.ReportPath));
        }
    }
}
