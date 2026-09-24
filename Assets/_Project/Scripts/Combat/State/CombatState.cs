using System;

namespace Game.Combat
{
    /// <summary>
    /// Everything the simulation needs to continue a fight: both combatants, the turn number and the RNG.
    /// Deliberately holds no sequences, so an enemy planner reading it can never see the player's plan.
    /// </summary>
    public sealed class CombatState
    {
        public CombatState(CombatantState player, CombatantState enemy, int seed)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Seed = seed;
            Rng = new SeededRandom(seed);
            TurnNumber = 1;
        }

        public CombatantState Player { get; }
        public CombatantState Enemy { get; }
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

        /// <summary>Sets up a fresh fight from the balance config (player) and an enemy definition.</summary>
        public static CombatState Create(BalanceConfig balance, EnemyDefinition enemy, int seed)
        {
            var player = new CombatantState(
                CombatantId.Player, "Player", balance.PlayerMaxHp, balance.PlayerStartingStamina, balance.MaxStamina);
            var foe = new CombatantState(
                CombatantId.Enemy, enemy.DisplayName, enemy.MaxHp, enemy.StartingStamina, balance.MaxStamina);
            return new CombatState(player, foe, seed);
        }
    }
}
