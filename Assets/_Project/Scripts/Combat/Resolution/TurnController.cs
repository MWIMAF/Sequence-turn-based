using System;
using System.Collections.Generic;

namespace Game.Combat
{
    /// <summary>
    /// Drives one fight through the turn loop:
    /// TurnStart → Planning → LockIn → Resolution → Playback → TurnEnd → (TurnStart | CombatOver).
    /// </summary>
    /// <remarks>
    /// Plain C#, so presentation owns timing: call BeginTurn, show planning UI, call LockIn with the
    /// player's sequence, play the returned events, then call CompletePlayback.
    /// </remarks>
    public sealed class TurnController
    {
        private readonly BalanceConfig _balance;
        private readonly SequenceResolver _resolver;
        private readonly IEnemyPlanner _enemyPlanner;

        public TurnController(CombatState state, BalanceConfig balance, InteractionTable table, IEnemyPlanner enemyPlanner)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _balance = balance ? balance : throw new ArgumentNullException(nameof(balance));
            _enemyPlanner = enemyPlanner ?? throw new ArgumentNullException(nameof(enemyPlanner));
            _resolver = new SequenceResolver(table, balance);
        }

        public event Action<TurnPhase> PhaseChanged;

        public CombatState State { get; }
        public TurnPhase Phase { get; private set; } = TurnPhase.NotStarted;

        /// <summary>
        /// The enemy's sequence for this turn. Fixed when Planning begins, before the player locks in.
        /// Presentation decides how much of it to telegraph.
        /// </summary>
        public Sequence EnemyPlan { get; private set; }

        public IReadOnlyList<CombatEvent> BeginTurn()
        {
            RequirePhase(TurnPhase.NotStarted, TurnPhase.TurnEnd);
            if (State.IsOver)
            {
                throw new InvalidOperationException("The fight is already over.");
            }

            var events = new List<CombatEvent>();

            SetPhase(TurnPhase.TurnStart);
            RegenerateStamina(State.Player, events);
            RegenerateStamina(State.Enemy, events);

            SetPhase(TurnPhase.Planning);
            EnemyPlan = _enemyPlanner.Plan(State, CombatantId.Enemy);
            var enemyCheck = SequenceValidator.Validate(EnemyPlan, State.Enemy, _balance);
            if (!enemyCheck.IsValid)
            {
                throw new InvalidOperationException($"Enemy planner returned an invalid sequence: {enemyCheck.Message}");
            }

            return events;
        }

        /// <summary>Checks a player sequence without committing it (for UI feedback while planning).</summary>
        public SequenceValidationResult Validate(Sequence playerSequence) =>
            SequenceValidator.Validate(playerSequence, State.Player, _balance);

        /// <summary>
        /// Validates the player's sequence. If valid, both sides pay stamina and the whole turn resolves.
        /// If not, nothing changes and the controller stays in Planning.
        /// </summary>
        public LockInResult LockIn(Sequence playerSequence)
        {
            RequirePhase(TurnPhase.Planning);

            var validation = Validate(playerSequence);
            if (!validation.IsValid)
            {
                return LockInResult.Rejected(validation);
            }

            SetPhase(TurnPhase.LockIn);
            var events = new List<CombatEvent>();
            PayForSequence(State.Player, playerSequence, events);
            PayForSequence(State.Enemy, EnemyPlan, events);

            SetPhase(TurnPhase.Resolution);
            events.AddRange(_resolver.Resolve(State, playerSequence, EnemyPlan));

            SetPhase(TurnPhase.Playback);
            return LockInResult.Resolved(events);
        }

        /// <summary>Call when presentation has finished playing the turn's events.</summary>
        public IReadOnlyList<CombatEvent> CompletePlayback()
        {
            RequirePhase(TurnPhase.Playback);

            SetPhase(TurnPhase.TurnEnd);
            var events = _resolver.EndTurn(State);
            EnemyPlan = null;

            if (State.IsOver)
            {
                SetPhase(TurnPhase.CombatOver);
            }
            else
            {
                State.AdvanceTurn();
            }

            return events;
        }

        private void RegenerateStamina(CombatantState who, List<CombatEvent> events)
        {
            int gained = who.GainStamina(_balance.PassiveStaminaRegenPerTurn);
            if (gained > 0)
            {
                events.Add(new StaminaChangedEvent(
                    State.TurnNumber, who.Id, gained, who.Stamina, StaminaChangeReason.TurnStartRegen));
            }
        }

        private void PayForSequence(CombatantState who, Sequence sequence, List<CombatEvent> events)
        {
            int cost = sequence.TotalStaminaCost;
            if (cost <= 0)
            {
                return;
            }

            who.SpendStamina(cost);
            events.Add(new StaminaChangedEvent(
                State.TurnNumber, who.Id, -cost, who.Stamina, StaminaChangeReason.SequenceCost));
        }

        private void SetPhase(TurnPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void RequirePhase(params TurnPhase[] allowed)
        {
            if (Array.IndexOf(allowed, Phase) < 0)
            {
                throw new InvalidOperationException($"Can't do that during {Phase}; expected {string.Join(" or ", allowed)}.");
            }
        }
    }
}
