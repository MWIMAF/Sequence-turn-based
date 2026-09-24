namespace Game.Combat
{
    /// <summary>
    /// Checks a sequence can be locked in. Used for the player's plan and to catch buggy enemy planners.
    /// </summary>
    public static class SequenceValidator
    {
        public static SequenceValidationResult Validate(Sequence sequence, CombatantState owner, BalanceConfig balance)
        {
            if (sequence == null || sequence.Count != balance.SlotCount)
            {
                int count = sequence?.Count ?? 0;
                return new SequenceValidationResult(
                    SequenceValidationError.WrongSlotCount,
                    $"Sequence has {count} slots; it needs {balance.SlotCount}.");
            }

            if (!balance.AllowEmptySlots)
            {
                for (int slot = 0; slot < sequence.Count; slot++)
                {
                    if (Sequence.IsEmptySlot(sequence[slot]))
                    {
                        return new SequenceValidationResult(
                            SequenceValidationError.EmptySlotNotAllowed,
                            $"Slot {slot + 1} is empty.");
                    }
                }
            }

            // Stamina is paid up front for the whole sequence, so stamina gained mid-turn
            // (Guard, Charge) can only be spent next turn. This keeps the budget readable:
            // the opponent's current stamina caps how many costly moves they can have planned.
            int cost = sequence.TotalStaminaCost;
            if (cost > owner.Stamina)
            {
                return new SequenceValidationResult(
                    SequenceValidationError.NotEnoughStamina,
                    $"Sequence costs {cost} stamina; {owner.DisplayName} has {owner.Stamina}.");
            }

            return SequenceValidationResult.Valid;
        }
    }
}
