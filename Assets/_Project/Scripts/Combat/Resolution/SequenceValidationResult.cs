namespace Game.Combat
{
    public readonly struct SequenceValidationResult
    {
        public SequenceValidationResult(SequenceValidationError error, string message)
        {
            Error = error;
            Message = message;
        }

        public static SequenceValidationResult Valid => new SequenceValidationResult(SequenceValidationError.None, string.Empty);

        public SequenceValidationError Error { get; }
        public string Message { get; }
        public bool IsValid => Error == SequenceValidationError.None;
    }
}
