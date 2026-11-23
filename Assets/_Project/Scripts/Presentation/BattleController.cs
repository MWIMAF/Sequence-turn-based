using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.AI;
using Game.Combat;
using Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Runs a test duel: the player fills slots from their loadout, locks in, and the turn's
    /// CombatEvents play back on screen.
    /// </summary>
    /// <remarks>
    /// All rules live in Game.Combat. This class only drives TurnController and shows what it returns:
    /// during playback, HP/stamina/statuses come from the events, never from re-reading combat state.
    /// </remarks>
    public sealed class BattleController : MonoBehaviour
    {
        private enum Mode
        {
            Setup,
            Planning,
            Playback,
            Over,
        }

        private const int MaxLogLines = 40;
        private const float FastForwardMultiplier = 4f;
        private const int EmptySlot = -1;

        [Header("Data")]
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private InteractionTable _interactionTable;
        [SerializeField] private PlayerDefinition _player;

        [Tooltip("Enemy prefabs to fight. E (or the header button) switches between them.")]
        [SerializeField] private List<EnemyUnit> _enemies = new List<EnemyUnit>();

        [Header("Test settings")]
        [Tooltip("0 = a new random seed every fight (affects crits only; enemy patterns are fixed).")]
        [SerializeField] private int _seed;

        [Tooltip("How much of the enemy's plan the player sees while planning. Also switchable in play (T).")]
        [SerializeField] private TelegraphMode _telegraph = TelegraphMode.Hidden;

        [Header("Playback pacing (seconds, presentation only)")]
        [SerializeField, Min(0.1f)] private float _clashSeconds = 1.3f;
        [SerializeField, Min(0f)] private float _beatSeconds = 0.45f;

        private readonly List<string> _log = new List<string>();
        private readonly List<StatusType> _playerStatuses = new List<StatusType>();
        private readonly List<StatusType> _enemyStatuses = new List<StatusType>();

        private BattleScreen _ui;
        private List<LoadoutAction> _actions;
        private EnemyUnit _enemy;
        private int _enemyIndex;
        private CombatState _state;
        private TurnController _turns;
        private int[] _planned;
        private Mode _mode = Mode.Setup;
        private int _fightSeed;
        private float _speed = 1f;
        private bool _fastForward;

        /// <summary>Assigns the data. Used by the editor scene builder.</summary>
        public void Configure(BalanceConfig balance, InteractionTable interactionTable, PlayerDefinition player,
            IEnumerable<EnemyUnit> enemies)
        {
            _balance = balance;
            _interactionTable = interactionTable;
            _player = player;
            _enemies = new List<EnemyUnit>(enemies);
        }

        private void Start()
        {
            EnsureEventSystem();

            string problem = FindSetupProblem();
            if (problem != null)
            {
                Debug.LogError($"[BattleController] {problem}", this);
                BattleScreen.BuildError(transform, problem + "\n\nStop Play mode and use  Sequence ▸ Set Up GDD Content  to fix it.");
                enabled = false;
                return;
            }

            _actions = _player.GetActions();
            if (!_balance.AllowEmptySlots && _balance.PassiveStaminaRegenPerTurn == 0 &&
                _actions.Count(a => a.Move.StaminaCost == 0) < _balance.SlotCount)
            {
                Debug.LogWarning("[BattleController] Empty slots are off, there's no stamina regen, and fewer free actions " +
                                 "than slots: at 0 stamina the player can't fill a sequence and gets stuck.", this);
            }

            _ui = BattleScreen.Build(transform, _balance.SlotCount, _actions);
            WireButtons();
            StartNewFight();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _ui == null)
            {
                return;
            }

            _fastForward = _mode == Mode.Playback && keyboard.spaceKey.isPressed;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                StartNewFight();
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                NextEnemy();
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                CycleTelegraph();
            }

            if (_mode != Mode.Planning)
            {
                return;
            }

            for (int i = 0; i < _actions.Count && i < 9; i++)
            {
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    AddAction(i);
                }
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveLastAction();
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                LockIn();
            }
        }

        // ---------------------------------------------------------------- setup

        private string FindSetupProblem()
        {
            if (_balance == null) return "No BalanceConfig assigned.";
            if (_balance.IdleMove == null) return "BalanceConfig has no Idle Move.";
            if (_interactionTable == null) return "No InteractionTable assigned.";
            if (_player == null) return "No PlayerDefinition assigned.";
            if (_player.GetActions().Count == 0) return $"{_player.name} has no usable actions. Equip something.";
            if (_enemies == null || _enemies.Count == 0 || _enemies.Any(e => e == null)) return "No enemy prefabs assigned.";
            return null;
        }

        private static void EnsureEventSystem()
        {
            // Start runs after every scene object's OnEnable, so EventSystem.current is reliable here.
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void WireButtons()
        {
            for (int i = 0; i < _ui.MoveButtons.Count; i++)
            {
                int index = i;
                _ui.MoveButtons[i].Button.onClick.AddListener(() => AddAction(index));
            }

            for (int i = 0; i < _ui.PlayerSlots.Length; i++)
            {
                int slot = i;
                _ui.PlayerSlots[i].Button.onClick.AddListener(() => ClearSlot(slot));
            }

            _ui.LockInButton.onClick.AddListener(LockIn);
            _ui.ClearButton.onClick.AddListener(ClearAllSlots);
            _ui.IntelButton.onClick.AddListener(CycleTelegraph);
            _ui.SpeedButton.onClick.AddListener(ToggleSpeed);
            _ui.EnemyButton.onClick.AddListener(NextEnemy);
            _ui.RestartButton.onClick.AddListener(StartNewFight);
            _ui.OverlayButton.onClick.AddListener(StartNewFight);
            RefreshHeaderButtons();
        }

        private void NextEnemy()
        {
            _enemyIndex = (_enemyIndex + 1) % _enemies.Count;
            StartNewFight();
        }

        private void StartNewFight()
        {
            StopAllCoroutines();

            _enemy = _enemies[_enemyIndex];
            _fightSeed = _seed != 0 ? _seed : UnityEngine.Random.Range(1, int.MaxValue);
            _state = CombatState.Create(_player, _enemy.DisplayName, _enemy.Stats, _enemy.Tier, _fightSeed);
            _turns = new TurnController(_state, _balance, _interactionTable, new PatternEnemyPlanner(_enemy.Pattern, _balance));
            _planned = Enumerable.Repeat(EmptySlot, _balance.SlotCount).ToArray();

            _log.Clear();
            _ui.Overlay.SetActive(false);
            _ui.SeedText.text = $"seed {_fightSeed}";
            _ui.PlayerPanel.SetName(_state.Player.DisplayName);
            _ui.PlayerPanel.SetStatsLine(StatsLine(_player.Stats));
            _ui.EnemyPanel.SetName(_state.Enemy.DisplayName);
            _ui.EnemyPanel.SetStatsLine($"{_enemy.Tier.ToString().ToUpperInvariant()}  ·  {StatsLine(_enemy.Stats)}");
            SyncPanelsFromState(instant: true);
            RefreshHeaderButtons();

            Log($"<b>A duel against {_state.Enemy.DisplayName} ({_enemy.Tier}) begins.</b>");
            BeginPlanning();
        }

        private static string StatsLine(CombatStats stats)
        {
            string crit = stats.CritChancePercent > 0 ? $"  CRIT {stats.CritChancePercent}%" : string.Empty;
            return $"ATK {stats.Attack}  DEF {stats.Defense}{crit}";
        }

        // ---------------------------------------------------------------- planning

        private void BeginPlanning()
        {
            var startEvents = _turns.BeginTurn();
            foreach (var e in startEvents.OfType<StaminaChangedEvent>())
            {
                Log($"{Who(e.Who)} regain {e.Delta} stamina.");
            }

            SyncPanelsFromState(instant: false);
            for (int i = 0; i < _planned.Length; i++) _planned[i] = EmptySlot;
            _mode = Mode.Planning;

            _ui.PhaseText.text = $"Turn {_state.TurnNumber}  ·  Plan your sequence";
            _ui.Announcement.text = $"Turn {_state.TurnNumber}";
            _ui.AnnouncementSub.text = $"Pick {_planned.Length} of your {_actions.Count} actions, then lock in.";
            Log($"<color=#{UiPalette.ToHex(UiPalette.Highlight)}>— Turn {_state.TurnNumber} —</color>");

            SetPlanningControls(true);
            RefreshPlanning();
        }

        private void RefreshPlanning()
        {
            var player = _state.Player;
            int cost = PlannedCost();
            int remaining = player.Stamina - cost;
            int firstEmpty = Array.IndexOf(_planned, EmptySlot);

            for (int i = 0; i < _planned.Length; i++)
            {
                var card = _ui.PlayerSlots[i];
                card.SetHighlight(false);
                card.SetDimmed(false);
                card.SetClickable(_planned[i] != EmptySlot);

                if (_planned[i] == EmptySlot)
                {
                    card.ShowEmpty(i == firstEmpty ? "Pick an action" : "Empty = Idle");
                    card.SetHighlight(i == firstEmpty);
                }
                else
                {
                    card.ShowMove(_actions[_planned[i]].Move);
                }

                ShowCarriedStatusBadge(card, player, i);
            }

            var enemyPlan = _turns.EnemyPlan;
            for (int i = 0; i < _ui.EnemySlots.Length; i++)
            {
                var card = _ui.EnemySlots[i];
                card.SetHighlight(false);
                card.SetDimmed(false);
                card.SetClickable(false);

                var move = enemyPlan[i] ?? _balance.IdleMove;
                if (IsRevealed(i)) card.ShowMove(move);
                else card.ShowHidden();

                ShowCarriedStatusBadge(card, _state.Enemy, i);
            }

            for (int i = 0; i < _ui.MoveButtons.Count; i++)
            {
                bool used = Array.IndexOf(_planned, i) >= 0;
                var button = _ui.MoveButtons[i];
                button.SetUsed(used);
                button.SetAvailable(CanAdd(i));
            }

            var validation = _turns.Validate(PlannedSequence());
            _ui.LockInButton.interactable = validation.IsValid;
            _ui.BudgetText.text = $"Stamina {player.Stamina}  ·  cost {cost}  ·  left {remaining}";

            int empty = _planned.Count(p => p == EmptySlot);
            string status = !validation.IsValid
                ? validation.Message
                : empty > 0 ? $"{empty} empty slot(s) will Idle." : "Ready. Lock in!";
            _ui.HintText.text = status + $"\n\n1–{Math.Min(_actions.Count, 9)} add · Backspace undo\n" +
                                "Click slot to clear\nT intel · E enemy · R restart";

            var patternTurn = _enemy.Pattern.ForTurn(_state.TurnNumber);
            int patternIndex = _enemy.Pattern.IndexForTurn(_state.TurnNumber);
            string step = patternIndex >= 0 && _telegraph == TelegraphMode.Full
                ? $"\nPattern turn {patternIndex + 1}/{_enemy.Pattern.Turns.Count}" +
                  (string.IsNullOrEmpty(patternTurn?.Label) ? string.Empty : $" ({patternTurn.Label})")
                : string.Empty;
            _ui.EnemyIntelText.text =
                $"<b>{_state.Enemy.DisplayName}'s plan is locked.</b>\n" +
                $"Intel: {TelegraphName(_telegraph)}{step}\n" +
                "It repeats a fixed pattern.\nLearn it!";
        }

        /// <summary>A Stagger carried over from last turn affects the first slot(s). Show that before the player commits.</summary>
        private void ShowCarriedStatusBadge(SlotCard card, CombatantState who, int slot)
        {
            var stagger = who.ActiveStatuses.FirstOrDefault(s => s.Type == StatusType.Staggered);
            if (stagger != null && slot < stagger.SlotsRemaining)
            {
                card.SetBadge($"STAGGERED · move canceled, +{_balance.StaggerDamageBonusPercent}% dmg taken", UiPalette.Warning);
            }
            else
            {
                card.ClearBadge();
            }
        }

        private void AddAction(int actionIndex)
        {
            if (_mode != Mode.Planning || actionIndex < 0 || actionIndex >= _actions.Count || !CanAdd(actionIndex)) return;

            _planned[Array.IndexOf(_planned, EmptySlot)] = actionIndex;
            RefreshPlanning();
        }

        /// <summary>
        /// Whether this action can go in the next empty slot. The rules come from the validator;
        /// the only UI rule is that each button (one body part) is placed once.
        /// </summary>
        private bool CanAdd(int actionIndex)
        {
            int slot = Array.IndexOf(_planned, EmptySlot);
            if (slot < 0 || Array.IndexOf(_planned, actionIndex) >= 0) return false;

            var moves = PlannedMoves();
            moves[slot] = _actions[actionIndex].Move;
            var check = _turns.Validate(new Sequence(moves));
            // A half-built plan is fine here; it only has to be full at lock-in.
            return check.IsValid || check.Error == SequenceValidationError.EmptySlotNotAllowed;
        }

        private void ClearSlot(int slot)
        {
            if (_mode != Mode.Planning) return;
            _planned[slot] = EmptySlot;
            RefreshPlanning();
        }

        private void RemoveLastAction()
        {
            for (int i = _planned.Length - 1; i >= 0; i--)
            {
                if (_planned[i] != EmptySlot)
                {
                    ClearSlot(i);
                    return;
                }
            }
        }

        private void ClearAllSlots()
        {
            if (_mode != Mode.Planning) return;
            for (int i = 0; i < _planned.Length; i++) _planned[i] = EmptySlot;
            RefreshPlanning();
        }

        private int PlannedCost() => _planned.Where(p => p != EmptySlot).Sum(p => _actions[p].Move.StaminaCost);

        private MoveDefinition[] PlannedMoves() =>
            _planned.Select(p => p == EmptySlot ? null : _actions[p].Move).ToArray();

        private Sequence PlannedSequence() => new Sequence(PlannedMoves());

        private void LockIn()
        {
            if (_mode != Mode.Planning) return;

            var playerSequence = PlannedSequence();
            var enemySequence = _turns.EnemyPlan;
            var result = _turns.LockIn(playerSequence);
            if (!result.Accepted)
            {
                _ui.HintText.text = result.Validation.Message;
                return;
            }

            StartCoroutine(Playback(playerSequence, enemySequence, result.Events));
        }

        private void SetPlanningControls(bool on)
        {
            _ui.LockInButton.gameObject.SetActive(on);
            _ui.ClearButton.gameObject.SetActive(on);
            _ui.BudgetText.gameObject.SetActive(on);
            foreach (var button in _ui.MoveButtons)
            {
                button.SetAvailable(on);
                if (!on) button.SetUsed(false);
            }

            foreach (var card in _ui.PlayerSlots)
            {
                card.SetClickable(on);
            }

            if (!on)
            {
                _ui.HintText.text = "Hold Space to fast-forward.";
                _ui.EnemyIntelText.text = string.Empty;
            }
        }

        // ---------------------------------------------------------------- playback

        private IEnumerator Playback(Sequence player, Sequence enemy, IReadOnlyList<CombatEvent> events)
        {
            _mode = Mode.Playback;
            SetPlanningControls(false);
            _ui.PhaseText.text = $"Turn {_state.TurnNumber}  ·  Resolving";
            Log($"You: {player}\n{_state.Enemy.DisplayName}: {enemy}");

            for (int i = 0; i < _planned.Length; i++)
            {
                RevealForPlayback(_ui.PlayerSlots[i], player[i]);
                RevealForPlayback(_ui.EnemySlots[i], enemy[i]);
            }

            _ui.Announcement.text = "Sequences locked!";
            _ui.AnnouncementSub.text = string.Empty;
            yield return Wait(_beatSeconds * 1.5f);

            int currentSlot = -1;
            foreach (var e in events)
            {
                if (e.Slot != CombatEvent.NoSlot && e.Slot != currentSlot)
                {
                    SetSlotFocus(currentSlot, false);
                    currentSlot = e.Slot;
                    SetSlotFocus(currentSlot, true);
                }

                switch (e)
                {
                    case StaminaChangedEvent stamina:
                        ShowStaminaPaid(stamina);
                        break;

                    case ClashResolvedEvent clash:
                        ShowClash(clash);
                        yield return Wait(_clashSeconds);
                        break;

                    case StatusAppliedEvent applied:
                        AddStatus(applied.Who, applied.Status);
                        Float(Panel(applied.Who).HpPosition + new Vector2(0f, 42f),
                            applied.Status.ToString().ToUpperInvariant(), UiPalette.ForStatus(applied.Status), 30);
                        Log($"S{applied.Slot + 1}: {Who(applied.Who)} {(applied.Who == CombatantId.Player ? "are" : "is")} " +
                            $"<color=#{UiPalette.ToHex(UiPalette.ForStatus(applied.Status))}>{applied.Status}</color> " +
                            $"(next {applied.DurationSlots} move(s) canceled, +{_balance.StaggerDamageBonusPercent}% dmg taken).");
                        yield return Wait(_beatSeconds * 0.6f);
                        break;

                    case StatusExpiredEvent expired:
                        RemoveStatus(expired.Who, expired.Status);
                        break;

                    case CombatantDefeatedEvent defeated:
                        _ui.Announcement.text = defeated.Who == CombatantId.Player ? "You are defeated!" : $"{_state.Enemy.DisplayName} is defeated!";
                        Log($"<b>{Who(defeated.Who)} {(defeated.Who == CombatantId.Player ? "are" : "is")} defeated.</b>");
                        yield return Wait(_beatSeconds);
                        break;
                }
            }

            SetSlotFocus(currentSlot, false);

            foreach (var e in _turns.CompletePlayback().OfType<StatusExpiredEvent>())
            {
                RemoveStatus(e.Who, e.Status);
            }

            if (_state.IsOver)
            {
                ShowResult();
                yield break;
            }

            yield return Wait(_beatSeconds * 2f);
            BeginPlanning();
        }

        private void RevealForPlayback(SlotCard card, MoveDefinition move)
        {
            card.ClearBadge();
            card.SetClickable(false);
            card.SetHighlight(false);
            card.ShowMove(move ?? _balance.IdleMove);
            card.SetDimmed(true);
        }

        private void SetSlotFocus(int slot, bool focused)
        {
            if (slot < 0) return;
            foreach (var card in new[] { _ui.PlayerSlots[slot], _ui.EnemySlots[slot] })
            {
                card.SetHighlight(focused);
                card.SetDimmed(false);
            }
        }

        private void ShowStaminaPaid(StaminaChangedEvent e)
        {
            var panel = Panel(e.Who);
            panel.SetStamina(e.StaminaAfter, _state.Get(e.Who).MaxStamina);
            Float(panel.StaminaPosition, $"{e.Delta} STA", UiPalette.Stamina, 28);
            Log($"{Who(e.Who)} pay {-e.Delta} stamina.");
        }

        private void ShowClash(ClashResolvedEvent clash)
        {
            var you = clash.Player;
            var foe = clash.Enemy;
            int slot = clash.Slot;

            ShowOutcomeOnCard(_ui.PlayerSlots[slot], you);
            ShowOutcomeOnCard(_ui.EnemySlots[slot], foe);
            ApplyOutcomeToPanel(CombatantId.Player, you, clash.Enemy.LandedCrit);
            ApplyOutcomeToPanel(CombatantId.Enemy, foe, clash.Player.LandedCrit);

            _ui.Announcement.text = $"{you.Move.DisplayName}  vs  {foe.Move.DisplayName}";
            _ui.AnnouncementSub.text = $"You: {you.Label}     ·     {_state.Enemy.DisplayName}: {foe.Label}";

            Log($"S{slot + 1}: {you.Move.DisplayName} vs {foe.Move.DisplayName}  →  you {you.Label}{Damage(you)}, " +
                $"{_state.Enemy.DisplayName} {foe.Label}{Damage(foe)}");
        }

        private static void ShowOutcomeOnCard(SlotCard card, SlotOutcome outcome)
        {
            var parts = new List<string> { outcome.Label };
            if (outcome.DamageTaken > 0) parts.Add($"-{outcome.DamageTaken} HP");
            if (outcome.Healed > 0) parts.Add($"+{outcome.Healed} HP");
            if (outcome.StaminaGained > 0) parts.Add($"+{outcome.StaminaGained} STA");

            var color = outcome.DamageTaken > 0 ? UiPalette.Bad
                : outcome.DamageDealt > 0 || outcome.Healed > 0 ? UiPalette.Good
                : UiPalette.Text;
            card.SetDetail(string.Join("  ", parts), color);
        }

        private void ApplyOutcomeToPanel(CombatantId who, SlotOutcome outcome, bool hitByCrit)
        {
            var panel = Panel(who);
            var fighter = _state.Get(who);
            panel.SetHp(outcome.HpAfter, fighter.MaxHp);
            if (fighter.UsesStamina)
            {
                panel.SetStamina(outcome.StaminaAfter, fighter.MaxStamina);
            }

            if (outcome.DamageTaken > 0)
            {
                string text = $"-{outcome.DamageTaken}";
                if (hitByCrit) text = "CRIT " + text;
                if (outcome.StaggerBonusTaken > 0) text += $"  (Stagger +{outcome.StaggerBonusTaken})";
                Float(panel.HpPosition, text, hitByCrit ? UiPalette.Crit : UiPalette.Bad, hitByCrit ? 52 : 46);
            }

            if (outcome.Healed > 0)
            {
                Float(panel.HpPosition + new Vector2(0f, -36f), $"+{outcome.Healed} HP", UiPalette.Good, 34);
            }

            if (outcome.StaminaGained > 0)
            {
                Float(panel.StaminaPosition, $"+{outcome.StaminaGained} STA", UiPalette.Stamina, 28);
            }
        }

        private void ShowResult()
        {
            _mode = Mode.Over;
            string title;
            Color color;
            switch (_state.Outcome)
            {
                case CombatOutcome.PlayerWon:
                    title = "VICTORY";
                    color = UiPalette.Good;
                    break;
                case CombatOutcome.EnemyWon:
                    title = "DEFEAT";
                    color = UiPalette.Bad;
                    break;
                default:
                    title = "DRAW";
                    color = UiPalette.Warning;
                    break;
            }

            _ui.PhaseText.text = "Fight over";
            _ui.OverlayTitle.text = title;
            _ui.OverlayTitle.color = color;
            _ui.OverlaySub.text = $"{_state.TurnNumber} turn(s)  ·  You {_state.Player.Hp} HP  ·  " +
                                  $"{_state.Enemy.DisplayName} {_state.Enemy.Hp} HP\nseed {_fightSeed}  ·  E for next enemy";
            _ui.Overlay.SetActive(true);
            Log($"<b>{title}</b>");
        }

        // ---------------------------------------------------------------- statuses & panels

        private void SyncPanelsFromState(bool instant)
        {
            _ui.PlayerPanel.SetHp(_state.Player.Hp, _state.Player.MaxHp, instant);
            _ui.PlayerPanel.SetStamina(_state.Player.Stamina, _state.Player.MaxStamina, instant);
            _ui.EnemyPanel.SetHp(_state.Enemy.Hp, _state.Enemy.MaxHp, instant);

            ResetStatuses(_playerStatuses, _state.Player);
            ResetStatuses(_enemyStatuses, _state.Enemy);
            _ui.PlayerPanel.SetStatuses(_playerStatuses);
            _ui.EnemyPanel.SetStatuses(_enemyStatuses);
        }

        private static void ResetStatuses(List<StatusType> list, CombatantState who)
        {
            list.Clear();
            list.AddRange(who.ActiveStatuses.Select(s => s.Type));
        }

        private void AddStatus(CombatantId who, StatusType status)
        {
            var list = Statuses(who);
            if (!list.Contains(status)) list.Add(status);
            Panel(who).SetStatuses(list);
        }

        private void RemoveStatus(CombatantId who, StatusType status)
        {
            var list = Statuses(who);
            list.Remove(status);
            Panel(who).SetStatuses(list);
        }

        private List<StatusType> Statuses(CombatantId who) => who == CombatantId.Player ? _playerStatuses : _enemyStatuses;

        private CombatantPanel Panel(CombatantId who) => who == CombatantId.Player ? _ui.PlayerPanel : _ui.EnemyPanel;

        // ---------------------------------------------------------------- header toggles

        private void CycleTelegraph()
        {
            _telegraph = (TelegraphMode)(((int)_telegraph + 1) % Enum.GetValues(typeof(TelegraphMode)).Length);
            RefreshHeaderButtons();
            if (_mode == Mode.Planning)
            {
                RefreshPlanning();
            }
        }

        private void ToggleSpeed()
        {
            _speed = _speed > 1f ? 1f : 2f;
            RefreshHeaderButtons();
        }

        private void RefreshHeaderButtons()
        {
            _ui.IntelLabel.text = $"Enemy intel: {TelegraphName(_telegraph)}  (T)";
            _ui.SpeedLabel.text = $"Speed {_speed:0}x";
            var enemy = _enemies[_enemyIndex];
            _ui.EnemyLabel.text = _enemies.Count > 1
                ? $"Enemy: {enemy.DisplayName}  ({_enemyIndex + 1}/{_enemies.Count})  (E)"
                : $"Enemy: {enemy.DisplayName}";
        }

        private bool IsRevealed(int slot)
        {
            switch (_telegraph)
            {
                case TelegraphMode.Full: return true;
                case TelegraphMode.FirstSlot: return slot == 0;
                case TelegraphMode.OneRandomSlot:
                    // Fixed per turn, so toggling intel doesn't re-roll which slot is shown.
                    return slot == new System.Random(_fightSeed ^ (_state.TurnNumber * 7919)).Next(_planned.Length);
                default: return false;
            }
        }

        private static string TelegraphName(TelegraphMode mode)
        {
            switch (mode)
            {
                case TelegraphMode.FirstSlot: return "Slot 1";
                case TelegraphMode.OneRandomSlot: return "1 random slot";
                case TelegraphMode.Full: return "Full";
                default: return "Hidden";
            }
        }

        // ---------------------------------------------------------------- helpers

        private IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime * _speed * (_fastForward ? FastForwardMultiplier : 1f);
                yield return null;
            }
        }

        private void Float(Vector2 position, string text, Color color, int size) =>
            FloatingText.Spawn(_ui.FxLayer, position, text, color, size);

        private string Who(CombatantId id) => id == CombatantId.Player ? "You" : _state.Enemy.DisplayName;

        private static string Damage(SlotOutcome outcome) =>
            outcome.DamageTaken > 0 ? $" (-{outcome.DamageTaken})" : string.Empty;

        private void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLogLines)
            {
                _log.RemoveRange(0, _log.Count - MaxLogLines);
            }

            _ui.LogText.text = string.Join("\n", _log);
        }
    }
}
