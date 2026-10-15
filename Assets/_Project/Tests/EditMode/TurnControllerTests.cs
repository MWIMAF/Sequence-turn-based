using System;
using System.Linq;
using NUnit.Framework;
using static Game.Combat.Tests.TestWorld;

namespace Game.Combat.Tests
{
    public sealed class TurnControllerTests
    {
        [Test]
        public void EnemyPlan_IsFixedBeforeThePlayerLocksIn()
        {
            using (var w = new TestWorld())
            {
                var planner = new ScriptedPlanner(Seq(w.Slash, w.Slash, w.Parry));
                var controller = new TurnController(w.State, w.Balance, w.Table, planner);

                controller.BeginTurn();

                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Planning));
                Assert.That(planner.CallCount, Is.EqualTo(1));
                Assert.That(controller.EnemyPlan, Is.Not.Null);
            }
        }

        [Test]
        public void LockIn_RejectsAnActionNotInTheLoadout()
        {
            using (var w = new TestWorld())
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Bash, null, null));

                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.ActionNotAvailable));
            }
        }

        [Test]
        public void LockIn_RejectsUsingTheSameLoadoutActionTwice()
        {
            using (var w = new TestWorld(stamina: 10))
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Slash, w.Slash, null));

                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.ActionNotAvailable));
            }
        }

        [Test]
        public void LockIn_AllowsTheSameMoveTwice_WhenTwoBodyPartsGiveIt()
        {
            using (var w = new TestWorld(stamina: 10, twoSwords: true))
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                Assert.That(controller.LockIn(Seq(w.Slash, w.Slash, w.Rest)).Accepted, Is.True);
            }
        }

        [Test]
        public void LockIn_RejectsASequenceThePlayerCantAfford_AndChangesNothing()
        {
            using (var w = new TestWorld(stamina: SlashCost + 1))
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Slash, w.Parry, null));

                Assert.That(result.Accepted, Is.False);
                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.NotEnoughStamina));
                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Planning));
                Assert.That(w.Player.Stamina, Is.EqualTo(SlashCost + 1));
            }
        }

        [Test]
        public void LockIn_PaysUpFront_ThenEvadeAndRestGainsArriveDuringResolution()
        {
            using (var w = new TestWorld(stamina: 5))
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Slash, w.Evade, w.Rest));

                Assert.That(result.Accepted, Is.True);
                var paid = result.Events.OfType<StaminaChangedEvent>().Single();
                Assert.That(paid.Who, Is.EqualTo(CombatantId.Player));
                Assert.That(paid.Delta, Is.EqualTo(-SlashCost));
                Assert.That(w.Player.Stamina, Is.EqualTo(5 - SlashCost + EvadeGain + RestGain));
            }
        }

        [Test]
        public void Enemy_PaysNoStamina_ForItsPattern()
        {
            using (var w = new TestWorld(stamina: 0))
            {
                var controller = w.Controller(Seq(w.Slash, w.Slash, w.Parry));
                Assert.DoesNotThrow(() => controller.BeginTurn());

                var result = controller.LockIn(Seq(w.Evade, w.Rest, null));

                Assert.That(result.Accepted, Is.True);
                Assert.That(result.Events.OfType<StaminaChangedEvent>().Any(e => e.Who == CombatantId.Enemy), Is.False);
            }
        }

        [Test]
        public void LockIn_RejectsEmptySlots_WhenBalanceDisallowsThem()
        {
            using (var w = new TestWorld(allowEmptySlots: false))
            {
                var controller = w.Controller(Seq(w.Slash, w.Slash, w.Slash));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Evade, null, w.Rest));

                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.EmptySlotNotAllowed));
            }
        }

        [Test]
        public void LockIn_RejectsTheWrongNumberOfSlots()
        {
            using (var w = new TestWorld())
            {
                var controller = w.Controller(Seq(null, null, null));
                controller.BeginTurn();

                Assert.That(controller.LockIn(Seq(w.Evade, w.Rest)).Validation.Error,
                    Is.EqualTo(SequenceValidationError.WrongSlotCount));
            }
        }

        [Test]
        public void FullTurnLoop_MovesThroughPhases_AndAdvancesTheTurnNumber()
        {
            using (var w = new TestWorld())
            {
                var controller = w.Controller(Seq(null, null, null), Seq(null, null, null));

                controller.BeginTurn();
                controller.LockIn(Seq(w.Evade, w.Rest, null));
                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Playback));

                controller.CompletePlayback();
                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.TurnEnd));
                Assert.That(w.State.TurnNumber, Is.EqualTo(2));

                controller.BeginTurn();
                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Planning));
            }
        }

        [Test]
        public void FightEnds_InCombatOver_AfterAKnockout()
        {
            using (var w = new TestWorld(hp: SlashHit))
            {
                var controller = w.Controller(Seq(w.Rest, w.Rest, w.Rest));
                controller.BeginTurn();
                controller.LockIn(Seq(w.Slash, null, null));
                controller.CompletePlayback();

                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.CombatOver));
                Assert.Throws<InvalidOperationException>(() => controller.BeginTurn());
            }
        }

        [Test]
        public void PassiveRegen_OnlyAppliesToThePlayer()
        {
            using (var w = new TestWorld(stamina: 0, passiveRegen: 2))
            {
                var controller = w.Controller(Seq(null, null, null));

                var events = controller.BeginTurn();

                Assert.That(w.Player.Stamina, Is.EqualTo(2));
                Assert.That(events.OfType<StaminaChangedEvent>().Single().Who, Is.EqualTo(CombatantId.Player));
            }
        }

        [Test]
        public void InvalidEnemyPlan_IsCaughtAtPlanning()
        {
            using (var w = new TestWorld())
            {
                var controller = w.Controller(Seq(w.Slash, w.Slash));
                Assert.Throws<InvalidOperationException>(() => controller.BeginTurn());
            }
        }

        [Test]
        public void CallingStepsOutOfOrder_Throws()
        {
            using (var w = new TestWorld())
            {
                var controller = w.Controller(Seq(null, null, null));

                Assert.Throws<InvalidOperationException>(() => controller.LockIn(Seq(null, null, null)));
                Assert.Throws<InvalidOperationException>(() => controller.CompletePlayback());
            }
        }
    }
}
