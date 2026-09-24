using System;
using System.Collections.Generic;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Baseline planner: fills each slot with a random affordable move from the pool.
    /// Useful as a sparring partner and as the "no strategy" reference when balancing smarter AIs.
    /// </summary>
    public sealed class RandomEnemyPlanner : IEnemyPlanner
    {
        private readonly List<MoveDefinition> _movePool;
        private readonly BalanceConfig _balance;

        public RandomEnemyPlanner(IEnumerable<MoveDefinition> movePool, BalanceConfig balance)
        {
            if (movePool == null) throw new ArgumentNullException(nameof(movePool));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));

            _movePool = new List<MoveDefinition>();
            foreach (var move in movePool)
            {
                // Idle is never *chosen*; it's only the fallback when nothing is affordable.
                if (move != null && move.Type != MoveType.Idle)
                {
                    _movePool.Add(move);
                }
            }
        }

        public Sequence Plan(CombatState state, CombatantId self)
        {
            var me = state.Get(self);
            int budget = me.Stamina;
            var slots = new MoveDefinition[_balance.SlotCount];
            var affordable = new List<MoveDefinition>(_movePool.Count);

            for (int slot = 0; slot < slots.Length; slot++)
            {
                affordable.Clear();
                foreach (var move in _movePool)
                {
                    if (move.StaminaCost <= budget)
                    {
                        affordable.Add(move);
                    }
                }

                if (affordable.Count == 0)
                {
                    slots[slot] = _balance.IdleMove;
                    continue;
                }

                var pick = affordable[state.Rng.Next(affordable.Count)];
                slots[slot] = pick;
                budget -= pick.StaminaCost;
            }

            return new Sequence(slots);
        }
    }
}
