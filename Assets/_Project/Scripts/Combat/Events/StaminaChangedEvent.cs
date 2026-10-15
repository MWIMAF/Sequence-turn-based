namespace Game.Combat
{
    /// <summary>
    /// Stamina changed outside a clash. (Stamina gained from moves like Evade/Rest is reported in SlotOutcome.)
    /// </summary>
    public sealed class StaminaChangedEvent : CombatEvent
    {
        public StaminaChangedEvent(int turn, CombatantId who, int delta, int staminaAfter, StaminaChangeReason reason)
            : base(turn, NoSlot)
        {
            Who = who;
            Delta = delta;
            StaminaAfter = staminaAfter;
            Reason = reason;
        }

        public CombatantId Who { get; }

        /// <summary>Negative when spent, positive when gained.</summary>
        public int Delta { get; }

        public int StaminaAfter { get; }
        public StaminaChangeReason Reason { get; }
    }
}
