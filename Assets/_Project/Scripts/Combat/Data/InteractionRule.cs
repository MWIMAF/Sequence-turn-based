using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// What ONE side's move does when it meets the opponent's move in the same slot.
    /// </summary>
    /// <remarks>
    /// Each ordered pair (Self, Opponent) describes only what Self's move causes. The resolver reads
    /// both directions of a clash and combines them. Keeping one fact in one row is what stops the
    /// table from contradicting itself (e.g. "Parry vs Block" vs "Block vs Parry").
    /// </remarks>
    [Serializable]
    public sealed class InteractionRule
    {
        [SerializeField] private MoveType _self;
        [SerializeField] private MoveType _opponent;
        [SerializeField] private DamageMode _damage;

        [Tooltip("Whether Self's move still applies its own effect (stamina gain) in this clash.")]
        [SerializeField] private bool _moveEffectApplies = true;

        [SerializeField] private StatusType _selfStatus;
        [SerializeField] private StatusType _opponentStatus;

        [Tooltip("Short, player-facing label for this side's result, e.g. Blocked, Parry!, Whiff.")]
        [SerializeField] private string _label;

        public InteractionRule()
        {
        }

        public InteractionRule(
            MoveType self,
            MoveType opponent,
            DamageMode damage,
            string label,
            StatusType selfStatus = StatusType.None,
            StatusType opponentStatus = StatusType.None,
            bool moveEffectApplies = true)
        {
            _self = self;
            _opponent = opponent;
            _damage = damage;
            _label = label;
            _selfStatus = selfStatus;
            _opponentStatus = opponentStatus;
            _moveEffectApplies = moveEffectApplies;
        }

        public MoveType Self => _self;
        public MoveType Opponent => _opponent;
        public DamageMode Damage => _damage;
        public bool MoveEffectApplies => _moveEffectApplies;
        public StatusType SelfStatus => _selfStatus;
        public StatusType OpponentStatus => _opponentStatus;
        public string Label => _label;

        public override string ToString() => $"{_self} vs {_opponent}: {_label}";
    }
}
