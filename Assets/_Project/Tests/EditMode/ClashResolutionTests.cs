using System.Linq;
using NUnit.Framework;
using static Game.Combat.Tests.TestWorld;

namespace Game.Combat.Tests
{
    /// <summary>The GDD clash table and cross-slot Stagger.</summary>
    public sealed class ClashResolutionTests
    {
        private TestWorld _w;

        [SetUp]
        public void SetUp() => _w = new TestWorld();

        [TearDown]
        public void TearDown() => _w.Dispose();

        [Test]
        public void DefaultTable_HasARuleForEveryMoveTypePair()
        {
            Assert.That(_w.Table.FindMissingPairs(), Is.Empty);
        }

        [Test]
        public void AttackVsAttack_BothTakeTheirOpponentsDamage()
        {
            var clash = Clash(_w.Resolve(Seq(_w.Slash, null, null), Seq(_w.Kick, null, null)), 0);

            Assert.That(clash.Player.Label, Is.EqualTo("Trade"));
            Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashHit));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(Attack + KickPower));
        }

        [Test]
        public void AttackVsDodge_DealsNothing_AndEvadeStillGainsStamina()
        {
            int before = _w.Player.Stamina;
            var clash = Clash(_w.Resolve(Seq(_w.Evade, null, null), Seq(_w.Slash, null, null)), 0);

            Assert.That(clash.Enemy.Label, Is.EqualTo("Dodged"));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(0));
            Assert.That(clash.Player.StaminaGained, Is.EqualTo(EvadeGain));
            Assert.That(_w.Player.Stamina, Is.EqualTo(before + EvadeGain));
        }

        [Test]
        public void ParryVsAttack_NoDamage_StaggersTheAttacker()
        {
            var events = _w.Resolve(Seq(_w.Parry, null, null), Seq(_w.Slash, null, null));

            var clash = Clash(events, 0);
            Assert.That(clash.Player.Label, Is.EqualTo("Parry!"));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(0));
            Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(0), "GDD: Parry has no counter damage.");

            var applied = events.OfType<StatusAppliedEvent>().Single();
            Assert.That(applied.Who, Is.EqualTo(CombatantId.Enemy));
            Assert.That(applied.Status, Is.EqualTo(StatusType.Staggered));
            Assert.That(applied.Slot, Is.EqualTo(0));
        }

        [Test]
        public void Staggered_TakesThirtyPercentMore_InTheNextSlotOnly()
        {
            var events = _w.Resolve(Seq(_w.Parry, _w.Slash, _w.Slash), Seq(_w.Slash, null, null));

            int staggered = SlashHit * (100 + StaggerPercent) / 100; // 13
            var slot2 = Clash(events, 1);
            Assert.That(slot2.Enemy.MoveType, Is.EqualTo(MoveType.Idle), "Stagger doesn't cancel moves any more.");
            Assert.That(slot2.Enemy.DamageTaken, Is.EqualTo(staggered));
            Assert.That(slot2.Enemy.StaggerBonusTaken, Is.EqualTo(staggered - SlashHit));

            Assert.That(Clash(events, 2).Enemy.DamageTaken, Is.EqualTo(SlashHit), "Stagger lasts one slot.");
            Assert.That(events.OfType<StatusExpiredEvent>().Single().Slot, Is.EqualTo(1));
        }

        [Test]
        public void ParriedPlayer_IsStaggeredToo_AndTakesTheBonusNextSlot()
        {
            var events = _w.Resolve(Seq(_w.Slash, null, null), Seq(_w.Parry, _w.Kick, null));

            Assert.That(Clash(events, 0).Player.DamageTaken, Is.EqualTo(0));
            int kick = Attack + KickPower;
            Assert.That(Clash(events, 1).Player.DamageTaken, Is.EqualTo(kick * (100 + StaggerPercent) / 100)); // 6 × 1.3 = 7
        }

        [TestCase(MoveType.Dodge)]
        [TestCase(MoveType.Parry)]
        [TestCase(MoveType.Utility)]
        [TestCase(MoveType.Idle)]
        public void ParryAgainstNonAttack_DoesNothing(MoveType enemyType)
        {
            var enemyMove = enemyType == MoveType.Dodge ? _w.Evade
                : enemyType == MoveType.Parry ? _w.Parry
                : enemyType == MoveType.Utility ? _w.Rest
                : null;

            var events = _w.Resolve(Seq(_w.Parry, null, null), Seq(enemyMove, null, null));

            Assert.That(events.OfType<StatusAppliedEvent>(), Is.Empty, "GDD: a whiffed parry has no penalty.");
            Assert.That(Clash(events, 0).Player.DamageTaken, Is.EqualTo(0));
        }

        [Test]
        public void RestVsAttack_TakesTheHit_ButStillHealsAndGainsStamina()
        {
            int stamina = _w.Player.Stamina;
            var clash = Clash(_w.Resolve(Seq(_w.Rest, null, null), Seq(_w.Slash, null, null)), 0);

            Assert.That(clash.Player.DamageTaken, Is.EqualTo(SlashHit));
            Assert.That(clash.Player.Healed, Is.EqualTo(RestHeal));
            Assert.That(clash.Player.StaminaGained, Is.EqualTo(RestGain));
            Assert.That(_w.Player.Hp, Is.EqualTo(30 - SlashHit + RestHeal));
            Assert.That(_w.Player.Stamina, Is.EqualTo(stamina + RestGain));
        }

        [Test]
        public void Heal_IsCappedAtMaxHp()
        {
            var clash = Clash(_w.Resolve(Seq(_w.Rest, null, null), Seq(null, null, null)), 0);
            Assert.That(clash.Player.Healed, Is.EqualTo(0));
            Assert.That(_w.Player.Hp, Is.EqualTo(_w.Player.MaxHp));
        }

        [Test]
        public void Heal_DoesNotRevive_AfterAKnockout()
        {
            using (var w = new TestWorld(hp: SlashHit))
            {
                var clash = Clash(w.Resolve(Seq(w.Rest, null, null), Seq(w.Slash, null, null)), 0);
                Assert.That(clash.Player.Healed, Is.EqualTo(0));
                Assert.That(w.State.Outcome, Is.EqualTo(CombatOutcome.EnemyWon));
            }
        }

        [Test]
        public void StaminaGain_IsCappedAtMax()
        {
            using (var w = new TestWorld(stamina: 9, maxStamina: 10))
            {
                var clash = Clash(w.Resolve(Seq(w.Rest, null, null), Seq(null, null, null)), 0);
                Assert.That(clash.Player.StaminaGained, Is.EqualTo(1));
                Assert.That(w.Player.Stamina, Is.EqualTo(10));
            }
        }

        [Test]
        public void Enemy_NeverGainsStamina()
        {
            var clash = Clash(_w.Resolve(Seq(null, null, null), Seq(_w.Rest, null, null)), 0);
            Assert.That(clash.Enemy.StaminaGained, Is.EqualTo(0));
            Assert.That(_w.Enemy.Stamina, Is.EqualTo(0));
        }

        [Test]
        public void EmptySlot_ResolvesAsIdle_AndGetsHit()
        {
            var clash = Clash(_w.Resolve(Seq(null, null, null), Seq(_w.Slash, null, null)), 0);

            Assert.That(clash.Player.MoveType, Is.EqualTo(MoveType.Idle));
            Assert.That(clash.Enemy.Label, Is.EqualTo("Hit"));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(SlashHit));
        }

        [Test]
        public void StaggerFromLastSlot_CarriesIntoNextTurnSlot1_WhenEnabled()
        {
            _w.Resolve(Seq(null, null, _w.Parry), Seq(null, null, _w.Slash));
            Assert.That(_w.Resolver.EndTurn(_w.State), Is.Empty);

            var nextTurn = _w.Resolve(Seq(_w.Slash, null, null), Seq(null, null, null));
            Assert.That(Clash(nextTurn, 0).Enemy.StaggerBonusTaken, Is.GreaterThan(0));
        }

        [Test]
        public void StatusesAreCleared_AtTurnEnd_WhenCarryIsDisabled()
        {
            using (var w = new TestWorld(carryStatuses: false))
            {
                w.Resolve(Seq(null, null, w.Parry), Seq(null, null, w.Slash));
                var endEvents = w.Resolver.EndTurn(w.State);
                Assert.That(endEvents.OfType<StatusExpiredEvent>().Single().Status, Is.EqualTo(StatusType.Staggered));

                var nextTurn = w.Resolve(Seq(w.Slash, null, null), Seq(null, null, null));
                Assert.That(Clash(nextTurn, 0).Enemy.StaggerBonusTaken, Is.EqualTo(0));
            }
        }

        [Test]
        public void Knockout_StopsTheRemainingSlots()
        {
            using (var w = new TestWorld(hp: SlashHit))
            {
                var events = w.Resolve(Seq(w.Slash, w.Slash, w.Slash), Seq(w.Rest, w.Slash, w.Slash));

                Assert.That(events.OfType<ClashResolvedEvent>().Count(), Is.EqualTo(1));
                Assert.That(events.OfType<CombatantDefeatedEvent>().Single().Who, Is.EqualTo(CombatantId.Enemy));
                Assert.That(w.State.Outcome, Is.EqualTo(CombatOutcome.PlayerWon));
            }
        }

        [Test]
        public void SimultaneousKnockout_IsADraw()
        {
            using (var w = new TestWorld(hp: SlashHit))
            {
                w.Resolve(Seq(w.Slash, null, null), Seq(w.Slash, null, null));
                Assert.That(w.State.Outcome, Is.EqualTo(CombatOutcome.Draw));
            }
        }

        [Test]
        public void Resolution_IsDeterministic_IncludingCrits()
        {
            using (var a = new TestWorld(playerCrit: 50, enemyCrit: 50))
            using (var b = new TestWorld(playerCrit: 50, enemyCrit: 50))
            {
                for (int turn = 0; turn < 3; turn++)
                {
                    var eventsA = a.Resolve(Seq(a.Kick, a.Slash, a.Kick), Seq(a.Slash, a.Kick, a.Slash));
                    var eventsB = b.Resolve(Seq(b.Kick, b.Slash, b.Kick), Seq(b.Slash, b.Kick, b.Slash));
                    Assert.That(eventsB.Count, Is.EqualTo(eventsA.Count));
                }

                Assert.That(b.Player.Hp, Is.EqualTo(a.Player.Hp));
                Assert.That(b.Enemy.Hp, Is.EqualTo(a.Enemy.Hp));
            }
        }
    }
}
