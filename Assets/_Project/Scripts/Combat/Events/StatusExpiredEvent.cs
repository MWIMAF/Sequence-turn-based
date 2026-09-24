namespace Game.Combat
{
    /// <summary>
    /// A status wore off, either after its last slot or at turn end when statuses don't carry over.
    /// </summary>
    public sealed class StatusExpiredEvent : CombatEvent
    {
        public StatusExpiredEvent(int turn, int slot, CombatantId who, StatusType status)
            : base(turn, slot)
        {
            Who = who;
            Status = status;
        }

        public CombatantId Who { get; }
        public StatusType Status { get; }
    }
}
