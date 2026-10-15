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
            bool landedCrit,
            int damageTaken,
            int staggerBonusTaken,
            int healed,
            int staminaGained,
            int hpAfter,
            int staminaAfter)
        {
            Who = who;
            Move = move;
            OpponentMoveType = opponentMoveType;
            Label = label;
            DamageDealt = damageDealt;
            LandedCrit = landedCrit;
            DamageTaken = damageTaken;
            StaggerBonusTaken = staggerBonusTaken;
            Healed = healed;
            StaminaGained = staminaGained;
            HpAfter = hpAfter;
            StaminaAfter = staminaAfter;
        }

        public CombatantId Who { get; }

        /// <summary>The move that actually resolved (the Idle move if the slot was empty).</summary>
        public MoveDefinition Move { get; }

        public MoveType MoveType => Move.Type;
        public MoveType OpponentMoveType { get; }

        /// <summary>From the InteractionTable, e.g. Trade, Dodged, Parry!, Nothing.</summary>
        public string Label { get; }

        public int DamageDealt { get; }

        /// <summary>True if this side's attack was a critical hit.</summary>
        public bool LandedCrit { get; }

        /// <summary>Total damage taken, including any Stagger bonus.</summary>
        public int DamageTaken { get; }

        /// <summary>The part of DamageTaken that came from being Staggered.</summary>
        public int StaggerBonusTaken { get; }

        public int Healed { get; }
        public int StaminaGained { get; }
        public int HpAfter { get; }
        public int StaminaAfter { get; }
    }
}
