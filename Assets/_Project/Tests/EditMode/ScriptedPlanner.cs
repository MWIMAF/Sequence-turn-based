using System.Collections.Generic;

namespace Game.Combat.Tests
{
    /// <summary>Test double: returns pre-written sequences in order and counts calls.</summary>
    internal sealed class ScriptedPlanner : IEnemyPlanner
    {
        private readonly Queue<Sequence> _plans;

        public ScriptedPlanner(params Sequence[] plans)
        {
            _plans = new Queue<Sequence>(plans);
        }

        public int CallCount { get; private set; }

        public Sequence Plan(CombatState state, CombatantId self)
        {
            CallCount++;
            return _plans.Dequeue();
        }
    }
}
