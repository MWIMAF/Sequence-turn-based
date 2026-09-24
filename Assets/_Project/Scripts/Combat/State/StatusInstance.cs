namespace Game.Combat
{
    /// <summary>
    /// A status currently on a combatant, and how many more slots it will affect.
    /// </summary>
    public sealed class StatusInstance
    {
        public StatusInstance(StatusType type, int slotsRemaining)
        {
            Type = type;
            SlotsRemaining = slotsRemaining;
        }

        public StatusType Type { get; }
        public int SlotsRemaining { get; internal set; }

        public override string ToString() => $"{Type} ({SlotsRemaining})";
    }
}
