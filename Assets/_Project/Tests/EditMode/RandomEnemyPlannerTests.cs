using System.Linq;
using Game.AI;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    public sealed class RandomEnemyPlannerTests
    {
        [Test]
        public void SameSeed_GivesTheSamePlans()
        {
            using (var a = new TestWorld())
            using (var b = new TestWorld())
            {
                var plannerA = new RandomEnemyPlanner(a.AllMoves, a.Balance);
                var plannerB = new RandomEnemyPlanner(b.AllMoves, b.Balance);

                for (int i = 0; i < 20; i++)
                {
                    Assert.That(plannerB.Plan(b.State, CombatantId.Enemy).ToString(),
                        Is.EqualTo(plannerA.Plan(a.State, CombatantId.Enemy).ToString()));
                }
            }
        }

        [Test]
        public void Plans_AreAlwaysValid_ForEveryStaminaLevel()
        {
            for (int stamina = 0; stamina <= 10; stamina++)
            {
                using (var w = new TestWorld(startingStamina: stamina))
                {
                    var planner = new RandomEnemyPlanner(w.AllMoves, w.Balance);
                    for (int i = 0; i < 50; i++)
                    {
                        var plan = planner.Plan(w.State, CombatantId.Enemy);
                        var result = SequenceValidator.Validate(plan, w.Enemy, w.Balance);
                        Assert.That(result.IsValid, Is.True, $"stamina {stamina}: {plan} → {result.Message}");
                    }
                }
            }
        }

        [Test]
        public void Planner_NeverPicksIdle_WhenAFreeMoveExists()
        {
            using (var w = new TestWorld(startingStamina: 0))
            {
                var planner = new RandomEnemyPlanner(w.AllMoves, w.Balance);
                for (int i = 0; i < 50; i++)
                {
                    var plan = planner.Plan(w.State, CombatantId.Enemy);
                    Assert.That(Enumerable.Range(0, plan.Count).All(s => plan[s].Type != MoveType.Idle), Is.True);
                }
            }
        }
    }
}
