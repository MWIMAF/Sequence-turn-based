using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// A fighter's numbers. Shared by the player (PlayerDefinition asset) and enemies (EnemyUnit prefab),
    /// so both are tuned the same way in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class CombatStats
    {
        [SerializeField, Min(1)] private int _maxHp = 20;

        [Tooltip("Added to a move's Skill Power to get its base damage.")]
        [SerializeField, Min(0)] private int _attack = 2;

        [Tooltip("Damage taken = raw × C / (Defense + C). C comes from the enemy tier in BalanceConfig.")]
        [SerializeField, Min(0)] private int _defense = 2;

        [Tooltip("Chance for an attack to crit, 0-100. Rolled with the fight's seeded RNG.")]
        [SerializeField, Range(0, 100)] private int _critChancePercent;

        [Tooltip("Damage on a crit, as a percent of normal. 150 = ×1.5.")]
        [SerializeField, Min(100)] private int _critMultiplierPercent = 150;

        [Tooltip("Flat attack buff, as a percent. 20 = +20% damage.")]
        [SerializeField, Min(0)] private int _attackBuffPercent;

        [Header("Stamina (player only; enemies follow their pattern for free)")]
        [SerializeField, Min(0)] private int _maxStamina = 8;
        [SerializeField, Min(0)] private int _startingStamina = 4;

        public int MaxHp => _maxHp;
        public int Attack => _attack;
        public int Defense => _defense;
        public int CritChancePercent => _critChancePercent;
        public int CritMultiplierPercent => _critMultiplierPercent;
        public int AttackBuffPercent => _attackBuffPercent;
        public int MaxStamina => _maxStamina;
        public int StartingStamina => _startingStamina;

        public static CombatStats Create(
            int maxHp = 20, int attack = 2, int defense = 2, int critChancePercent = 0, int critMultiplierPercent = 150,
            int attackBuffPercent = 0, int maxStamina = 8, int startingStamina = 4)
        {
            return new CombatStats
            {
                _maxHp = Math.Max(1, maxHp),
                _attack = Math.Max(0, attack),
                _defense = Math.Max(0, defense),
                _critChancePercent = Math.Max(0, Math.Min(100, critChancePercent)),
                _critMultiplierPercent = Math.Max(100, critMultiplierPercent),
                _attackBuffPercent = Math.Max(0, attackBuffPercent),
                _maxStamina = Math.Max(0, maxStamina),
                _startingStamina = Math.Max(0, startingStamina),
            };
        }

        public override string ToString() => $"HP {_maxHp} ATK {_attack} DEF {_defense} CRIT {_critChancePercent}%";
    }
}
