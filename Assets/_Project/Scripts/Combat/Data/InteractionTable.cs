using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Every clash rule in the game. The resolver never branches on move types; it asks this table.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionTable_New", menuName = "Sequence/Interaction Table")]
    public sealed class InteractionTable : ScriptableObject
    {
        [SerializeField] private List<InteractionRule> _rules = new List<InteractionRule>();

        private Dictionary<(MoveType, MoveType), InteractionRule> _lookup;

        public IReadOnlyList<InteractionRule> Rules => _rules;

        public InteractionRule Get(MoveType self, MoveType opponent)
        {
            if (TryGet(self, opponent, out var rule))
            {
                return rule;
            }

            throw new InvalidOperationException(
                $"InteractionTable '{name}' has no rule for {self} vs {opponent}. Every MoveType pair needs a row.");
        }

        public bool TryGet(MoveType self, MoveType opponent, out InteractionRule rule)
        {
            EnsureLookup();
            return _lookup.TryGetValue((self, opponent), out rule);
        }

        /// <summary>Every ordered MoveType pair that has no rule. Empty means the table is complete.</summary>
        public List<(MoveType self, MoveType opponent)> FindMissingPairs()
        {
            var missing = new List<(MoveType, MoveType)>();
            var types = (MoveType[])Enum.GetValues(typeof(MoveType));
            foreach (var self in types)
            {
                foreach (var opponent in types)
                {
                    if (!TryGet(self, opponent, out _))
                    {
                        missing.Add((self, opponent));
                    }
                }
            }

            return missing;
        }

        /// <summary>Replaces all rules. Used by tests and by editor tooling that seeds the default table.</summary>
        public void SetRules(IEnumerable<InteractionRule> rules)
        {
            _rules = new List<InteractionRule>(rules);
            _lookup = null;
        }

        public static InteractionTable CreateRuntime(IEnumerable<InteractionRule> rules)
        {
            var table = CreateInstance<InteractionTable>();
            table.name = "InteractionTable_Runtime";
            table.SetRules(rules);
            return table;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<(MoveType, MoveType), InteractionRule>();
            foreach (var rule in _rules)
            {
                if (rule == null)
                {
                    continue;
                }

                var key = (rule.Self, rule.Opponent);
                if (_lookup.ContainsKey(key))
                {
                    Debug.LogWarning($"InteractionTable '{name}' has a duplicate rule for {rule.Self} vs {rule.Opponent}; using the first.");
                    continue;
                }

                _lookup.Add(key, rule);
            }
        }

        private void OnValidate()
        {
            // Rules edited in the Inspector must rebuild the cached lookup.
            _lookup = null;
        }
    }
}
