namespace Game.Combat
{
    /// <summary>
    /// Cross-slot statuses. These are what make a turn a *sequence* instead of three separate rounds.
    /// </summary>
    public enum StatusType
    {
        None = 0,

        /// <summary>
        /// Caused by getting parried. In the next slot the fighter's move is canceled (plays as Idle)
        /// and they take extra damage (BalanceConfig.StaggerDamageBonusPercent).
        /// </summary>
        Staggered = 1,
    }
}
