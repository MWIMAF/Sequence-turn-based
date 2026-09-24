namespace Game.Combat
{
    /// <summary>
    /// Both moves in one slot met and resolved simultaneously.
    /// </summary>
    public sealed class ClashResolvedEvent : CombatEvent
    {
        public ClashResolvedEvent(int turn, int slot, SlotOutcome player, SlotOutcome enemy)
            : base(turn, slot)
        {
            Player = player;
            Enemy = enemy;
        }

        public SlotOutcome Player { get; }
        public SlotOutcome Enemy { get; }

        public SlotOutcome For(CombatantId id) => id == CombatantId.Player ? Player : Enemy;
    }
}
