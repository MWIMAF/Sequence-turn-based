using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// Checks a sequence can be locked in. Used for the player's plan and to catch broken enemy patterns.
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
                            SequenceValidationError.EmptySlotNotAllowed, $"Slot {slot + 1} is empty.");
                    }
                }
            }

            var unavailable = FindUnavailableAction(sequence, owner.AvailableActions);
            if (unavailable != null)
            {
                return new SequenceValidationResult(
                    SequenceValidationError.ActionNotAvailable,
                    $"{unavailable.DisplayName} isn't available (each loadout action can be used once per turn).");
            }

            // Stamina is paid up front for the whole sequence, so stamina gained mid-turn
            // (Evade, Rest) can only be spent next turn. Enemies don't use stamina.
            int cost = sequence.TotalStaminaCost;
            if (owner.UsesStamina && cost > owner.Stamina)
            {
                return new SequenceValidationResult(
                    SequenceValidationError.NotEnoughStamina,
                    $"Sequence costs {cost} stamina; {owner.DisplayName} has {owner.Stamina}.");
            }

            return SequenceValidationResult.Valid;
        }

        /// <summary>
        /// GDD: the player picks 3 of their 5 loadout actions. Each copy in the loadout can fill one slot,
        /// so two Swords allow Slash twice, one Sword allows it once.
        /// </summary>
        private static MoveDefinition FindUnavailableAction(Sequence sequence, IReadOnlyList<MoveDefinition> available)
        {
            if (available == null)
            {
                return null;
            }

            var remaining = new List<MoveDefinition>(available);
            for (int slot = 0; slot < sequence.Count; slot++)
            {
                var move = sequence[slot];
                if (Sequence.IsEmptySlot(move))
                {
                    continue;
                }

                if (!remaining.Remove(move))
                {
                    return move;
                }
            }

            return null;
        }
    }
}
