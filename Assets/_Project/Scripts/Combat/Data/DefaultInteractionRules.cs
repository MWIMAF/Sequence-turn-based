using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// The GDD clash table, as code, so the default InteractionTable asset and the tests share one source.
    /// Mirrors the table in CLAUDE.md. Contains rules only, no balance numbers.
    /// </summary>
    public static class DefaultInteractionRules
    {
        public static List<InteractionRule> Create()
        {
            var rules = new List<InteractionRule>();

            void Add(MoveType self, MoveType opponent, DamageMode damage, string label,
                StatusType selfStatus = StatusType.None, StatusType opponentStatus = StatusType.None)
            {
                rules.Add(new InteractionRule(self, opponent, damage, label, selfStatus, opponentStatus));
            }

            // Attack: trades with Attack, is dodged by Dodge, is parried (and Staggered) by Parry,
            // and lands fully on Utility and Idle.
            Add(MoveType.Attack, MoveType.Attack, DamageMode.Full, "Trade");
            Add(MoveType.Attack, MoveType.Dodge, DamageMode.None, "Dodged");
            Add(MoveType.Attack, MoveType.Parry, DamageMode.None, "Parried");
            Add(MoveType.Attack, MoveType.Utility, DamageMode.Full, "Hit");
            Add(MoveType.Attack, MoveType.Idle, DamageMode.Full, "Hit");

            // Dodge: only answers Attack. Against anything else it's wasted (but still gains its stamina).
            Add(MoveType.Dodge, MoveType.Attack, DamageMode.None, "Dodge!");
            Add(MoveType.Dodge, MoveType.Dodge, DamageMode.None, "Nothing");
            Add(MoveType.Dodge, MoveType.Parry, DamageMode.None, "Nothing");
            Add(MoveType.Dodge, MoveType.Utility, DamageMode.None, "Wasted");
            Add(MoveType.Dodge, MoveType.Idle, DamageMode.None, "Wasted");

            // Parry: punishes Attack by Staggering the attacker (no counter damage in the GDD).
            // Whiffing does nothing; the cost is the stamina and the slot.
            Add(MoveType.Parry, MoveType.Attack, DamageMode.None, "Parry!", opponentStatus: StatusType.Staggered);
            Add(MoveType.Parry, MoveType.Dodge, DamageMode.None, "Nothing");
            Add(MoveType.Parry, MoveType.Parry, DamageMode.None, "Nothing");
            Add(MoveType.Parry, MoveType.Utility, DamageMode.None, "Nothing");
            Add(MoveType.Parry, MoveType.Idle, DamageMode.None, "Nothing");

            // Utility (Rest): always applies, which punishes a defensive opponent; it can't defend itself.
            Add(MoveType.Utility, MoveType.Attack, DamageMode.None, "Hit while resting");
            Add(MoveType.Utility, MoveType.Dodge, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Parry, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Utility, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Idle, DamageMode.None, "Applied");

            // Idle does nothing. It exists so empty slots resolve like any other move.
            Add(MoveType.Idle, MoveType.Attack, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Dodge, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Parry, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Utility, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Idle, DamageMode.None, "Idle");

            return rules;
        }
    }
}
