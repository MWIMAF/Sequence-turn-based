using NUnit.Framework;
using static Game.Combat.Tests.TestWorld;

namespace Game.Combat.Tests
{
    /// <summary>GDD damage formula: raw = Attack + SkillPower, × crit/buff/stagger, × C / (Defense + C).</summary>
    public sealed class DamageFormulaTests
    {
        [Test]
        public void Defense_ReducesDamage_ByCOverDefensePlusC()
        {
            using (var w = new TestWorld(enemyDefense: NormalC))
            {
                var clash = Clash(w.Resolve(Seq(w.Slash, null, null), Seq(null, null, null)), 0);
                Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashHit * NormalC / (NormalC + NormalC))); // 10 × 50/100 = 5
            }
        }

        [Test]
        public void HigherTier_UsesALargerC_SoDefenseMattersLess()
        {
            using (var w = new TestWorld(enemyDefense: NormalC, tier: EnemyTier.Boss))
            {
                var clash = Clash(w.Resolve(Seq(w.Slash, null, null), Seq(null, null, null)), 0);
                Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashHit * BossC / (NormalC + BossC))); // 10 × 100/150 = 6
            }
        }

        [Test]
        public void ALandedHit_DealsAtLeastMinimumDamage()
        {
            using (var w = new TestWorld(attack: 0, enemyDefense: 10000))
            {
                var clash = Clash(w.Resolve(Seq(w.Kick, null, null), Seq(null, null, null)), 0);
                Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(1));
            }
        }

        [Test]
        public void Crit_MultipliesDamage_AndIsReported()
        {
            using (var w = new TestWorld(playerCrit: 100))
            {
                var clash = Clash(w.Resolve(Seq(w.Slash, null, null), Seq(null, null, null)), 0);
                Assert.That(clash.Player.LandedCrit, Is.True);
                Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashHit * 150 / 100)); // 15
            }
        }

        [Test]
        public void CritAndStagger_StackWithASingleRoundDown()
        {
            using (var w = new TestWorld(playerCrit: 100))
            {
                var events = w.Resolve(Seq(w.Parry, w.Slash, null), Seq(w.Slash, null, null));
                var hit = Clash(events, 1).Enemy;

                // 10 × 1.5 × 1.3 = 19.5 → 19; without stagger 15, so the stagger part is 4.
                Assert.That(hit.DamageTaken, Is.EqualTo(19));
                Assert.That(hit.StaggerBonusTaken, Is.EqualTo(4));
            }
        }

        [Test]
        public void NoCritChance_NeverCrits()
        {
            using (var w = new TestWorld(playerCrit: 0))
            {
                for (int i = 0; i < 20; i++)
                {
                    var clash = Clash(w.Resolve(Seq(w.Kick, null, null), Seq(null, null, null)), 0);
                    Assert.That(clash.Player.LandedCrit, Is.False);
                    if (w.State.IsOver) break;
                }
            }
        }
    }
}
