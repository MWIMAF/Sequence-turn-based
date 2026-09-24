namespace Game.Combat
{
    /// <summary>
    /// The broad category of a move. Clash outcomes are decided per MoveType pair in the InteractionTable.
    /// </summary>
    /// <remarks>
    /// Idle is a real type (not "null") so an empty slot, or a slot cancelled by Staggered,
    /// still resolves through the InteractionTable like any other move.
    /// Values are explicit because Unity serializes enums as ints: never renumber them.
    /// </remarks>
    public enum MoveType
    {
        Idle = 0,
        Attack = 1,
        Block = 2,
        Parry = 3,
        Utility = 4,
    }
}
