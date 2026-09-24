namespace Game.Combat
{
    /// <summary>
    /// Cross-slot statuses. These are what make a turn a *sequence* instead of three separate rounds.
    /// </summary>
    public enum StatusType
    {
        None = 0,

        /// <summary>The combatant's next slot is cancelled (becomes Idle). Caused by getting parried.</summary>
        Staggered = 1,

        /// <summary>The combatant takes bonus damage in the next slot. Caused by a whiffed parry.</summary>
        Exposed = 2,
    }
}
