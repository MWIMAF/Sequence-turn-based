namespace Game.Combat
{
    /// <summary>
    /// One side's result in a single slot clash.
    /// </summary>
    public sealed class SlotOutcome
    {
        public SlotOutcome(
            CombatantId who,
            MoveDefinition move,
            MoveType opponentMoveType,
            string label,
            int damageDealt,
            int damageTaken,
            int exposedBonusTaken,
            int staminaGained,
            int hpAfter,
            int staminaAfter)
        {
            Who = who;
            Move = move;
            OpponentMoveType = opponentMoveType;
            Label = label;
            DamageDealt = damageDealt;
            DamageTaken = damageTaken;
            ExposedBonusTaken = exposedBonusTaken;
            StaminaGained = staminaGained;
            HpAfter = hpAfter;
            StaminaAfter = staminaAfter;
        }

        public CombatantId Who { get; }

        /// <summary>The move that actually resolved (the Idle move if the slot was empty or cancelled).</summary>
        public MoveDefinition Move { get; }

        public MoveType MoveType => Move.Type;
        public MoveType OpponentMoveType { get; }

        /// <summary>From the InteractionTable, e.g. Trade, Blocked, Parry!, Whiff.</summary>
        public string Label { get; }

        public int DamageDealt { get; }

        /// <summary>Total damage taken, including any Exposed bonus.</summary>
        public int DamageTaken { get; }

        /// <summary>The part of DamageTaken that came from being Exposed.</summary>
        public int ExposedBonusTaken { get; }

        public int StaminaGained { get; }
        public int HpAfter { get; }
        public int StaminaAfter { get; }
    }
}
