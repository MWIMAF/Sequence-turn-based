using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Game.Combat.Tests
{
    /// <summary>
    /// A small, fully specified fight for tests. Numbers are set here explicitly so tests don't break
    /// when the real assets get retuned. Defaults use Defense 0, so damage = Attack + SkillPower exactly.
    /// </summary>
    internal sealed class TestWorld : IDisposable
    {
        public const int Attack = 4;
        public const int SlashPower = 6;
        public const int SlashHit = Attack + SlashPower; // 10 with Defense 0
        public const int SlashCost = 2;
        public const int KickPower = 2;
        public const int KickCost = 1;
        public const int ParryCost = 2;
        public const int EvadeGain = 1;
        public const int RestGain = 3;
        public const int RestHeal = 2;
        public const int StaggerPercent = 30;
        public const int NormalC = 50;
        public const int BossC = 100;

        private readonly List<Object> _owned = new List<Object>();

        public readonly MoveDefinition Idle;
        public readonly MoveDefinition Slash;
        public readonly MoveDefinition Kick;
        public readonly MoveDefinition Bash;
        public readonly MoveDefinition Parry;
        public readonly MoveDefinition Evade;
        public readonly MoveDefinition Rest;

        public TestWorld(
            int hp = 30,
            int stamina = 5,
            int maxStamina = 10,
            int playerDefense = 0,
            int enemyDefense = 0,
            int playerCrit = 0,
            int enemyCrit = 0,
            int attack = Attack,
            EnemyTier tier = EnemyTier.Normal,
            bool carryStatuses = true,
            bool allowEmptySlots = true,
            int passiveRegen = 0,
            int seed = 12345,
            bool twoSwords = false)
        {
            Idle = Own(MoveDefinition.CreateRuntime("Idle", MoveType.Idle));
            Slash = Own(MoveDefinition.CreateRuntime("Slash", MoveType.Attack, SlashCost, SlashPower));
            Kick = Own(MoveDefinition.CreateRuntime("Kick", MoveType.Attack, KickCost, KickPower));
            Bash = Own(MoveDefinition.CreateRuntime("Bash", MoveType.Attack, 1, 1));
            Parry = Own(MoveDefinition.CreateRuntime("Parry", MoveType.Parry, ParryCost));
            Evade = Own(MoveDefinition.CreateRuntime("Evade", MoveType.Dodge, 0, 0, EvadeGain));
            Rest = Own(MoveDefinition.CreateRuntime("Rest", MoveType.Utility, 0, 0, RestGain, RestHeal));

            Balance = Own(BalanceConfig.CreateRuntime(
                Idle,
                slotCount: 3,
                allowEmptySlots: allowEmptySlots,
                passiveStaminaRegenPerTurn: passiveRegen,
                staggerDamageBonusPercent: StaggerPercent,
                staggeredDurationSlots: 1,
                statusesCarryAcrossTurns: carryStatuses,
                defenseConstantNormal: NormalC,
                defenseConstantAlpha: 75,
                defenseConstantBoss: BossC,
                minimumDamage: 1));
            Table = Own(InteractionTable.CreateRuntime(DefaultInteractionRules.Create()));
            Resolver = new SequenceResolver(Table, Balance);

            // The player's loadout: one action per body part. "Two swords" swaps the off-hand Parry for a second Slash.
            var actions = twoSwords
                ? new List<MoveDefinition> { Slash, Slash, Rest, Kick, Evade }
                : new List<MoveDefinition> { Slash, Parry, Rest, Kick, Evade };

            var playerStats = CombatStats.Create(hp, attack, playerDefense, playerCrit, 150, 0, maxStamina, stamina);
            var enemyStats = CombatStats.Create(hp, attack, enemyDefense, enemyCrit, 150, 0, maxStamina, stamina);
            State = new CombatState(
                new CombatantState(CombatantId.Player, "Player", playerStats, usesStamina: true, availableActions: actions),
                new CombatantState(CombatantId.Enemy, "Enemy", enemyStats, usesStamina: false),
                tier,
                seed);
        }

        public BalanceConfig Balance { get; }
        public InteractionTable Table { get; }
        public SequenceResolver Resolver { get; }
        public CombatState State { get; }
        public CombatantState Player => State.Player;
        public CombatantState Enemy => State.Enemy;

        public List<CombatEvent> Resolve(Sequence player, Sequence enemy) => Resolver.Resolve(State, player, enemy);

        public TurnController Controller(params Sequence[] enemyPlans) =>
            new TurnController(State, Balance, Table, new ScriptedPlanner(enemyPlans));

        public T Own<T>(T obj) where T : Object
        {
            _owned.Add(obj);
            return obj;
        }

        public static Sequence Seq(params MoveDefinition[] moves) => Sequence.Of(moves);

        public static ClashResolvedEvent Clash(IEnumerable<CombatEvent> events, int slot) =>
            events.OfType<ClashResolvedEvent>().Single(c => c.Slot == slot);

        public void Dispose()
        {
            foreach (var obj in _owned)
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
