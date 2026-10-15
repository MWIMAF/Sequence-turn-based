using System;
using System.Linq;

namespace Game.Combat
{
    /// <summary>
    /// Everything the simulation needs to continue a fight: both combatants, the enemy tier, the turn number
    /// and the RNG. Deliberately holds no sequences, so an enemy planner reading it can never see the player's plan.
    /// </summary>
    public sealed class CombatState
    {
        public CombatState(CombatantState player, CombatantState enemy, EnemyTier enemyTier, int seed)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            EnemyTier = enemyTier;
            Seed = seed;
            Rng = new SeededRandom(seed);
            TurnNumber = 1;
        }

        public CombatantState Player { get; }
        public CombatantState Enemy { get; }

        /// <summary>Picks the damage formula's C constant for this fight.</summary>
        public EnemyTier EnemyTier { get; }

        public int Seed { get; }
        public SeededRandom Rng { get; }
        public int TurnNumber { get; private set; }

        public CombatOutcome Outcome
        {
            get
            {
                if (Player.IsDefeated && Enemy.IsDefeated) return CombatOutcome.Draw;
                if (Enemy.IsDefeated) return CombatOutcome.PlayerWon;
                if (Player.IsDefeated) return CombatOutcome.EnemyWon;
                return CombatOutcome.Ongoing;
            }
        }

        public bool IsOver => Outcome != CombatOutcome.Ongoing;

        public CombatantState Get(CombatantId id) => id == CombatantId.Player ? Player : Enemy;

        public CombatantState OpponentOf(CombatantId id) => id == CombatantId.Player ? Enemy : Player;

        internal void AdvanceTurn() => TurnNumber++;

        /// <summary>
        /// Sets up a fight. The player's sequence is limited to their loadout actions and pays stamina;
        /// the enemy follows its pattern freely.
        /// </summary>
        public static CombatState Create(PlayerDefinition player, string enemyName, CombatStats enemyStats,
            EnemyTier enemyTier, int seed)
        {
            var you = new CombatantState(CombatantId.Player, player.DisplayName, player.Stats, usesStamina: true,
                availableActions: player.GetActions().Select(a => a.Move));
            var foe = new CombatantState(CombatantId.Enemy, enemyName, enemyStats, usesStamina: false);
            return new CombatState(you, foe, enemyTier, seed);
        }
    }
}
