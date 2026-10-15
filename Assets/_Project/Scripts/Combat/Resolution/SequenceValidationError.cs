namespace Game.Combat
{
    public enum SequenceValidationError
    {
        None = 0,
        WrongSlotCount = 1,
        EmptySlotNotAllowed = 2,
        NotEnoughStamina = 3,
        ActionNotAvailable = 4,
    }
}
