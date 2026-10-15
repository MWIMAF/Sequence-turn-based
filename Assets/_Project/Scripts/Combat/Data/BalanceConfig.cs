using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Every global tunable number. Per-fighter numbers live in CombatStats (player asset, enemy prefabs)
    /// and per-move numbers in MoveDefinition. Scripts never hardcode balance values.
    /// </summary>
    [CreateAssetMenu(fileName = "Balance_New", menuName = "Sequence/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Sequence")]
        [SerializeField, Min(1)] private int _slotCount = 3;

        [Tooltip("If false, every slot must hold a real move.")]
        [SerializeField] private bool _allowEmptySlots = true;

        [Tooltip("The move an empty slot becomes. Must be MoveType.Idle.")]
        [SerializeField] private MoveDefinition _idleMove;

        [Header("Stamina (player only)")]
        [Tooltip("Stamina the player gains at the start of each turn. 0 = stamina only comes from moves like Evade/Rest.")]
        [SerializeField, Min(0)] private int _passiveStaminaRegenPerTurn;

        [Header("Stagger")]
        [Tooltip("Staggered: extra damage taken, as a percent of the hit. GDD: 30.")]
        [SerializeField, Min(0)] private int _staggerDamageBonusPercent = 30;

        [Tooltip("How many following slots Staggered lasts.")]
        [SerializeField, Min(1)] private int _staggeredDurationSlots = 1;

        [Tooltip("If true, a Stagger caused in the last slot carries into slot 1 of the next turn.")]
        [SerializeField] private bool _statusesCarryAcrossTurns = true;

        [Header("Damage formula: taken = raw × C / (Defense + C)")]
        [Tooltip("C against Normal enemies (GDD: x).")]
        [SerializeField, Min(1)] private int _defenseConstantNormal = 50;

        [Tooltip("C against Alpha enemies (GDD: y > x). Higher C = Defense matters less.")]
        [SerializeField, Min(1)] private int _defenseConstantAlpha = 75;

        [Tooltip("C against Bosses (GDD: z > y).")]
        [SerializeField, Min(1)] private int _defenseConstantBoss = 100;

        [Tooltip("Any hit that lands deals at least this much.")]
        [SerializeField, Min(0)] private int _minimumDamage = 1;

        public int SlotCount => _slotCount;
        public bool AllowEmptySlots => _allowEmptySlots;
        public MoveDefinition IdleMove => _idleMove;
        public int PassiveStaminaRegenPerTurn => _passiveStaminaRegenPerTurn;
        public int StaggerDamageBonusPercent => _staggerDamageBonusPercent;
        public bool StatusesCarryAcrossTurns => _statusesCarryAcrossTurns;
        public int MinimumDamage => _minimumDamage;

        public int DurationFor(StatusType status) => status == StatusType.Staggered ? _staggeredDurationSlots : 0;

        public int DefenseConstantFor(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Alpha: return _defenseConstantAlpha;
                case EnemyTier.Boss: return _defenseConstantBoss;
                default: return _defenseConstantNormal;
            }
        }

        /// <summary>
        /// Builds an in-memory config for tests and tooling. Any argument left null keeps the default.
        /// </summary>
        public static BalanceConfig CreateRuntime(
            MoveDefinition idleMove,
            int? slotCount = null,
            bool? allowEmptySlots = null,
            int? passiveStaminaRegenPerTurn = null,
            int? staggerDamageBonusPercent = null,
            int? staggeredDurationSlots = null,
            bool? statusesCarryAcrossTurns = null,
            int? defenseConstantNormal = null,
            int? defenseConstantAlpha = null,
            int? defenseConstantBoss = null,
            int? minimumDamage = null)
        {
            var config = CreateInstance<BalanceConfig>();
            config.name = "Balance_Runtime";
            config._idleMove = idleMove;
            config._slotCount = slotCount ?? config._slotCount;
            config._allowEmptySlots = allowEmptySlots ?? config._allowEmptySlots;
            config._passiveStaminaRegenPerTurn = passiveStaminaRegenPerTurn ?? config._passiveStaminaRegenPerTurn;
            config._staggerDamageBonusPercent = staggerDamageBonusPercent ?? config._staggerDamageBonusPercent;
            config._staggeredDurationSlots = staggeredDurationSlots ?? config._staggeredDurationSlots;
            config._statusesCarryAcrossTurns = statusesCarryAcrossTurns ?? config._statusesCarryAcrossTurns;
            config._defenseConstantNormal = defenseConstantNormal ?? config._defenseConstantNormal;
            config._defenseConstantAlpha = defenseConstantAlpha ?? config._defenseConstantAlpha;
            config._defenseConstantBoss = defenseConstantBoss ?? config._defenseConstantBoss;
            config._minimumDamage = minimumDamage ?? config._minimumDamage;
            return config;
        }

        /// <summary>Points the config at the Idle move. Used by the editor content builder.</summary>
        public void SetIdleMove(MoveDefinition idleMove) => _idleMove = idleMove;

        private void OnValidate()
        {
            if (_idleMove != null && _idleMove.Type != MoveType.Idle)
            {
                Debug.LogWarning($"{name}: Idle Move '{_idleMove.name}' should have MoveType.Idle.", this);
            }

            if (_defenseConstantAlpha < _defenseConstantNormal || _defenseConstantBoss < _defenseConstantAlpha)
            {
                Debug.LogWarning($"{name}: GDD expects C to grow Normal < Alpha < Boss.", this);
            }
        }
    }
}
