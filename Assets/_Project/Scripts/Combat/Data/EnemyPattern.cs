using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// A fixed, looping list of turns. Turn 1 plays the first entry, and after the last entry it starts over,
    /// so the player can learn the enemy.
    /// </summary>
    [Serializable]
    public sealed class EnemyPattern
    {
        [SerializeField] private List<PatternTurn> _turns = new List<PatternTurn>();

        public EnemyPattern()
        {
        }

        public EnemyPattern(IEnumerable<PatternTurn> turns)
        {
            _turns = new List<PatternTurn>(turns);
        }

        public IReadOnlyList<PatternTurn> Turns => _turns;

        /// <summary>0-based index into Turns for a 1-based turn number, looping. -1 if the pattern is empty.</summary>
        public int IndexForTurn(int turnNumber)
        {
            if (_turns.Count == 0) return -1;
            int index = (turnNumber - 1) % _turns.Count;
            return index < 0 ? index + _turns.Count : index;
        }

        public PatternTurn ForTurn(int turnNumber)
        {
            int index = IndexForTurn(turnNumber);
            return index < 0 ? null : _turns[index];
        }
    }
}
