using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A number, word or effect sprite that pops up, drifts upward and fades out (damage, stamina, statuses, VFX).
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        private const float Lifetime = 1.3f;
        private const float Rise = 70f;

        private Graphic _graphic;
        private Color _color;
        private Vector2 _start;
        private float _age;

        public static void Spawn(RectTransform layer, Vector2 position, string text, Color color, int fontSize = 40)
        {
            var label = UiFactory.Label("Float", layer, text, fontSize, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(label.rectTransform, position, new Vector2(420f, fontSize * 1.6f));
            UiFactory.AddShadow(label);

            Attach(label, color, position);
        }

        public static void SpawnSprite(RectTransform layer, Vector2 position, Sprite sprite, float size)
        {
            var image = UiFactory.Panel("FloatSprite", layer, Color.white, rounded: false);
            image.sprite = sprite;
            image.preserveAspect = true;
            UiFactory.Place(image.rectTransform, position, new Vector2(size, size));
            Attach(image, Color.white, position);
        }

        private static void Attach(Graphic graphic, Color color, Vector2 position)
        {
            var floating = graphic.gameObject.AddComponent<FloatingText>();
            floating._graphic = graphic;
            floating._color = color;
            floating._start = position;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Lifetime);
            float eased = 1f - (1f - t) * (1f - t);

            _graphic.rectTransform.anchoredPosition = _start + new Vector2(0f, Rise * eased);
            _graphic.rectTransform.localScale = Vector3.one * (t < 0.12f ? Mathf.Lerp(0.6f, 1.1f, t / 0.12f) : 1f);
            _graphic.color = new Color(_color.r, _color.g, _color.b, 1f - t * t);

            if (_age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
