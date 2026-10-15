using Game.AI;
using NUnit.Framework;
using static Game.Combat.Tests.TestWorld;

namespace Game.Combat.Tests
{
    /// <summary>Enemies follow an authored, looping pattern so the player can learn it.</summary>
    public sealed class PatternEnemyPlannerTests
    {
        [Test]
        public void PlaysTurnsInOrder_AndLoops()
        {
            using (var w = new TestWorld())
            {
                var pattern = new EnemyPattern(new[]
                {
                    new PatternTurn("Opener", w.Slash, w.Kick, w.Evade),
                    new PatternTurn("Bait", w.Parry, w.Rest, w.Slash),
                });
                var controller = new TurnController(w.State, w.Balance, w.Table, new PatternEnemyPlanner(pattern, w.Balance));

                string[] expected = { "[Slash | Kick | Evade]", "[Parry | Rest | Slash]", "[Slash | Kick | Evade]" };
                foreach (var plan in expected)
                {
                    controller.BeginTurn();
                    Assert.That(controller.EnemyPlan.ToString(), Is.EqualTo(plan));
                    controller.LockIn(Seq(w.Evade, null, null));
                    controller.CompletePlayback();
                }
            }
        }

        [Test]
        public void MissingOrEmptySlots_BecomeIdle()
        {
            using (var w = new TestWorld())
            {
                var pattern = new EnemyPattern(new[] { new PatternTurn("Short", w.Slash, null) });
                var plan = new PatternEnemyPlanner(pattern, w.Balance).Plan(w.State, CombatantId.Enemy);

                Assert.That(plan.Count, Is.EqualTo(3));
                Assert.That(plan[0], Is.SameAs(w.Slash));
                Assert.That(plan[1].Type, Is.EqualTo(MoveType.Idle));
                Assert.That(plan[2].Type, Is.EqualTo(MoveType.Idle));
            }
        }

        [Test]
        public void EmptyPattern_IdlesEverySlot()
        {
            using (var w = new TestWorld())
            {
                var plan = new PatternEnemyPlanner(new EnemyPattern(), w.Balance).Plan(w.State, CombatantId.Enemy);
                for (int i = 0; i < plan.Count; i++)
                {
                    Assert.That(plan[i].Type, Is.EqualTo(MoveType.Idle));
                }
            }
        }

        [Test]
        public void Planning_UsesNoRandomness()
        {
            using (var w = new TestWorld())
            {
                var pattern = new EnemyPattern(new[] { new PatternTurn("A", w.Slash, w.Slash, w.Parry) });
                uint before = w.State.Rng.State;

                new PatternEnemyPlanner(pattern, w.Balance).Plan(w.State, CombatantId.Enemy);

                Assert.That(w.State.Rng.State, Is.EqualTo(before));
            }
        }

        [Test]
        public void IndexForTurn_Loops()
        {
            using (var w = new TestWorld())
            {
                var pattern = new EnemyPattern(new[]
                {
                    new PatternTurn("A", w.Slash), new PatternTurn("B", w.Kick), new PatternTurn("C", w.Rest),
                });

                Assert.That(pattern.IndexForTurn(1), Is.EqualTo(0));
                Assert.That(pattern.IndexForTurn(3), Is.EqualTo(2));
                Assert.That(pattern.IndexForTurn(4), Is.EqualTo(0));
                Assert.That(pattern.IndexForTurn(8), Is.EqualTo(1));
            }
        }
    }
}
