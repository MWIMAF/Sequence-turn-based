using System;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Turns an EnemyDefinition's PlannerKind into a planner instance.
    /// Game.Combat can't reference Game.AI, so the mapping lives here.
    /// </summary>
    public static class EnemyPlannerFactory
    {
        public static IEnemyPlanner Create(EnemyDefinition enemy, BalanceConfig balance)
        {
            switch (enemy.PlannerKind)
            {
                case EnemyPlannerKind.Random:
                    return new RandomEnemyPlanner(enemy.MovePool, balance);
                default:
                    throw new ArgumentOutOfRangeException(nameof(enemy), $"No planner for kind {enemy.PlannerKind}.");
            }
        }
    }
}
