using System.Collections.Generic;

namespace Game.Combat
{
    public sealed class LockInResult
    {
        private LockInResult(SequenceValidationResult validation, IReadOnlyList<CombatEvent> events)
        {
            Validation = validation;
            Events = events;
        }

        public bool Accepted => Validation.IsValid;
        public SequenceValidationResult Validation { get; }

        /// <summary>Everything that happened this turn, in playback order. Empty if rejected.</summary>
        public IReadOnlyList<CombatEvent> Events { get; }

        internal static LockInResult Rejected(SequenceValidationResult validation) =>
            new LockInResult(validation, new List<CombatEvent>());

        internal static LockInResult Resolved(IReadOnlyList<CombatEvent> events) =>
            new LockInResult(SequenceValidationResult.Valid, events);
    }
}
