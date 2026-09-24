namespace Game.Combat
{
    /// <summary>
    /// A planned move was replaced with Idle before its slot resolved (e.g. the combatant was Staggered).
    /// </summary>
    public sealed class MoveCancelledEvent : CombatEvent
    {
        public MoveCancelledEvent(int turn, int slot, CombatantId who, MoveDefinition cancelledMove, StatusType cause)
            : base(turn, slot)
        {
            Who = who;
            CancelledMove = cancelledMove;
            Cause = cause;
        }

        public CombatantId Who { get; }
        public MoveDefinition CancelledMove { get; }
        public StatusType Cause { get; }
    }
}
