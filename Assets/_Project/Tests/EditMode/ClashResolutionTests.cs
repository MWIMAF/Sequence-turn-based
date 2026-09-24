using System.Linq;
using NUnit.Framework;
using static Game.Combat.Tests.TestWorld;

namespace Game.Combat.Tests
{
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
        public void AttackVsAttack_BothTakeTheOthersDamage()
        {
            var events = _w.Resolve(Seq(_w.Slash, _w.Guard, _w.Guard), Seq(_w.Slash, _w.Guard, _w.Guard));

            var clash = Clash(events, 0);
            Assert.That(clash.Player.Label, Is.EqualTo("Trade"));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(SlashPower));
            Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashPower));
            Assert.That(_w.Player.Hp, Is.EqualTo(20 - SlashPower));
            Assert.That(_w.Enemy.Hp, Is.EqualTo(20 - SlashPower));
        }

        [Test]
        public void AttackVsBlock_BlockAbsorbsItsPower_AndGuardGainsStamina()
        {
            var events = _w.Resolve(Seq(_w.Slash, null, null), Seq(_w.Guard, null, null));

            var clash = Clash(events, 0);
            Assert.That(clash.Player.Label, Is.EqualTo("Blocked"));
            Assert.That(clash.Enemy.DamageTaken, Is.EqualTo(SlashPower - GuardPower));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(0));
            Assert.That(clash.Enemy.StaminaGained, Is.EqualTo(GuardGain));
        }

        [Test]
        public void ParryVsAttack_CountersAndStaggers_CancellingAttackersNextSlot()
        {
            var events = _w.Resolve(Seq(_w.Parry, _w.Slash, _w.Guard), Seq(_w.Slash, _w.Slash, _w.Guard));

            var slot1 = Clash(events, 0);
            Assert.That(slot1.Player.Label, Is.EqualTo("Parry!"));
            Assert.That(slot1.Enemy.DamageTaken, Is.EqualTo(ParryPower));
            Assert.That(slot1.Player.DamageTaken, Is.EqualTo(0));
            Assert.That(events.OfType<StatusAppliedEvent>().Single(e => e.Slot == 0).Who, Is.EqualTo(CombatantId.Enemy));

            var cancelled = events.OfType<MoveCancelledEvent>().Single();
            Assert.That(cancelled.Slot, Is.EqualTo(1));
            Assert.That(cancelled.Who, Is.EqualTo(CombatantId.Enemy));
            Assert.That(cancelled.Cause, Is.EqualTo(StatusType.Staggered));

            var slot2 = Clash(events, 1);
            Assert.That(slot2.Enemy.MoveType, Is.EqualTo(MoveType.Idle));
            Assert.That(slot2.Enemy.DamageTaken, Is.EqualTo(SlashPower), "Staggered enemy stands idle and eats the hit.");

            var slot3 = Clash(events, 2);
            Assert.That(slot3.Enemy.MoveType, Is.EqualTo(MoveType.Block), "Stagger lasts one slot only.");
        }

        [Test]
        public void ParryVsBlock_ParrierIsExposed_AndTakesBonusDamageNextSlot()
        {
            var events = _w.Resolve(Seq(_w.Parry, _w.Charge, _w.Guard), Seq(_w.Guard, _w.Slash, _w.Guard));

            Assert.That(Clash(events, 0).Player.Label, Is.EqualTo("Whiff"));
            var applied = events.OfType<StatusAppliedEvent>().Single(e => e.Slot == 0);
            Assert.That(applied.Who, Is.EqualTo(CombatantId.Player));
            Assert.That(applied.Status, Is.EqualTo(StatusType.Exposed));

            var slot2 = Clash(events, 1);
            int bonus = SlashPower * ExposedPercent / 100;
            Assert.That(slot2.Player.DamageTaken, Is.EqualTo(SlashPower + bonus));
            Assert.That(slot2.Player.ExposedBonusTaken, Is.EqualTo(bonus));

            var expired = events.OfType<StatusExpiredEvent>().Single();
            Assert.That(expired.Slot, Is.EqualTo(1), "Exposed wears off after the slot it affected.");
        }

        [Test]
        public void BlockVsParry_GivesTheSameResultFromEitherSide()
        {
            var events = _w.Resolve(Seq(_w.Guard, null, null), Seq(_w.Parry, null, null));

            var exposed = events.OfType<StatusAppliedEvent>().Single();
            Assert.That(exposed.Who, Is.EqualTo(CombatantId.Enemy), "The parrier is Exposed, never the blocker.");
            Assert.That(exposed.Status, Is.EqualTo(StatusType.Exposed));
        }

        [Test]
        public void ParryVsParry_BothWhiffAndAreExposed()
        {
            var events = _w.Resolve(Seq(_w.Parry, null, null), Seq(_w.Parry, null, null));

            var applied = events.OfType<StatusAppliedEvent>().Where(e => e.Slot == 0).ToList();
            Assert.That(applied.Select(e => e.Who), Is.EquivalentTo(new[] { CombatantId.Player, CombatantId.Enemy }));
            Assert.That(applied.All(e => e.Status == StatusType.Exposed), Is.True);
        }

        [Test]
        public void ChargeVsAttack_TakesTheFullHit_ButStillGainsStamina()
        {
            int before = _w.Player.Stamina;
            var events = _w.Resolve(Seq(_w.Charge, null, null), Seq(_w.Slash, null, null));

            var clash = Clash(events, 0);
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(SlashPower));
            Assert.That(clash.Player.StaminaGained, Is.EqualTo(ChargeGain));
            Assert.That(_w.Player.Stamina, Is.EqualTo(before + ChargeGain));
        }

        [Test]
        public void StaminaGain_IsCappedAtMax()
        {
            using (var w = new TestWorld(startingStamina: 9, maxStamina: 10))
            {
                var events = w.Resolve(Seq(w.Charge, null, null), Seq(null, null, null));

                Assert.That(Clash(events, 0).Player.StaminaGained, Is.EqualTo(1));
                Assert.That(w.Player.Stamina, Is.EqualTo(10));
            }
        }

        [Test]
        public void EmptySlot_ResolvesAsIdle_AndGetsHit()
        {
            var events = _w.Resolve(Seq(null, null, null), Seq(_w.Slash, null, null));

            var clash = Clash(events, 0);
            Assert.That(clash.Player.MoveType, Is.EqualTo(MoveType.Idle));
            Assert.That(clash.Enemy.Label, Is.EqualTo("Hit"));
            Assert.That(clash.Player.DamageTaken, Is.EqualTo(SlashPower));
        }

        [Test]
        public void StaggerFromLastSlot_CarriesIntoNextTurnSlot1_WhenEnabled()
        {
            _w.Resolve(Seq(_w.Guard, _w.Guard, _w.Parry), Seq(_w.Guard, _w.Guard, _w.Slash));
            Assert.That(_w.Resolver.EndTurn(_w.State), Is.Empty);

            var nextTurn = _w.Resolve(Seq(_w.Guard, _w.Guard, _w.Guard), Seq(_w.Slash, _w.Guard, _w.Guard));

            var cancelled = nextTurn.OfType<MoveCancelledEvent>().Single();
            Assert.That(cancelled.Slot, Is.EqualTo(0));
            Assert.That(cancelled.Who, Is.EqualTo(CombatantId.Enemy));
        }

        [Test]
        public void StatusesAreCleared_AtTurnEnd_WhenCarryIsDisabled()
        {
            using (var w = new TestWorld(carryStatuses: false))
            {
                w.Resolve(Seq(w.Guard, w.Guard, w.Parry), Seq(w.Guard, w.Guard, w.Slash));
                var endEvents = w.Resolver.EndTurn(w.State);

                Assert.That(endEvents.OfType<StatusExpiredEvent>().Single().Status, Is.EqualTo(StatusType.Staggered));

                var nextTurn = w.Resolve(Seq(w.Guard, w.Guard, w.Guard), Seq(w.Slash, w.Guard, w.Guard));
                Assert.That(nextTurn.OfType<MoveCancelledEvent>(), Is.Empty);
            }
        }

        [Test]
        public void Knockout_StopsTheRemainingSlots()
        {
            using (var w = new TestWorld(hp: SlashPower))
            {
                var events = w.Resolve(Seq(w.Slash, w.Slash, w.Slash), Seq(w.Charge, w.Slash, w.Slash));

                Assert.That(events.OfType<ClashResolvedEvent>().Count(), Is.EqualTo(1));
                Assert.That(events.OfType<CombatantDefeatedEvent>().Single().Who, Is.EqualTo(CombatantId.Enemy));
                Assert.That(w.State.Outcome, Is.EqualTo(CombatOutcome.PlayerWon));
            }
        }

        [Test]
        public void SimultaneousKnockout_IsADraw()
        {
            using (var w = new TestWorld(hp: SlashPower))
            {
                w.Resolve(Seq(w.Slash, null, null), Seq(w.Slash, null, null));

                Assert.That(w.State.Outcome, Is.EqualTo(CombatOutcome.Draw));
            }
        }

        [Test]
        public void Resolution_IsDeterministic()
        {
            using (var a = new TestWorld())
            using (var b = new TestWorld())
            {
                var eventsA = a.Resolve(Seq(a.Parry, a.Slash, a.Charge), Seq(a.Slash, a.Parry, a.Guard));
                var eventsB = b.Resolve(Seq(b.Parry, b.Slash, b.Charge), Seq(b.Slash, b.Parry, b.Guard));

                Assert.That(eventsB.Count, Is.EqualTo(eventsA.Count));
                Assert.That(b.Player.Hp, Is.EqualTo(a.Player.Hp));
                Assert.That(b.Enemy.Hp, Is.EqualTo(a.Enemy.Hp));
                Assert.That(b.Player.Stamina, Is.EqualTo(a.Player.Stamina));
            }
        }
    }
}
