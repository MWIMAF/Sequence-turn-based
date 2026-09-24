using System;
using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// One fighter's live numbers. Plain C#: presentation reads it, only Game.Combat changes it.
    /// </summary>
    public sealed class CombatantState
    {
        // Statuses caused during a slot wait here until that slot ends, so they only
        // ever affect the *next* slot, never the one that caused them.
        private readonly List<StatusInstance> _active = new List<StatusInstance>();
        private readonly List<StatusInstance> _pending = new List<StatusInstance>();

        public CombatantState(CombatantId id, string displayName, int maxHp, int startingStamina, int maxStamina)
        {
            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Hp = maxHp;
            MaxStamina = maxStamina;
            Stamina = Math.Min(Math.Max(0, startingStamina), maxStamina);
        }

        public CombatantId Id { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int MaxStamina { get; }
        public int Stamina { get; private set; }
        public bool IsDefeated => Hp <= 0;

        /// <summary>Statuses affecting the current or next slot to resolve.</summary>
        public IReadOnlyList<StatusInstance> ActiveStatuses => _active;

        /// <summary>Statuses caused this slot that switch on when it ends.</summary>
        public IReadOnlyList<StatusInstance> PendingStatuses => _pending;

        public bool HasActive(StatusType type) => Find(_active, type) != null;

        /// <summary>True if the status is active now or will be from the next slot.</summary>
        public bool HasActiveOrPending(StatusType type) => HasActive(type) || Find(_pending, type) != null;

        internal void ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Hp = Math.Max(0, Hp - amount);
        }

        internal void SpendStamina(int amount)
        {
            if (amount > Stamina)
            {
                throw new InvalidOperationException($"{DisplayName} cannot spend {amount} stamina with only {Stamina}.");
            }

            Stamina -= amount;
        }

        /// <summary>Adds stamina up to the cap and returns how much was actually gained.</summary>
        internal int GainStamina(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int before = Stamina;
            Stamina = Math.Min(MaxStamina, Stamina + amount);
            return Stamina - before;
        }

        internal void QueueStatus(StatusType type, int durationSlots)
        {
            if (type == StatusType.None || durationSlots <= 0)
            {
                return;
            }

            var existing = Find(_pending, type);
            if (existing != null)
            {
                existing.SlotsRemaining = Math.Max(existing.SlotsRemaining, durationSlots);
                return;
            }

            _pending.Add(new StatusInstance(type, durationSlots));
        }

        /// <summary>
        /// Called once a slot has resolved: active statuses count down, and ones reaching 0 are removed.
        /// </summary>
        internal List<StatusType> TickActiveStatuses()
        {
            var expired = new List<StatusType>();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].SlotsRemaining--;
                if (_active[i].SlotsRemaining <= 0)
                {
                    expired.Add(_active[i].Type);
                    _active.RemoveAt(i);
                }
            }

            expired.Reverse();
            return expired;
        }

        /// <summary>Switches on the statuses caused during the slot that just resolved.</summary>
        internal void PromotePendingStatuses()
        {
            foreach (var pending in _pending)
            {
                var existing = Find(_active, pending.Type);
                if (existing != null)
                {
                    existing.SlotsRemaining = Math.Max(existing.SlotsRemaining, pending.SlotsRemaining);
                }
                else
                {
                    _active.Add(pending);
                }
            }

            _pending.Clear();
        }

        /// <summary>Removes every status and returns the ones that were active.</summary>
        internal List<StatusType> ClearAllStatuses()
        {
            var cleared = new List<StatusType>();
            foreach (var status in _active)
            {
                cleared.Add(status.Type);
            }

            _active.Clear();
            _pending.Clear();
            return cleared;
        }

        private static StatusInstance Find(List<StatusInstance> list, StatusType type)
        {
            foreach (var status in list)
            {
                if (status.Type == type)
                {
                    return status;
                }
            }

            return null;
        }

        public override string ToString() => $"{DisplayName} HP {Hp}/{MaxHp} STA {Stamina}/{MaxStamina}";
    }
}
