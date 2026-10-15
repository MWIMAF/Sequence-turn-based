using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.AI;
using Game.Combat;
using Game.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Runs the real combat simulation with no UI. The player is a random autopilot (picks random affordable
    /// loadout actions) against each enemy prefab's pattern: a "how hard is this pattern if you don't read it"
    /// baseline. A pattern a random player beats often is probably too soft.
    /// </summary>
    public static class CombatSimulatorMenu
    {
        // Safety caps for the tool, not balance values: stops a stalemate looping forever.
        private const int MaxTurnsPerFight = 60;
        private const int BatchFightCount = 1000;

        [MenuItem("Sequence/Simulate One Random Duel (Console Log)", priority = 40)]
        public static void SimulateOneDuel()
        {
            if (!TryLoad(out var balance, out var table, out var player, out var enemies)) return;

            var enemy = enemies[0];
            int seed = Environment.TickCount;
            var log = new StringBuilder();
            log.AppendLine($"=== Random player vs {enemy.DisplayName} (seed {seed}) ===");
            var result = RunFight(balance, table, player, enemy, seed, log);
            log.AppendLine($"=== {result.outcome} after {result.turns} turn(s) ===");
            Debug.Log(log.ToString());
        }

        [MenuItem("Sequence/Simulate 1000 Random Duels per Enemy (Stats)", priority = 41)]
        public static void SimulateBatch()
        {
            if (!TryLoad(out var balance, out var table, out var player, out var enemies)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"=== {BatchFightCount} fights per enemy: random-autopilot player vs enemy pattern ===");
            foreach (var enemy in enemies)
            {
                var outcomes = new Dictionary<CombatOutcome, int>();
                int totalTurns = 0;
                for (int i = 0; i < BatchFightCount; i++)
                {
                    var result = RunFight(balance, table, player, enemy, seed: i + 1, log: null);
                    outcomes.TryGetValue(result.outcome, out int n);
                    outcomes[result.outcome] = n + 1;
                    totalTurns += result.turns;
                }

                string rates = string.Join("  ", outcomes.OrderBy(p => p.Key)
                    .Select(p => $"{p.Key} {p.Value * 100f / BatchFightCount:0.0}%"));
                sb.AppendLine($"{enemy.DisplayName} ({enemy.Tier}): {rates}  ·  avg {totalTurns / (float)BatchFightCount:0.0} turns");
            }

            Debug.Log(sb.ToString());
        }

        private static (CombatOutcome outcome, int turns) RunFight(
            BalanceConfig balance, InteractionTable table, PlayerDefinition player, EnemyUnit enemy, int seed, StringBuilder log)
        {
            var state = CombatState.Create(player, enemy.DisplayName, enemy.Stats, enemy.Tier, seed);
            var controller = new TurnController(state, balance, table, new PatternEnemyPlanner(enemy.Pattern, balance));
            var autopilot = new RandomEnemyPlanner(null, balance);

            int turns = 0;
            while (!state.IsOver && turns < MaxTurnsPerFight)
            {
                turns++;
                var startEvents = controller.BeginTurn();
                var playerPlan = autopilot.Plan(state, CombatantId.Player);

                if (log != null)
                {
                    log.AppendLine();
                    log.AppendLine($"-- Turn {state.TurnNumber}  |  {state.Player}  |  {state.Enemy}");
                    log.Append(CombatLogFormatter.Format(startEvents));
                    log.AppendLine($"     Player plans {playerPlan}   Enemy plans {controller.EnemyPlan}");
                }

                var result = controller.LockIn(playerPlan);
                if (!result.Accepted)
                {
                    throw new InvalidOperationException("Autopilot built an invalid plan: " + result.Validation.Message);
                }

                log?.Append(CombatLogFormatter.Format(result.Events));
                var endEvents = controller.CompletePlayback();
                log?.Append(CombatLogFormatter.Format(endEvents));
            }

            return (state.Outcome, turns);
        }

        private static bool TryLoad(out BalanceConfig balance, out InteractionTable table, out PlayerDefinition player,
            out List<EnemyUnit> enemies)
        {
            balance = FindFirst<BalanceConfig>();
            table = FindFirst<InteractionTable>();
            player = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(SequenceContentBuilder.PlayerPath) ?? FindFirst<PlayerDefinition>();
            enemies = BattleSceneBuilder.FindEnemyPrefabs();

            if (balance == null || table == null || player == null || enemies.Count == 0)
            {
                Debug.LogError("Combat simulator needs a BalanceConfig, an InteractionTable, a PlayerDefinition and " +
                               "enemy prefabs. Try  Sequence ▸ Create Missing Content.");
                return false;
            }

            return true;
        }

        private static T FindFirst<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Project/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null);
    }
}
