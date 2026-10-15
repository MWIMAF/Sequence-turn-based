using System.Linq;
using Game.AI;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    /// <summary>The random autopilot the simulator uses for the player.</summary>
    public sealed class RandomEnemyPlannerTests
    {
        [Test]
        public void SameSeed_GivesTheSamePlans()
        {
            using (var a = new TestWorld())
            using (var b = new TestWorld())
            {
                var plannerA = new RandomEnemyPlanner(null, a.Balance);
                var plannerB = new RandomEnemyPlanner(null, b.Balance);

                for (int i = 0; i < 20; i++)
                {
                    Assert.That(plannerB.Plan(b.State, CombatantId.Player).ToString(),
                        Is.EqualTo(plannerA.Plan(a.State, CombatantId.Player).ToString()));
                }
            }
        }

        [Test]
        public void PlayerPlans_AreAlwaysValid_ForEveryStaminaLevel()
        {
            for (int stamina = 0; stamina <= 10; stamina++)
            {
                using (var w = new TestWorld(stamina: stamina))
                {
                    var planner = new RandomEnemyPlanner(null, w.Balance);
                    for (int i = 0; i < 50; i++)
                    {
                        var plan = planner.Plan(w.State, CombatantId.Player);
                        var result = SequenceValidator.Validate(plan, w.Player, w.Balance);
                        Assert.That(result.IsValid, Is.True, $"stamina {stamina}: {plan} → {result.Message}");
                    }
                }
            }
        }

        [Test]
        public void PlayerPlans_NeverRepeatASingleLoadoutAction()
        {
            using (var w = new TestWorld(stamina: 10))
            {
                var planner = new RandomEnemyPlanner(null, w.Balance);
                for (int i = 0; i < 100; i++)
                {
                    var plan = planner.Plan(w.State, CombatantId.Player);
                    var moves = Enumerable.Range(0, plan.Count).Select(s => plan[s]).Where(m => m.Type != MoveType.Idle).ToList();
                    Assert.That(moves.Distinct().Count(), Is.EqualTo(moves.Count), plan.ToString());
                }
            }
        }
    }
}
