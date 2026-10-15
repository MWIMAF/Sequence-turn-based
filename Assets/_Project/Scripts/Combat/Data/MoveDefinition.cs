using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Combat
{
    /// <summary>
    /// Pure data for one action (Slash, Evade, Rest...). No logic lives here; the resolver reads these values.
    /// </summary>
    [CreateAssetMenu(fileName = "Move_New", menuName = "Sequence/Move Definition")]
    public sealed class MoveDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private MoveType _moveType;
        [SerializeField] private DamageKind _damageKind;

        [Tooltip("Stamina paid at lock-in (before any slot resolves). Only the player uses stamina.")]
        [SerializeField, Min(0)] private int _staminaCost;

        [Tooltip("Attack: added to the user's Attack stat before multipliers and Defense. Unused by other types.")]
        [FormerlySerializedAs("_power")]
        [SerializeField, Min(0)] private int _skillPower;

        [Tooltip("Stamina gained when this move resolves (e.g. Evade a little, Rest a lot).")]
        [SerializeField, Min(0)] private int _staminaGain;

        [Tooltip("HP restored when this move resolves (Utility). Capped at max HP.")]
        [SerializeField, Min(0)] private int _heal;

        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private Sprite _icon;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? Id : _displayName;
        public MoveType Type => _moveType;
        public DamageKind DamageKind => _damageKind;
        public int StaminaCost => _staminaCost;
        public int SkillPower => _skillPower;
        public int StaminaGain => _staminaGain;
        public int Heal => _heal;
        public Sprite Icon => _icon;

        public bool HasTag(string tag) => Array.IndexOf(_tags, tag) >= 0;

        /// <summary>
        /// Builds an in-memory move. For tests and editor tooling; gameplay should use authored assets.
        /// </summary>
        public static MoveDefinition CreateRuntime(
            string id, MoveType type, int staminaCost = 0, int skillPower = 0, int staminaGain = 0, int heal = 0,
            DamageKind damageKind = DamageKind.Physical, string displayName = null)
        {
            var move = CreateInstance<MoveDefinition>();
            move.name = id;
            move.Configure(id, displayName ?? id, type, damageKind, staminaCost, skillPower, staminaGain, heal);
            return move;
        }

        /// <summary>Overwrites every value. Used by CreateRuntime and the editor content builder.</summary>
        public void Configure(string id, string displayName, MoveType type, DamageKind damageKind,
            int staminaCost, int skillPower, int staminaGain, int heal)
        {
            _id = id;
            _displayName = displayName;
            _moveType = type;
            _damageKind = damageKind;
            _staminaCost = Math.Max(0, staminaCost);
            _skillPower = Math.Max(0, skillPower);
            _staminaGain = Math.Max(0, staminaGain);
            _heal = Math.Max(0, heal);
        }

        public override string ToString() => $"{DisplayName} ({_moveType})";
    }
}
