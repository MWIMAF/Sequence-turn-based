namespace Game.Combat
{
    /// <summary>
    /// Something that happened during a turn. The simulation produces an ordered list of these;
    /// presentation plays them back and reads nothing else.
    /// </summary>
    public abstract class CombatEvent
    {
        /// <summary>Slot value for events that belong to the turn rather than a slot (lock-in, turn end).</summary>
        public const int NoSlot = -1;

        protected CombatEvent(int turn, int slot)
        {
            Turn = turn;
            Slot = slot;
        }

        public int Turn { get; }

        /// <summary>0-based slot index, or NoSlot.</summary>
        public int Slot { get; }
    }
}
