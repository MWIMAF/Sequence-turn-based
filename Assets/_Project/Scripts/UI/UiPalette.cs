using Game.Combat;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Placeholder colours for the test UI. Each move type has one colour so a player can read
    /// a sequence at a glance; swap these when real art arrives.
    /// </summary>
    public static class UiPalette
    {
        public static readonly Color Background = Hex(0x14161B);
        public static readonly Color Panel = Hex(0x1F232B);
        public static readonly Color PanelLight = Hex(0x2A2F39);
        public static readonly Color Text = Hex(0xE8EAF0);
        public static readonly Color TextDim = Hex(0x8C93A3);
        public static readonly Color Hp = Hex(0xE5484D);
        public static readonly Color HpGhost = Hex(0xF2C4C5);
        public static readonly Color Stamina = Hex(0x3E9BFF);
        public static readonly Color StaminaGhost = Hex(0xBFDCFF);
        public static readonly Color Good = Hex(0x46C37B);
        public static readonly Color Bad = Hex(0xFF6B6B);
        public static readonly Color Warning = Hex(0xF5A524);
        public static readonly Color Crit = Hex(0xFFD23F);
        public static readonly Color Highlight = Hex(0xFFE08A);
        public static readonly Color Accent = Hex(0x6E7BFF);

        public static Color ForMove(MoveType type)
        {
            switch (type)
            {
                case MoveType.Attack: return Hex(0xC2413B);
                case MoveType.Dodge: return Hex(0x2F6FC9);
                case MoveType.Parry: return Hex(0xC08A1E);
                case MoveType.Utility: return Hex(0x2E9E62);
                default: return Hex(0x4A505C);
            }
        }

        public static Color ForStatus(StatusType status)
        {
            switch (status)
            {
                case StatusType.Staggered: return Warning;
                default: return TextDim;
            }
        }

        /// <summary>RRGGBB for rich-text colour tags.</summary>
        public static string ToHex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
