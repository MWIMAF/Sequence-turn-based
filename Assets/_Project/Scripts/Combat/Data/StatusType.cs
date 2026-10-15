namespace Game.Combat
{
    /// <summary>
    /// Cross-slot statuses. These are what make a turn a *sequence* instead of three separate rounds.
    /// </summary>
    public enum StatusType
    {
        None = 0,

        /// <summary>
        /// Takes extra damage (BalanceConfig.StaggerDamageBonusPercent) in the next slot. Caused by getting parried.
        /// </summary>
        Staggered = 1,
    }
}
