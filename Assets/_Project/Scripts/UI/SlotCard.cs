using Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One slot of a sequence: shows the planned move, a hidden "?", or an empty slot,
    /// plus a result line after the clash and an optional warning badge.
    /// </summary>
    public sealed class SlotCard : MonoBehaviour
    {
        private Image _background;
        private Outline _outline;
        private CanvasGroup _group;
        private Button _button;
        private Text _header;
        private Text _title;
        private Image _icon;
        private Text _detail;
        private Text _badge;
        private Image _badgeBack;

        public Button Button => _button;
        public Vector2 Position => ((RectTransform)transform).anchoredPosition;

        public static SlotCard Create(Transform parent, string name, Vector2 position, Vector2 size, string header)
        {
            var background = UiFactory.Panel(name, parent, UiPalette.PanelLight);
            background.raycastTarget = true;
            var rt = UiFactory.Place(background.rectTransform, position, size);

            var card = rt.gameObject.AddComponent<SlotCard>();
            card._background = background;
            card._group = rt.gameObject.AddComponent<CanvasGroup>();

            card._outline = rt.gameObject.AddComponent<Outline>();
            card._outline.effectColor = UiPalette.Highlight;
            card._outline.effectDistance = new Vector2(4f, -4f);
            card._outline.enabled = false;

            card._button = rt.gameObject.AddComponent<Button>();
            card._button.targetGraphic = background;
            card._button.transition = Selectable.Transition.None;

            card._header = UiFactory.Label("Header", rt, header, 16, UiPalette.WithAlpha(UiPalette.Text, 0.7f),
                TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(card._header.rectTransform, new Vector2(0f, size.y / 2f - 18f), new Vector2(size.x - 20f, 24f));

            card._title = UiFactory.Label("Title", rt, string.Empty, 32, UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(card._title.rectTransform, new Vector2(0f, 8f), new Vector2(size.x - 16f, 44f));
            UiFactory.AddShadow(card._title);

            card._icon = UiFactory.Panel("Icon", rt, Color.white, rounded: false);
            card._icon.preserveAspect = true;
            UiFactory.Place(card._icon.rectTransform, new Vector2(-size.x / 2f + 42f, 8f), new Vector2(60f, 60f));
            card._icon.gameObject.SetActive(false);

            card._detail = UiFactory.Label("Detail", rt, string.Empty, 17, UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(card._detail.rectTransform, new Vector2(0f, -size.y / 2f + 28f), new Vector2(size.x - 12f, 44f));
            UiFactory.AddShadow(card._detail);

            card._badgeBack = UiFactory.Panel("Badge", rt, UiPalette.Warning);
            UiFactory.Place(card._badgeBack.rectTransform, new Vector2(0f, size.y / 2f + 6f), new Vector2(size.x - 10f, 28f));
            card._badge = UiFactory.Label("Text", card._badgeBack.transform, string.Empty, 15, Color.black,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(card._badge.rectTransform, 2f);
            card._badgeBack.gameObject.SetActive(false);

            return card;
        }

        public void ShowEmpty(string hint)
        {
            _background.color = UiPalette.WithAlpha(UiPalette.PanelLight, 0.6f);
            _title.text = "—";
            _title.color = UiPalette.TextDim;
            SetIcon(null);
            SetDetail(hint, UiPalette.TextDim);
        }

        public void ShowHidden()
        {
            _background.color = UiPalette.Panel;
            _title.text = "?";
            _title.color = UiPalette.TextDim;
            SetIcon(null);
            SetDetail("Hidden", UiPalette.TextDim);
        }

        public void ShowMove(MoveDefinition move)
        {
            _background.color = UiPalette.ForMove(move.Type);
            _title.text = move.DisplayName;
            _title.color = UiPalette.Text;
            SetIcon(move.Icon);
            SetDetail(move.Type == MoveType.Idle ? "Empty slot" : MoveText.Summary(move), UiPalette.Text);
        }

        private void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            _title.rectTransform.anchoredPosition = new Vector2(icon != null ? 26f : 0f, 8f); // make room for the icon
        }

        public void SetDetail(string text, Color color)
        {
            _detail.text = text;
            _detail.color = color;
        }

        public void SetBadge(string text, Color color)
        {
            _badgeBack.gameObject.SetActive(true);
            _badgeBack.color = color;
            _badge.text = text;
        }

        public void ClearBadge() => _badgeBack.gameObject.SetActive(false);

        public void SetHighlight(bool on)
        {
            _outline.enabled = on;
            transform.localScale = on ? Vector3.one * 1.06f : Vector3.one;
        }

        public void SetDimmed(bool dimmed) => _group.alpha = dimmed ? 0.45f : 1f;

        public void SetClickable(bool clickable)
        {
            _button.interactable = clickable;
            _background.raycastTarget = clickable;
        }
    }
}
