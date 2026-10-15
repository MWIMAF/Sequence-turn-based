namespace Game.Combat
{
    /// <summary>
    /// Enemy rank. Picks the C constant in the damage formula (BalanceConfig): Normal &lt; Alpha &lt; Boss.
    /// </summary>
    public enum EnemyTier
    {
        Normal = 0,
        Alpha = 1,
        Boss = 2,
    }
}
