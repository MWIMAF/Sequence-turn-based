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
                var planner = new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard));
                var controller = new TurnController(w.State, w.Balance, w.Table, planner);

                controller.BeginTurn();

                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Planning));
                Assert.That(planner.CallCount, Is.EqualTo(1));
                Assert.That(controller.EnemyPlan, Is.Not.Null);
            }
        }

        [Test]
        public void LockIn_RejectsASequenceThePlayerCantAfford_AndChangesNothing()
        {
            using (var w = new TestWorld(startingStamina: SlashCost + 1))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Slash, w.Slash, w.Guard));

                Assert.That(result.Accepted, Is.False);
                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.NotEnoughStamina));
                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.Planning));
                Assert.That(w.Player.Stamina, Is.EqualTo(SlashCost + 1));
            }
        }

        [Test]
        public void LockIn_PaysUpFront_ThenGuardGainsArriveDuringResolution()
        {
            using (var w = new TestWorld(startingStamina: 4))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Slash, w.Guard, w.Guard));

                Assert.That(result.Accepted, Is.True);
                var paid = result.Events.OfType<StaminaChangedEvent>().Single(e => e.Who == CombatantId.Player);
                Assert.That(paid.Delta, Is.EqualTo(-SlashCost));
                Assert.That(paid.StaminaAfter, Is.EqualTo(4 - SlashCost));
                Assert.That(w.Player.Stamina, Is.EqualTo(4 - SlashCost + 2 * GuardGain));
            }
        }

        [Test]
        public void LockIn_RejectsEmptySlots_WhenBalanceDisallowsThem()
        {
            using (var w = new TestWorld(allowEmptySlots: false))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Guard, null, w.Guard));

                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.EmptySlotNotAllowed));
            }
        }

        [Test]
        public void LockIn_RejectsTheWrongNumberOfSlots()
        {
            using (var w = new TestWorld())
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));
                controller.BeginTurn();

                var result = controller.LockIn(Seq(w.Guard, w.Guard));

                Assert.That(result.Validation.Error, Is.EqualTo(SequenceValidationError.WrongSlotCount));
            }
        }

        [Test]
        public void FullTurnLoop_MovesThroughPhases_AndAdvancesTheTurnNumber()
        {
            using (var w = new TestWorld())
            {
                var controller = new TurnController(w.State, w.Balance, w.Table,
                    new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard), Seq(w.Guard, w.Guard, w.Guard)));

                controller.BeginTurn();
                controller.LockIn(Seq(w.Guard, w.Guard, w.Guard));
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
            using (var w = new TestWorld(hp: SlashPower))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Charge, w.Charge, w.Charge)));
                controller.BeginTurn();
                controller.LockIn(Seq(w.Slash, w.Guard, w.Guard));
                controller.CompletePlayback();

                Assert.That(controller.Phase, Is.EqualTo(TurnPhase.CombatOver));
                Assert.Throws<InvalidOperationException>(() => controller.BeginTurn());
            }
        }

        [Test]
        public void PassiveRegen_IsAppliedAtTurnStart_WhenConfigured()
        {
            using (var w = new TestWorld(startingStamina: 0, passiveRegen: 2))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));

                var events = controller.BeginTurn();

                Assert.That(w.Player.Stamina, Is.EqualTo(2));
                Assert.That(events.OfType<StaminaChangedEvent>().Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void InvalidEnemyPlan_IsCaughtAtPlanning()
        {
            using (var w = new TestWorld(startingStamina: 0))
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Slash, w.Slash, w.Slash)));

                Assert.Throws<InvalidOperationException>(() => controller.BeginTurn());
            }
        }

        [Test]
        public void CallingStepsOutOfOrder_Throws()
        {
            using (var w = new TestWorld())
            {
                var controller = new TurnController(w.State, w.Balance, w.Table, new ScriptedPlanner(Seq(w.Guard, w.Guard, w.Guard)));

                Assert.Throws<InvalidOperationException>(() => controller.LockIn(Seq(w.Guard, w.Guard, w.Guard)));
                Assert.Throws<InvalidOperationException>(() => controller.CompletePlayback());
            }
        }
    }
}
