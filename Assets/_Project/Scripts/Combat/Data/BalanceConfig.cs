using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Every tunable number that isn't per-move. Scripts read from here; they never hardcode balance values.
    /// </summary>
    [CreateAssetMenu(fileName = "Balance_New", menuName = "Sequence/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Sequence")]
        [SerializeField, Min(1)] private int _slotCount = 3;

        [Tooltip("If false, every slot must hold a real move (Idle counts as empty).")]
        [SerializeField] private bool _allowEmptySlots = true;

        [Tooltip("The move an empty or cancelled slot becomes. Must be MoveType.Idle.")]
        [SerializeField] private MoveDefinition _idleMove;

        [Header("Player")]
        [SerializeField, Min(1)] private int _playerMaxHp = 20;
        [SerializeField, Min(0)] private int _playerStartingStamina = 4;

        [Header("Stamina")]
        [SerializeField, Min(0)] private int _maxStamina = 10;

        [Tooltip("Stamina both sides gain at the start of each turn. 0 = stamina only comes from Guard/Charge.")]
        [SerializeField, Min(0)] private int _passiveStaminaRegenPerTurn = 0;

        [Header("Statuses")]
        [Tooltip("Exposed: extra damage taken, as a percent of the incoming hit (integer math, rounds down).")]
        [SerializeField, Min(0)] private int _exposedDamageBonusPercent = 50;

        [Tooltip("How many following slots a status lasts.")]
        [SerializeField, Min(1)] private int _staggeredDurationSlots = 1;
        [SerializeField, Min(1)] private int _exposedDurationSlots = 1;

        [Tooltip("If true, a status caused in the last slot carries into slot 1 of the next turn.")]
        [SerializeField] private bool _statusesCarryAcrossTurns = true;

        public int SlotCount => _slotCount;
        public bool AllowEmptySlots => _allowEmptySlots;
        public MoveDefinition IdleMove => _idleMove;
        public int PlayerMaxHp => _playerMaxHp;
        public int PlayerStartingStamina => _playerStartingStamina;
        public int MaxStamina => _maxStamina;
        public int PassiveStaminaRegenPerTurn => _passiveStaminaRegenPerTurn;
        public int ExposedDamageBonusPercent => _exposedDamageBonusPercent;
        public bool StatusesCarryAcrossTurns => _statusesCarryAcrossTurns;

        public int DurationFor(StatusType status)
        {
            switch (status)
            {
                case StatusType.Staggered: return _staggeredDurationSlots;
                case StatusType.Exposed: return _exposedDurationSlots;
                default: return 0;
            }
        }

        /// <summary>
        /// Builds an in-memory config for tests and tooling. Any argument left null keeps the asset default.
        /// </summary>
        public static BalanceConfig CreateRuntime(
            MoveDefinition idleMove,
            int? slotCount = null,
            bool? allowEmptySlots = null,
            int? playerMaxHp = null,
            int? playerStartingStamina = null,
            int? maxStamina = null,
            int? passiveStaminaRegenPerTurn = null,
            int? exposedDamageBonusPercent = null,
            int? staggeredDurationSlots = null,
            int? exposedDurationSlots = null,
            bool? statusesCarryAcrossTurns = null)
        {
            var config = CreateInstance<BalanceConfig>();
            config.name = "Balance_Runtime";
            config._idleMove = idleMove;
            config._slotCount = slotCount ?? config._slotCount;
            config._allowEmptySlots = allowEmptySlots ?? config._allowEmptySlots;
            config._playerMaxHp = playerMaxHp ?? config._playerMaxHp;
            config._playerStartingStamina = playerStartingStamina ?? config._playerStartingStamina;
            config._maxStamina = maxStamina ?? config._maxStamina;
            config._passiveStaminaRegenPerTurn = passiveStaminaRegenPerTurn ?? config._passiveStaminaRegenPerTurn;
            config._exposedDamageBonusPercent = exposedDamageBonusPercent ?? config._exposedDamageBonusPercent;
            config._staggeredDurationSlots = staggeredDurationSlots ?? config._staggeredDurationSlots;
            config._exposedDurationSlots = exposedDurationSlots ?? config._exposedDurationSlots;
            config._statusesCarryAcrossTurns = statusesCarryAcrossTurns ?? config._statusesCarryAcrossTurns;
            return config;
        }

        private void OnValidate()
        {
            if (_idleMove != null && _idleMove.Type != MoveType.Idle)
            {
                Debug.LogWarning($"{name}: Idle Move '{_idleMove.name}' should have MoveType.Idle.", this);
            }
        }
    }
}
