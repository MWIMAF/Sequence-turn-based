using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A number or word that pops up, drifts upward and fades out (damage, stamina, status names).
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.3f;
        private const float Rise = 70f;

        private Text _text;
        private Color _color;
        private Vector2 _start;
        private float _age;

        public static void Spawn(RectTransform layer, Vector2 position, string text, Color color, int fontSize = 40)
        {
            var label = UiFactory.Label("Float", layer, text, fontSize, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(label.rectTransform, position, new Vector2(420f, fontSize * 1.6f));
            UiFactory.AddShadow(label);

            var floating = label.gameObject.AddComponent<FloatingText>();
            floating._text = label;
            floating._color = color;
            floating._start = position;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Lifetime);
            float eased = 1f - (1f - t) * (1f - t);

            _text.rectTransform.anchoredPosition = _start + new Vector2(0f, Rise * eased);
            _text.rectTransform.localScale = Vector3.one * (t < 0.12f ? Mathf.Lerp(0.6f, 1.1f, t / 0.12f) : 1f);
            _text.color = new Color(_color.r, _color.g, _color.b, 1f - t * t);

            if (_age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
