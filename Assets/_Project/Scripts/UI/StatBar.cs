using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A horizontal bar with a lagging "ghost" segment, so a hit is visible as a chunk that drains away.
    /// </summary>
    public sealed class StatBar : MonoBehaviour
    {
        private const float FillSpeed = 2.5f;
        private const float GhostDelay = 0.35f;
        private const float GhostSpeed = 0.9f;

        private RectTransform _fill;
        private RectTransform _ghost;
        private Text _label;
        private string _suffix;
        private float _target = 1f;
        private float _shown = 1f;
        private float _ghostShown = 1f;
        private float _ghostWait;

        public static StatBar Create(Transform parent, string name, Vector2 position, Vector2 size,
            Color fillColor, Color ghostColor, string suffix)
        {
            var back = UiFactory.Panel(name, parent, new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Place(back.rectTransform, position, size);

            var ghost = UiFactory.Panel("Ghost", back.transform, ghostColor);
            var fill = UiFactory.Panel("Fill", back.transform, fillColor);

            var label = UiFactory.Label("Value", back.transform, string.Empty, Mathf.RoundToInt(size.y * 0.62f),
                UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(label.rectTransform);
            UiFactory.AddShadow(label);

            var bar = back.gameObject.AddComponent<StatBar>();
            bar._fill = fill.rectTransform;
            bar._ghost = ghost.rectTransform;
            bar._label = label;
            bar._suffix = suffix;
            bar.Apply();
            return bar;
        }

        public void Set(int current, int max, bool instant = false)
        {
            float previous = _target;
            _target = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
            _label.text = string.IsNullOrEmpty(_suffix) ? $"{current} / {max}" : $"{current} / {max} {_suffix}";

            if (instant)
            {
                _shown = _target;
                _ghostShown = _target;
            }
            else if (_target < previous)
            {
                _ghostWait = GhostDelay;
            }

            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(_shown, _target) && Mathf.Approximately(_ghostShown, _target))
            {
                return;
            }

            _shown = Mathf.MoveTowards(_shown, _target, FillSpeed * Time.deltaTime);
            if (_ghostShown < _target)
            {
                // Gains: the ghost has nothing to show, keep it under the fill.
                _ghostShown = _shown;
            }
            else if (_ghostWait > 0f)
            {
                _ghostWait -= Time.deltaTime;
            }
            else
            {
                _ghostShown = Mathf.MoveTowards(_ghostShown, _target, GhostSpeed * Time.deltaTime);
            }

            Apply();
        }

        private void Apply()
        {
            SetWidth(_fill, _shown);
            SetWidth(_ghost, Mathf.Max(_ghostShown, _shown));
        }

        private static void SetWidth(RectTransform rt, float fraction)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(fraction, 1f);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            rt.gameObject.SetActive(fraction > 0.001f);
        }
    }
}
