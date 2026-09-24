using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Stats and move pool for one enemy archetype.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "Sequence/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, Min(1)] private int _maxHp = 20;
        [SerializeField, Min(0)] private int _startingStamina = 4;
        [SerializeField] private List<MoveDefinition> _movePool = new List<MoveDefinition>();
        [SerializeField] private EnemyPlannerKind _plannerKind = EnemyPlannerKind.Random;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public int MaxHp => _maxHp;
        public int StartingStamina => _startingStamina;
        public IReadOnlyList<MoveDefinition> MovePool => _movePool;
        public EnemyPlannerKind PlannerKind => _plannerKind;

        public static EnemyDefinition CreateRuntime(
            string displayName, int maxHp, int startingStamina, IEnumerable<MoveDefinition> movePool,
            EnemyPlannerKind plannerKind = EnemyPlannerKind.Random)
        {
            var enemy = CreateInstance<EnemyDefinition>();
            enemy.name = displayName;
            enemy._displayName = displayName;
            enemy._maxHp = maxHp;
            enemy._startingStamina = startingStamina;
            enemy._movePool = new List<MoveDefinition>(movePool);
            enemy._plannerKind = plannerKind;
            return enemy;
        }
    }
}
