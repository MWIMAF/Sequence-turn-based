using System;
using System.Collections.Generic;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Fills each slot with a random allowed move. Used as the "no strategy" autopilot in the simulator,
    /// the baseline to compare enemy patterns against.
    /// </summary>
    /// <remarks>
    /// Respects the fighter's rules: if it has a loadout, each action is used at most once per turn,
    /// and if it uses stamina, the sequence stays affordable.
    /// </remarks>
    public sealed class RandomEnemyPlanner : IEnemyPlanner
    {
        private readonly List<MoveDefinition> _movePool;
        private readonly BalanceConfig _balance;

        /// <param name="movePool">Used only when the fighter has no loadout (AvailableActions is null).</param>
        public RandomEnemyPlanner(IEnumerable<MoveDefinition> movePool, BalanceConfig balance)
        {
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
            _movePool = new List<MoveDefinition>();
            if (movePool != null)
            {
                foreach (var move in movePool)
                {
                    if (move != null && move.Type != MoveType.Idle) _movePool.Add(move);
                }
            }
        }

        public Sequence Plan(CombatState state, CombatantId self)
        {
            var me = state.Get(self);
            bool limited = me.AvailableActions != null;
            var remaining = new List<MoveDefinition>(limited ? me.AvailableActions : _movePool);
            remaining.RemoveAll(m => m == null || m.Type == MoveType.Idle);

            int budget = me.UsesStamina ? me.Stamina : int.MaxValue;
            var slots = new MoveDefinition[_balance.SlotCount];
            var affordable = new List<MoveDefinition>(remaining.Count);

            for (int slot = 0; slot < slots.Length; slot++)
            {
                affordable.Clear();
                foreach (var move in remaining)
                {
                    if (move.StaminaCost <= budget) affordable.Add(move);
                }

                if (affordable.Count == 0)
                {
                    slots[slot] = _balance.IdleMove;
                    continue;
                }

                var pick = affordable[state.Rng.Next(affordable.Count)];
                slots[slot] = pick;
                if (me.UsesStamina) budget -= pick.StaminaCost;
                if (limited) remaining.Remove(pick);
            }

            return new Sequence(slots);
        }
    }
}
