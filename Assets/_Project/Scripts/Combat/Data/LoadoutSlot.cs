using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// One body part in the player's loadout: the equipment worn there and which of its actions is used.
    /// </summary>
    [Serializable]
    public sealed class LoadoutSlot
    {
        [SerializeField] private BodyPart _bodyPart;
        [SerializeField] private EquipmentDefinition _equipment;

        [Tooltip("Must be one of the equipment's actions. Left empty, the equipment's first action is used.")]
        [SerializeField] private MoveDefinition _action;

        /// <summary>Needed by Unity when a new entry is added in the Inspector.</summary>
        public LoadoutSlot()
        {
        }

        public LoadoutSlot(BodyPart bodyPart, EquipmentDefinition equipment = null, MoveDefinition action = null)
        {
            _bodyPart = bodyPart;
            _equipment = equipment;
            _action = action;
        }

        public BodyPart BodyPart => _bodyPart;
        public EquipmentDefinition Equipment => _equipment;

        /// <summary>The action this body part gives, or null if nothing usable is equipped.</summary>
        public MoveDefinition Action
        {
            get
            {
                if (_equipment == null) return null;
                if (_action != null && _equipment.Offers(_action)) return _action;
                return _equipment.Actions.Count > 0 ? _equipment.Actions[0] : null;
            }
        }

        /// <summary>Empty string when valid; otherwise a short description of the problem.</summary>
        public string Problem
        {
            get
            {
                if (_equipment == null) return $"{_bodyPart}: nothing equipped.";
                if (_equipment.Slot != BodyParts.SlotFor(_bodyPart))
                    return $"{_bodyPart}: {_equipment.DisplayName} is {_equipment.Slot} gear.";
                if (_action != null && !_equipment.Offers(_action))
                    return $"{_bodyPart}: {_equipment.DisplayName} doesn't offer {_action.DisplayName}; using {Action?.DisplayName ?? "nothing"}.";
                return string.Empty;
            }
        }
    }
}
