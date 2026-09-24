namespace Game.Combat
{
    public enum StaminaChangeReason
    {
        /// <summary>Paid at lock-in for the whole sequence.</summary>
        SequenceCost = 0,

        /// <summary>BalanceConfig passive regen at turn start.</summary>
        TurnStartRegen = 1,
    }
}
