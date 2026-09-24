using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Game.Combat.Tests
{
    /// <summary>
    /// A small, fully specified fight for tests. Numbers are set here explicitly so tests
    /// don't break when the real Balance_Default asset gets retuned.
    /// </summary>
    internal sealed class TestWorld : IDisposable
    {
        public const int SlashPower = 4;
        public const int SlashCost = 2;
        public const int GuardPower = 3;
        public const int GuardGain = 1;
        public const int ParryPower = 3;
        public const int ParryCost = 2;
        public const int ChargeGain = 3;
        public const int ExposedPercent = 50;

        public readonly MoveDefinition Idle = MoveDefinition.CreateRuntime("Idle", MoveType.Idle);
        public readonly MoveDefinition Slash = MoveDefinition.CreateRuntime("Slash", MoveType.Attack, SlashCost, SlashPower);
        public readonly MoveDefinition Guard = MoveDefinition.CreateRuntime("Guard", MoveType.Block, 0, GuardPower, GuardGain);
        public readonly MoveDefinition Parry = MoveDefinition.CreateRuntime("Parry", MoveType.Parry, ParryCost, ParryPower);
        public readonly MoveDefinition Charge = MoveDefinition.CreateRuntime("Charge", MoveType.Utility, 0, 0, ChargeGain);

        public TestWorld(
            int hp = 20,
            int startingStamina = 5,
            int maxStamina = 10,
            bool carryStatuses = true,
            bool allowEmptySlots = true,
            int passiveRegen = 0)
        {
            Balance = BalanceConfig.CreateRuntime(
                Idle,
                slotCount: 3,
                allowEmptySlots: allowEmptySlots,
                playerMaxHp: hp,
                playerStartingStamina: startingStamina,
                maxStamina: maxStamina,
                passiveStaminaRegenPerTurn: passiveRegen,
                exposedDamageBonusPercent: ExposedPercent,
                staggeredDurationSlots: 1,
                exposedDurationSlots: 1,
                statusesCarryAcrossTurns: carryStatuses);
            Table = InteractionTable.CreateRuntime(DefaultInteractionRules.Create());
            Resolver = new SequenceResolver(Table, Balance);
            State = new CombatState(
                new CombatantState(CombatantId.Player, "Player", hp, startingStamina, maxStamina),
                new CombatantState(CombatantId.Enemy, "Enemy", hp, startingStamina, maxStamina),
                seed: 12345);
        }

        public BalanceConfig Balance { get; }
        public InteractionTable Table { get; }
        public SequenceResolver Resolver { get; }
        public CombatState State { get; }
        public CombatantState Player => State.Player;
        public CombatantState Enemy => State.Enemy;

        public IReadOnlyList<MoveDefinition> AllMoves => new[] { Slash, Guard, Parry, Charge };

        public List<CombatEvent> Resolve(Sequence player, Sequence enemy) => Resolver.Resolve(State, player, enemy);

        public static Sequence Seq(params MoveDefinition[] moves) => Sequence.Of(moves);

        public static ClashResolvedEvent Clash(IEnumerable<CombatEvent> events, int slot) =>
            events.OfType<ClashResolvedEvent>().Single(c => c.Slot == slot);

        public void Dispose()
        {
            foreach (var move in new[] { Idle, Slash, Guard, Parry, Charge })
            {
                Object.DestroyImmediate(move);
            }

            Object.DestroyImmediate(Balance);
            Object.DestroyImmediate(Table);
        }
    }
}
