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
    /// The only randomness is crit rolls, from the fight's seeded RNG: the same seed and sequences
    /// always give the same result. Clash outcomes themselves never roll dice.
    /// </remarks>
    public sealed class SequenceResolver
    {
        private const int PercentRoll = 100;

        private readonly InteractionTable _table;
        private readonly BalanceConfig _balance;

        public SequenceResolver(InteractionTable table, BalanceConfig balance)
        {
            _table = table ? table : throw new ArgumentNullException(nameof(table));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));

            if (_balance.IdleMove == null)
            {
                throw new ArgumentException("BalanceConfig needs an Idle Move for empty slots.", nameof(balance));
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
            int defenseConstant = _balance.DefenseConstantFor(state.EnemyTier);

            // A Staggered fighter's move is canceled: the slot plays as Idle, so the opponent gets a free hit.
            var playerMove = Resolved(player, playerPlanned);
            var enemyMove = Resolved(enemy, enemyPlanned);

            // 1. Each side's rule says what its own move does to the other.
            var playerRule = _table.Get(playerMove.Type, enemyMove.Type);
            var enemyRule = _table.Get(enemyMove.Type, playerMove.Type);

            // 2. Damage for both sides is computed before either is applied: the clash is simultaneous.
            //    Stagger is read now, so a Stagger caused in this slot only affects the next one.
            var toEnemy = ComputeDamage(state, player, enemy, playerMove, playerRule, defenseConstant);
            var toPlayer = ComputeDamage(state, enemy, player, enemyMove, enemyRule, defenseConstant);
            enemy.ApplyDamage(toEnemy.Total);
            player.ApplyDamage(toPlayer.Total);

            // 3. Move effects (stamina gain, heal). Utility still applies when hit, unless that hit was a knockout.
            int playerGain = playerRule.MoveEffectApplies ? player.GainStamina(playerMove.StaminaGain) : 0;
            int enemyGain = enemyRule.MoveEffectApplies ? enemy.GainStamina(enemyMove.StaminaGain) : 0;
            int playerHealed = playerRule.MoveEffectApplies ? player.Heal(playerMove.Heal) : 0;
            int enemyHealed = enemyRule.MoveEffectApplies ? enemy.Heal(enemyMove.Heal) : 0;

            events.Add(new ClashResolvedEvent(turn, slot,
                new SlotOutcome(CombatantId.Player, playerMove, enemyMove.Type, Label(player, playerPlanned, playerRule),
                    toEnemy.Total, toEnemy.IsCrit, toPlayer.Total, toPlayer.StaggerBonus,
                    playerHealed, playerGain, player.Hp, player.Stamina),
                new SlotOutcome(CombatantId.Enemy, enemyMove, playerMove.Type, Label(enemy, enemyPlanned, enemyRule),
                    toPlayer.Total, toPlayer.IsCrit, toEnemy.Total, toEnemy.StaggerBonus,
                    enemyHealed, enemyGain, enemy.Hp, enemy.Stamina)));

            // 4. Statuses that were active this slot count down.
            ExpireStatuses(player, turn, slot, events);
            ExpireStatuses(enemy, turn, slot, events);

            // 5. Statuses caused by this clash switch on for the next slot (or next turn's slot 1).
            QueueStatus(player, playerRule.SelfStatus, turn, slot, events);
            QueueStatus(enemy, playerRule.OpponentStatus, turn, slot, events);
            QueueStatus(enemy, enemyRule.SelfStatus, turn, slot, events);
            QueueStatus(player, enemyRule.OpponentStatus, turn, slot, events);
            player.PromotePendingStatuses();
            enemy.PromotePendingStatuses();

            // 6. A knockout ends the fight; later slots don't resolve.
            if (player.IsDefeated)
            {
                events.Add(new CombatantDefeatedEvent(turn, slot, CombatantId.Player));
            }

            if (enemy.IsDefeated)
            {
                events.Add(new CombatantDefeatedEvent(turn, slot, CombatantId.Enemy));
            }
        }

        private MoveDefinition Resolved(CombatantState who, MoveDefinition planned) =>
            Sequence.IsEmptySlot(planned) || who.HasActive(StatusType.Staggered) ? _balance.IdleMove : planned;

        private static string Label(CombatantState who, MoveDefinition planned, InteractionRule rule)
        {
            if (!who.HasActive(StatusType.Staggered)) return rule.Label;
            return Sequence.IsEmptySlot(planned) ? "Staggered" : $"Staggered: {planned.DisplayName} canceled";
        }

        private DamageResult ComputeDamage(CombatState state, CombatantState attacker, CombatantState target,
            MoveDefinition move, InteractionRule rule, int defenseConstant)
        {
            if (rule.Damage != DamageMode.Full)
            {
                return DamageResult.None;
            }

            bool isCrit = attacker.CritChancePercent > 0 && state.Rng.Next(PercentRoll) < attacker.CritChancePercent;
            bool staggered = target.HasActive(StatusType.Staggered);
            return DamageCalculator.Compute(attacker, target, move, isCrit, staggered, defenseConstant, _balance);
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
