using System.Collections.Generic;
using System.Text;
using Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Name, stats line, HP bar, optional stamina bar and status chips for one fighter.
    /// </summary>
    public sealed class CombatantPanel : MonoBehaviour
    {
        private Text _name;
        private Text _statsLine;
        private Text _statuses;
        private Image _portrait;
        private StatBar _hp;
        private StatBar _stamina;
        private Vector2 _hpPosition;
        private Vector2 _staminaPosition;
        private Vector2 _portraitPosition;

        /// <summary>Where to spawn damage numbers, in the panel's parent space.</summary>
        public Vector2 HpPosition => _hpPosition;

        /// <summary>Where to spawn stamina numbers, in the panel's parent space.</summary>
        public Vector2 StaminaPosition => _staminaPosition;

        /// <summary>Centre of the portrait (where hit and parry effects play), in the panel's parent space.</summary>
        public Vector2 PortraitPosition => _portraitPosition;

        public static CombatantPanel Create(Transform parent, string name, Vector2 position, Vector2 size, Color accent,
            bool showStamina)
        {
            var back = UiFactory.Panel(name, parent, UiPalette.Panel);
            var rt = UiFactory.Place(back.rectTransform, position, size);
            var panel = rt.gameObject.AddComponent<CombatantPanel>();

            var stripe = UiFactory.Panel("Accent", rt, accent);
            UiFactory.Place(stripe.rectTransform, new Vector2(-size.x / 2f + 6f, 0f), new Vector2(8f, size.y - 16f));

            float left = -size.x / 2f + 24f;
            panel._name = UiFactory.Label("Name", rt, string.Empty, 26, UiPalette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(panel._name.rectTransform, new Vector2(left + 110f, 14f), new Vector2(220f, 34f));

            panel._statsLine = UiFactory.Label("Stats", rt, string.Empty, 15, UiPalette.TextDim, TextAnchor.MiddleLeft);
            UiFactory.Place(panel._statsLine.rectTransform, new Vector2(left + 110f, -20f), new Vector2(220f, 24f));

            var hpLocal = new Vector2(left + 230f + 210f, showStamina ? 12f : 0f);
            panel._hp = StatBar.Create(rt, "HP", hpLocal, new Vector2(420f, 30f), UiPalette.Hp, UiPalette.HpGhost, "HP");

            var staminaLocal = new Vector2(left + 230f + 140f, -22f);
            panel._stamina = StatBar.Create(rt, "Stamina", staminaLocal, new Vector2(280f, 24f),
                UiPalette.Stamina, UiPalette.StaminaGhost, "STA");
            panel._stamina.gameObject.SetActive(showStamina);

            panel._statuses = UiFactory.Label("Statuses", rt, string.Empty, 18, UiPalette.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Place(panel._statuses.rectTransform, new Vector2(size.x / 2f - 115f, 0f), new Vector2(200f, size.y - 10f));

            // Sits in the free strip left of the panel; hidden until a portrait is set.
            var portraitLocal = new Vector2(-size.x / 2f - 100f, -10f);
            panel._portrait = UiFactory.Panel("Portrait", rt, Color.white, rounded: false);
            panel._portrait.preserveAspect = true;
            UiFactory.Place(panel._portrait.rectTransform, portraitLocal, new Vector2(160f, 160f));
            panel._portrait.gameObject.SetActive(false);

            panel._portraitPosition = position + portraitLocal;
            panel._hpPosition = position + hpLocal;
            panel._staminaPosition = position + staminaLocal;
            return panel;
        }

        public void SetName(string displayName) => _name.text = displayName;

        public void SetPortrait(Sprite portrait)
        {
            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);
        }

        public void SetStatsLine(string text) => _statsLine.text = text;

        public void SetHp(int hp, int maxHp, bool instant = false) => _hp.Set(hp, maxHp, instant);

        public void SetStamina(int stamina, int maxStamina, bool instant = false) => _stamina.Set(stamina, maxStamina, instant);

        public void SetStatuses(IEnumerable<StatusType> statuses)
        {
            var sb = new StringBuilder();
            foreach (var status in statuses)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append($"<color=#{UiPalette.ToHex(UiPalette.ForStatus(status))}>{status.ToString().ToUpperInvariant()}</color>");
            }

            _statuses.text = sb.ToString();
        }
    }
}
