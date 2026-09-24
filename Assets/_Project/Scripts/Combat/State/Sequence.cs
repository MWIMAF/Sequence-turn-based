using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Combat
{
    /// <summary>
    /// One side's plan for a turn: one move per slot. A null slot is empty and resolves as Idle.
    /// </summary>
    public sealed class Sequence
    {
        private readonly MoveDefinition[] _slots;

        public Sequence(IReadOnlyList<MoveDefinition> slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            _slots = new MoveDefinition[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                _slots[i] = slots[i];
            }
        }

        public static Sequence Of(params MoveDefinition[] moves) => new Sequence(moves);

        public int Count => _slots.Length;

        public MoveDefinition this[int slot] => _slots[slot];

        public static bool IsEmptySlot(MoveDefinition move) => move == null || move.Type == MoveType.Idle;

        public int TotalStaminaCost
        {
            get
            {
                int total = 0;
                foreach (var move in _slots)
                {
                    if (move != null)
                    {
                        total += move.StaminaCost;
                    }
                }

                return total;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append(_slots[i] != null ? _slots[i].DisplayName : "-");
            }

            return sb.Append(']').ToString();
        }
    }
}
