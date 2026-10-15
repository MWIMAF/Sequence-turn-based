using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// One turn of an enemy pattern: the move in each slot. An empty slot resolves as Idle.
    /// </summary>
    [Serializable]
    public sealed class PatternTurn
    {
        [Tooltip("Designer note, e.g. \"Opener\" or \"Punish window\". Not shown to the player.")]
        [SerializeField] private string _label;

        [SerializeField] private MoveDefinition[] _slots = new MoveDefinition[3];

        /// <summary>Needed by Unity when a new entry is added in the Inspector.</summary>
        public PatternTurn()
        {
        }

        public PatternTurn(string label, params MoveDefinition[] slots)
        {
            _label = label;
            _slots = slots ?? new MoveDefinition[0];
        }

        public string Label => _label;
        public IReadOnlyList<MoveDefinition> Slots => _slots;
    }
}
