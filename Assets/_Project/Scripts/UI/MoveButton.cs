using Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One loadout action in the player's hand (e.g. "R.Hand · Sword → Slash").
    /// Clicking it puts the action in the next empty slot; each can be used once per turn.
    /// </summary>
    public sealed class MoveButton : MonoBehaviour
    {
        private Button _button;
        private CanvasGroup _group;
        private Text _used;

        public MoveDefinition Move { get; private set; }
        public Button Button => _button;

        public static MoveButton Create(Transform parent, Vector2 position, Vector2 size, MoveDefinition move,
            string caption, string hotkey)
        {
            var button = UiFactory.Button("Move_" + move.DisplayName, parent, null, UiPalette.ForMove(move.Type), 0, out _);
            var rt = UiFactory.Place((RectTransform)button.transform, position, size);
            float inner = size.x - 20f;

            var source = UiFactory.Label("Source", rt, caption, 14, UiPalette.WithAlpha(UiPalette.Text, 0.8f),
                TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(source.rectTransform, new Vector2(-8f, size.y / 2f - 15f), new Vector2(inner - 20f, 20f));

            var key = UiFactory.Label("Hotkey", rt, hotkey, 16, UiPalette.WithAlpha(UiPalette.Text, 0.75f),
                TextAnchor.UpperRight, FontStyle.Bold);
            UiFactory.Place(key.rectTransform, new Vector2(0f, size.y / 2f - 15f), new Vector2(inner, 20f));

            var name = UiFactory.Label("Name", rt, move.DisplayName, 26, UiPalette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 4f), new Vector2(inner, 32f));
            UiFactory.AddShadow(name);

            if (move.Icon != null)
            {
                var icon = UiFactory.Panel("Icon", rt, Color.white, rounded: false);
                icon.sprite = move.Icon;
                icon.preserveAspect = true;
                UiFactory.Place(icon.rectTransform, new Vector2(size.x / 2f - 32f, 4f), new Vector2(44f, 44f));
            }

            var info = UiFactory.Label("Info", rt, MoveText.Summary(move), 14, UiPalette.Text, TextAnchor.MiddleLeft);
            UiFactory.Place(info.rectTransform, new Vector2(0f, -30f), new Vector2(inner, 34f));
            UiFactory.AddShadow(info);

            var used = UiFactory.Label("Used", rt, "IN SEQUENCE", 18, UiPalette.Highlight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(used.rectTransform);
            UiFactory.AddShadow(used);
            used.gameObject.SetActive(false);

            var view = rt.gameObject.AddComponent<MoveButton>();
            view._button = button;
            view._group = rt.gameObject.AddComponent<CanvasGroup>();
            view._used = used;
            view.Move = move;
            return view;
        }

        public void SetAvailable(bool available)
        {
            _button.interactable = available;
            _group.alpha = available ? 1f : 0.4f;
        }

        /// <summary>Marks the action as already placed in this turn's sequence.</summary>
        public void SetUsed(bool used) => _used.gameObject.SetActive(used);
    }
}
