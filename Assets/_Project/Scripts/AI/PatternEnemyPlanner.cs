using System;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Plays an authored EnemyPattern: turn 1 uses the first pattern entry, then the next, looping.
    /// No randomness, so the player can learn it.
    /// </summary>
    public sealed class PatternEnemyPlanner : IEnemyPlanner
    {
        private readonly EnemyPattern _pattern;
        private readonly BalanceConfig _balance;

        public PatternEnemyPlanner(EnemyPattern pattern, BalanceConfig balance)
        {
            _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
        }

        public Sequence Plan(CombatState state, CombatantId self)
        {
            var slots = new MoveDefinition[_balance.SlotCount];
            var turn = _pattern.ForTurn(state.TurnNumber);

            for (int i = 0; i < slots.Length; i++)
            {
                // Missing or empty entries become Idle, so a half-written pattern still runs.
                var move = turn != null && i < turn.Slots.Count ? turn.Slots[i] : null;
                slots[i] = move != null ? move : _balance.IdleMove;
            }

            return new Sequence(slots);
        }
    }
}
