namespace Game.Combat
{
    /// <summary>
    /// A combatant reached 0 HP. Remaining slots in the turn do not resolve.
    /// </summary>
    public sealed class CombatantDefeatedEvent : CombatEvent
    {
        public CombatantDefeatedEvent(int turn, int slot, CombatantId who)
            : base(turn, slot)
        {
            Who = who;
        }

        public CombatantId Who { get; }
    }
}
