using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Pure data for one move. No logic lives here; the SequenceResolver reads these values.
    /// </summary>
    [CreateAssetMenu(fileName = "Move_New", menuName = "Sequence/Move Definition")]
    public sealed class MoveDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private MoveType _moveType;

        [Tooltip("Stamina paid when the sequence is locked in (before any slot resolves).")]
        [SerializeField, Min(0)] private int _staminaCost;

        [Tooltip("Attack: damage dealt. Block: damage absorbed. Parry: counter damage. Unused by Utility/Idle.")]
        [SerializeField, Min(0)] private int _power;

        [Tooltip("Stamina gained when this move resolves (Guard gives a little, Charge gives a lot).")]
        [SerializeField, Min(0)] private int _staminaGain;

        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private Sprite _icon;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? Id : _displayName;
        public MoveType Type => _moveType;
        public int StaminaCost => _staminaCost;
        public int Power => _power;
        public int StaminaGain => _staminaGain;
        public Sprite Icon => _icon;

        public bool HasTag(string tag) => Array.IndexOf(_tags, tag) >= 0;

        /// <summary>
        /// Builds an in-memory move. For tests and editor tooling; gameplay should use authored assets.
        /// </summary>
        public static MoveDefinition CreateRuntime(
            string id, MoveType type, int staminaCost = 0, int power = 0, int staminaGain = 0, string displayName = null)
        {
            var move = CreateInstance<MoveDefinition>();
            move.name = id;
            move._id = id;
            move._displayName = displayName ?? id;
            move._moveType = type;
            move._staminaCost = Math.Max(0, staminaCost);
            move._power = Math.Max(0, power);
            move._staminaGain = Math.Max(0, staminaGain);
            return move;
        }

        public override string ToString() => $"{DisplayName} ({_moveType})";
    }
}
