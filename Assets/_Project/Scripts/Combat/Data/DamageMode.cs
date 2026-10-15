namespace Game.Combat
{
    /// <summary>
    /// Whether a move deals damage to the opponent in a given clash.
    /// </summary>
    public enum DamageMode
    {
        /// <summary>Deals no damage.</summary>
        None = 0,

        /// <summary>Deals damage from DamageCalculator (attack + skill power, crit, buff, stagger, defense).</summary>
        Full = 1,
    }
}
