namespace Game.Combat
{
    /// <summary>
    /// A status was caused in this slot. It takes effect from the next slot.
    /// </summary>
    public sealed class StatusAppliedEvent : CombatEvent
    {
        public StatusAppliedEvent(int turn, int slot, CombatantId who, StatusType status, int durationSlots)
            : base(turn, slot)
        {
            Who = who;
            Status = status;
            DurationSlots = durationSlots;
        }

        public CombatantId Who { get; }
        public StatusType Status { get; }
        public int DurationSlots { get; }
    }
}
