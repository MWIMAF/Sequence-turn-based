using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// The player's stats and equipment loadout. The loadout's five actions are the moves
    /// the player can put into their sequence each turn.
    /// </summary>
    [CreateAssetMenu(fileName = "Player_New", menuName = "Sequence/Player Definition")]
    public sealed class PlayerDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "You";
        [SerializeField] private CombatStats _stats = new CombatStats();
        [SerializeField] private Sprite _portrait;

        [Tooltip("One entry per body part: Right Hand, Left Hand, Body, Right Leg, Left Leg.")]
        [SerializeField] private List<LoadoutSlot> _loadout = DefaultLoadout();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public CombatStats Stats => _stats;
        public Sprite Portrait => _portrait;
        public IReadOnlyList<LoadoutSlot> Loadout => _loadout;

        /// <summary>The usable actions, in body-part order. Body parts with nothing usable are skipped.</summary>
        public List<LoadoutAction> GetActions()
        {
            var actions = new List<LoadoutAction>();
            foreach (var slot in _loadout)
            {
                var move = slot?.Action;
                if (move != null)
                {
                    actions.Add(new LoadoutAction(slot.BodyPart, slot.Equipment, move));
                }
            }

            return actions;
        }

        public static PlayerDefinition CreateRuntime(string displayName, CombatStats stats, IEnumerable<LoadoutSlot> loadout)
        {
            var player = CreateInstance<PlayerDefinition>();
            player.name = displayName;
            player.Configure(displayName, stats, loadout);
            return player;
        }

        /// <summary>Overwrites every value. Used by CreateRuntime and the editor content builder.</summary>
        public void Configure(string displayName, CombatStats stats, IEnumerable<LoadoutSlot> loadout)
        {
            _displayName = displayName;
            _stats = stats;
            _loadout = new List<LoadoutSlot>(loadout);
        }

        private static List<LoadoutSlot> DefaultLoadout()
        {
            var slots = new List<LoadoutSlot>();
            foreach (var part in BodyParts.All)
            {
                slots.Add(new LoadoutSlot(part));
            }

            return slots;
        }

        private void OnValidate()
        {
            foreach (var slot in _loadout)
            {
                string problem = slot?.Problem;
                if (!string.IsNullOrEmpty(problem))
                {
                    Debug.LogWarning($"{name}: {problem}", this);
                }
            }
        }
    }
}
