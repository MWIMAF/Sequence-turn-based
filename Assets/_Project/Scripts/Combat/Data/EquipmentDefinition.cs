using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// A piece of gear (Sword, Shield, Armor, Boots). It fits one kind of body part and offers
    /// a list of actions; the loadout picks one of them per body part.
    /// </summary>
    [CreateAssetMenu(fileName = "Equip_New", menuName = "Sequence/Equipment Definition")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private EquipSlot _slot;
        [SerializeField] private List<MoveDefinition> _actions = new List<MoveDefinition>();
        [SerializeField] private Sprite _icon;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public EquipSlot Slot => _slot;
        public IReadOnlyList<MoveDefinition> Actions => _actions;
        public Sprite Icon => _icon;

        public bool Offers(MoveDefinition move) => move != null && _actions.Contains(move);

        public static EquipmentDefinition CreateRuntime(string displayName, EquipSlot slot, params MoveDefinition[] actions)
        {
            var equipment = CreateInstance<EquipmentDefinition>();
            equipment.name = displayName;
            equipment.Configure(displayName, slot, actions);
            return equipment;
        }

        /// <summary>Overwrites every value. Used by CreateRuntime and the editor content builder.</summary>
        public void Configure(string displayName, EquipSlot slot, IEnumerable<MoveDefinition> actions)
        {
            _displayName = displayName;
            _slot = slot;
            _actions = new List<MoveDefinition>(actions);
        }
    }
}
