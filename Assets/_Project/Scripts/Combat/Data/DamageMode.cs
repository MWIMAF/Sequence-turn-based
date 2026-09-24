namespace Game.Combat
{
    /// <summary>
    /// How much damage a move deals to the opponent in a given clash.
    /// </summary>
    public enum DamageMode
    {
        /// <summary>Deals no damage.</summary>
        None = 0,

        /// <summary>Deals the move's full Power (Attack hits, Parry counters).</summary>
        Full = 1,

        /// <summary>Deals Power minus the opponent move's Power, never below 0 (Attack into Block).</summary>
        ReducedByOpponentPower = 2,
    }
}
