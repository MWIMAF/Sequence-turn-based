namespace Game.Combat
{
    /// <summary>
    /// The broad category of a move. Clash outcomes are decided per MoveType pair in the InteractionTable.
    /// </summary>
    /// <remarks>
    /// Idle is a real type (not "null") so an empty slot still resolves through the InteractionTable.
    /// Values are explicit because Unity serializes enums as ints: never renumber them.
    /// Dodge reuses value 2 (it replaced Block in the GDD), so old assets keep a valid value.
    /// </remarks>
    public enum MoveType
    {
        Idle = 0,
        Attack = 1,
        Dodge = 2,
        Parry = 3,
        Utility = 4,
    }
}
