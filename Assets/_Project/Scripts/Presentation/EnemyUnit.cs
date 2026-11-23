using Game.Combat;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// The component on an enemy prefab: name, tier, stats and the looping move pattern.
    /// Edit the prefab to tune an enemy. Combat reads these values; nothing here runs logic.
    /// </summary>
    /// <remarks>
    /// Later this prefab is where the enemy's sprite, animator and VFX hooks go.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EnemyUnit : MonoBehaviour
    {
        [SerializeField] private string _displayName = "Enemy";

        [Tooltip("Picks C in the damage formula: Normal < Alpha < Boss.")]
        [SerializeField] private EnemyTier _tier = EnemyTier.Normal;

        [Tooltip("Stamina fields are ignored for enemies: the pattern decides what they do.")]
        [SerializeField] private CombatStats _stats = new CombatStats();

        [SerializeField] private Sprite _portrait;

        [Tooltip("Turn 1 plays the first entry, then the next, and loops. Empty slots are Idle.")]
        [SerializeField] private EnemyPattern _pattern = new EnemyPattern();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public EnemyTier Tier => _tier;
        public CombatStats Stats => _stats;
        public EnemyPattern Pattern => _pattern;
        public Sprite Portrait => _portrait;

        /// <summary>Overwrites every value. Used by the editor content builder.</summary>
        public void Configure(string displayName, EnemyTier tier, CombatStats stats, EnemyPattern pattern)
        {
            _displayName = displayName;
            _tier = tier;
            _stats = stats;
            _pattern = pattern;
        }
    }
}
