using System.Linq;
using NUnit.Framework;
using WorldBeyond.Simulation.Core;
using WorldBeyond.Simulation.Systems;

namespace WorldBeyond.Tests
{
    /// <summary>Ein simuliertes Jahr als Szenario-Smoke-Test mit lesbarem Bericht (dotnet test -v n zeigt ihn).</summary>
    public class ScenarioReportTests
    {
        [Test]
        public void OneYear_ReportAndBasicHealth()
        {
            var sim = TestUtil.NewSim();
            var w = sim.World;
            for (int q = 1; q <= 4; q++)
            {
                sim.Run(Calendar.TicksPerDay * 90);
                TestUtil.AssertValid(w);
                foreach (var s in w.Settlements)
                {
                    var st = Economy.SettlementStock(w, sim.Content, s.Id);
                    int houses = w.Buildings.Count(b => b.SettlementId == s.Id && b.Complete && b.DefId == "haus");
                    int hungry = w.People.Count(p => p.SettlementId == s.Id && p.Hunger > 0);
                    TestContext.WriteLine($"{Calendar.Format(w.Tick)} {s.Name}: Nahrung {st[0]} Holz {st[1]} Stein {st[2]} Häuser {houses} Hungrig {hungry} Zufrieden Ø {Satisfaction.Average(w, s.Id)} | {s.Status}");
                }
            }
            Assert.That(w.Households.Count(h => h.HomeBuildingId != 0), Is.GreaterThanOrEqualTo(3), "Mindestens ein Haus pro Siedlung nach einem Jahr");
            Assert.That(w.People.Count(p => p.Health < 50), Is.EqualTo(0), "Niemand sollte im Standardszenario schwer hungern");
        }
    }
}
