using Game.Combat;

namespace Game.UI
{
    /// <summary>
    /// Player-facing text for moves. Reads numbers from the MoveDefinition, never hardcodes them.
    /// </summary>
    public static class MoveText
    {
        public static string TypeName(MoveType type)
        {
            switch (type)
            {
                case MoveType.Attack: return "ATTACK";
                case MoveType.Dodge: return "DODGE";
                case MoveType.Parry: return "PARRY";
                case MoveType.Utility: return "UTILITY";
                default: return "IDLE";
            }
        }

        public static string Cost(MoveDefinition move) =>
            move.StaminaCost > 0 ? $"Cost {move.StaminaCost}" : "Free";

        public static string Effect(MoveDefinition move)
        {
            switch (move.Type)
            {
                case MoveType.Attack:
                    string kind = move.DamageKind == DamageKind.Magical ? "Magic" : "Power";
                    return $"{kind} {move.SkillPower}";
                case MoveType.Dodge: return Join("Dodge attacks", Gain(move));
                case MoveType.Parry: return "Stagger attacker";
                case MoveType.Utility: return Join(Heal(move), Gain(move)) ?? "Utility";
                default: return "Does nothing";
            }
        }

        /// <summary>One line for a slot card: "Cost 2 · Power 3".</summary>
        public static string Summary(MoveDefinition move) => Join(Cost(move), Effect(move));

        private static string Gain(MoveDefinition move) =>
            move.StaminaGain > 0 ? $"+{move.StaminaGain} STA" : null;

        private static string Heal(MoveDefinition move) =>
            move.Heal > 0 ? $"+{move.Heal} HP" : null;

        private static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            return a + "  ·  " + b;
        }
    }
}
