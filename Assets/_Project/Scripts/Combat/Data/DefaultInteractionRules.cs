using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// The designed clash table, as code, so the default InteractionTable asset and the tests
    /// share one source. Mirrors the table in CLAUDE.md. Contains rules only, no balance numbers.
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

            // Attack punishes Utility, Idle and whiffed parries. Block blunts it; Parry punishes it.
            Add(MoveType.Attack, MoveType.Attack, DamageMode.Full, "Trade");
            Add(MoveType.Attack, MoveType.Block, DamageMode.ReducedByOpponentPower, "Blocked");
            Add(MoveType.Attack, MoveType.Parry, DamageMode.None, "Parried");
            Add(MoveType.Attack, MoveType.Utility, DamageMode.Full, "Hit");
            Add(MoveType.Attack, MoveType.Idle, DamageMode.Full, "Hit");

            // Block is safe with low reward: it only answers Attack, but always banks a little stamina.
            Add(MoveType.Block, MoveType.Attack, DamageMode.None, "Guard");
            Add(MoveType.Block, MoveType.Block, DamageMode.None, "Standoff");
            Add(MoveType.Block, MoveType.Parry, DamageMode.None, "Guard");
            Add(MoveType.Block, MoveType.Utility, DamageMode.None, "Guard wasted");
            Add(MoveType.Block, MoveType.Idle, DamageMode.None, "Guard wasted");

            // Parry is high risk, high reward: counters and staggers an Attack, but any whiff leaves you Exposed.
            Add(MoveType.Parry, MoveType.Attack, DamageMode.Full, "Parry!", opponentStatus: StatusType.Staggered);
            Add(MoveType.Parry, MoveType.Block, DamageMode.None, "Whiff", selfStatus: StatusType.Exposed);
            Add(MoveType.Parry, MoveType.Parry, DamageMode.None, "Whiff", selfStatus: StatusType.Exposed);
            Add(MoveType.Parry, MoveType.Utility, DamageMode.None, "Whiff", selfStatus: StatusType.Exposed);
            Add(MoveType.Parry, MoveType.Idle, DamageMode.None, "Whiff", selfStatus: StatusType.Exposed);

            // Utility (Charge) always applies, which punishes a turtling opponent; it just can't defend itself.
            Add(MoveType.Utility, MoveType.Attack, DamageMode.None, "Hit while charging");
            Add(MoveType.Utility, MoveType.Block, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Parry, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Utility, DamageMode.None, "Applied");
            Add(MoveType.Utility, MoveType.Idle, DamageMode.None, "Applied");

            // Idle does nothing. It exists so empty and cancelled slots resolve like any other move.
            Add(MoveType.Idle, MoveType.Attack, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Block, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Parry, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Utility, DamageMode.None, "Idle");
            Add(MoveType.Idle, MoveType.Idle, DamageMode.None, "Idle");

            return rules;
        }
    }
}
