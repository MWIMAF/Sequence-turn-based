using System;
using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// Resolves a whole turn instantly, slot 1 to slot N, and returns what happened as CombatEvents.
    /// Never waits on animation; presentation plays the events back afterwards.
    /// </summary>
    /// <remarks>
    /// Sequences must already be validated and paid for (TurnController.LockIn does both).
    /// No randomness is used here: the same state and sequences always give the same result.
    /// </remarks>
    public sealed class SequenceResolver
    {
        private readonly InteractionTable _table;
        private readonly BalanceConfig _balance;

        public SequenceResolver(InteractionTable table, BalanceConfig balance)
        {
            _table = table ? table : throw new ArgumentNullException(nameof(table));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));

            if (_balance.IdleMove == null)
            {
                throw new ArgumentException("BalanceConfig needs an Idle Move for empty and cancelled slots.", nameof(balance));
            }
        }

        public List<CombatEvent> Resolve(CombatState state, Sequence player, Sequence enemy)
        {
            var events = new List<CombatEvent>();
            for (int slot = 0; slot < _balance.SlotCount; slot++)
            {
                if (state.IsOver)
                {
                    break;
                }

                ResolveSlot(state, slot, player[slot], enemy[slot], events);
            }

            return events;
        }

        /// <summary>
        /// End-of-turn status handling. Statuses either carry into next turn's slot 1 or are cleared.
        /// </summary>
        public List<CombatEvent> EndTurn(CombatState state)
        {
            var events = new List<CombatEvent>();
            if (_balance.StatusesCarryAcrossTurns)
            {
                return events;
            }

            foreach (var combatant in new[] { state.Player, state.Enemy })
            {
                foreach (var status in combatant.ClearAllStatuses())
                {
                    events.Add(new StatusExpiredEvent(state.TurnNumber, CombatEvent.NoSlot, combatant.Id, status));
                }
            }

            return events;
        }

        private void ResolveSlot(CombatState state, int slot, MoveDefinition playerPlanned, MoveDefinition enemyPlanned,
            List<CombatEvent> events)
        {
            int turn = state.TurnNumber;
            var player = state.Player;
            var enemy = state.Enemy;

            // 1. Staggered cancels this slot's move before anything clashes.
            var playerMove = EffectiveMove(player, playerPlanned, turn, slot, events);
            var enemyMove = EffectiveMove(enemy, enemyPlanned, turn, slot, events);

            // 2. Each side's rule says what its own move does to the other.
            var playerRule = _table.Get(playerMove.Type, enemyMove.Type);
            var enemyRule = _table.Get(enemyMove.Type, playerMove.Type);

            // 3. Damage is computed for both sides before either is applied: the clash is simultaneous.
            int toEnemy = BaseDamage(playerRule, playerMove, enemyMove);
            int toPlayer = BaseDamage(enemyRule, enemyMove, playerMove);
            int enemyExposedBonus = ExposedBonus(enemy, toEnemy);
            int playerExposedBonus = ExposedBonus(player, toPlayer);
            toEnemy += enemyExposedBonus;
            toPlayer += playerExposedBonus;

            enemy.ApplyDamage(toEnemy);
            player.ApplyDamage(toPlayer);

            // 4. Move effects (stamina gain from Guard / Charge).
            int playerGain = playerRule.MoveEffectApplies ? player.GainStamina(playerMove.StaminaGain) : 0;
            int enemyGain = enemyRule.MoveEffectApplies ? enemy.GainStamina(enemyMove.StaminaGain) : 0;

            events.Add(new ClashResolvedEvent(turn, slot,
                new SlotOutcome(CombatantId.Player, playerMove, enemyMove.Type, playerRule.Label,
                    toEnemy, toPlayer, playerExposedBonus, playerGain, player.Hp, player.Stamina),
                new SlotOutcome(CombatantId.Enemy, enemyMove, playerMove.Type, enemyRule.Label,
                    toPlayer, toEnemy, enemyExposedBonus, enemyGain, enemy.Hp, enemy.Stamina)));

            // 5. Statuses that were active this slot count down.
            ExpireStatuses(player, turn, slot, events);
            ExpireStatuses(enemy, turn, slot, events);

            // 6. Statuses caused by this clash switch on for the next slot (or next turn's slot 1).
            QueueStatus(player, playerRule.SelfStatus, turn, slot, events);
            QueueStatus(enemy, playerRule.OpponentStatus, turn, slot, events);
            QueueStatus(enemy, enemyRule.SelfStatus, turn, slot, events);
            QueueStatus(player, enemyRule.OpponentStatus, turn, slot, events);
            player.PromotePendingStatuses();
            enemy.PromotePendingStatuses();

            // 7. A knockout ends the fight; later slots don't resolve.
            if (player.IsDefeated)
            {
                events.Add(new CombatantDefeatedEvent(turn, slot, CombatantId.Player));
            }

            if (enemy.IsDefeated)
            {
                events.Add(new CombatantDefeatedEvent(turn, slot, CombatantId.Enemy));
            }
        }

        private MoveDefinition EffectiveMove(CombatantState who, MoveDefinition planned, int turn, int slot,
            List<CombatEvent> events)
        {
            if (Sequence.IsEmptySlot(planned))
            {
                return _balance.IdleMove;
            }

            if (who.HasActive(StatusType.Staggered))
            {
                events.Add(new MoveCancelledEvent(turn, slot, who.Id, planned, StatusType.Staggered));
                return _balance.IdleMove;
            }

            return planned;
        }

        private static int BaseDamage(InteractionRule rule, MoveDefinition own, MoveDefinition opponent)
        {
            switch (rule.Damage)
            {
                case DamageMode.Full:
                    return own.Power;
                case DamageMode.ReducedByOpponentPower:
                    return Math.Max(0, own.Power - opponent.Power);
                default:
                    return 0;
            }
        }

        private int ExposedBonus(CombatantState target, int incomingDamage)
        {
            if (incomingDamage <= 0 || !target.HasActive(StatusType.Exposed))
            {
                return 0;
            }

            // Integer percent, rounded down, so HP stays an int.
            return incomingDamage * _balance.ExposedDamageBonusPercent / 100;
        }

        private static void ExpireStatuses(CombatantState who, int turn, int slot, List<CombatEvent> events)
        {
            foreach (var status in who.TickActiveStatuses())
            {
                events.Add(new StatusExpiredEvent(turn, slot, who.Id, status));
            }
        }

        private void QueueStatus(CombatantState who, StatusType status, int turn, int slot, List<CombatEvent> events)
        {
            if (status == StatusType.None)
            {
                return;
            }

            int duration = _balance.DurationFor(status);
            who.QueueStatus(status, duration);
            events.Add(new StatusAppliedEvent(turn, slot, who.Id, status, duration));
        }
    }
}
