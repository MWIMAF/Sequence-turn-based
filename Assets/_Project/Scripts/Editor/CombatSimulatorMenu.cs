using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.AI;
using Game.Combat;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Editor menu that runs the real combat simulation with no UI, so rules and numbers can be
    /// checked from the Console before any presentation exists. Both sides use RandomEnemyPlanner.
    /// </summary>
    public static class CombatSimulatorMenu
    {
        // Safety caps for the tool, not balance values: stops a stalemate looping forever.
        private const int MaxTurnsPerFight = 50;
        private const int BatchFightCount = 1000;

        [MenuItem("Sequence/Simulate One Random Duel (Console Log)")]
        public static void SimulateOneDuel()
        {
            if (!TryLoadAssets(out var balance, out var table, out var enemy, out var playerMoves)) return;

            int seed = Environment.TickCount;
            var log = new StringBuilder();
            log.AppendLine($"=== Random duel vs {enemy.DisplayName} (seed {seed}) ===");
            var result = RunFight(balance, table, enemy, playerMoves, seed, log);
            log.AppendLine($"=== {result.outcome} after {result.turns} turn(s) ===");
            Debug.Log(log.ToString());
        }

        [MenuItem("Sequence/Simulate 1000 Random Duels (Stats)")]
        public static void SimulateBatch()
        {
            if (!TryLoadAssets(out var balance, out var table, out var enemy, out var playerMoves)) return;

            var outcomes = new Dictionary<CombatOutcome, int>();
            int totalTurns = 0;
            for (int i = 0; i < BatchFightCount; i++)
            {
                var result = RunFight(balance, table, enemy, playerMoves, seed: i + 1, log: null);
                outcomes.TryGetValue(result.outcome, out int n);
                outcomes[result.outcome] = n + 1;
                totalTurns += result.turns;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== {BatchFightCount} random-vs-random duels vs {enemy.DisplayName} ===");
            foreach (var pair in outcomes.OrderBy(p => p.Key))
            {
                sb.AppendLine($"{pair.Key}: {pair.Value} ({pair.Value * 100f / BatchFightCount:0.0}%)");
            }

            sb.AppendLine($"Average fight length: {totalTurns / (float)BatchFightCount:0.0} turns");
            Debug.Log(sb.ToString());
        }

        private static (CombatOutcome outcome, int turns) RunFight(
            BalanceConfig balance, InteractionTable table, EnemyDefinition enemy,
            List<MoveDefinition> playerMoves, int seed, StringBuilder log)
        {
            var state = CombatState.Create(balance, enemy, seed);
            var controller = new TurnController(state, balance, table, EnemyPlannerFactory.Create(enemy, balance));
            var playerAutopilot = new RandomEnemyPlanner(playerMoves, balance);

            int turns = 0;
            while (!state.IsOver && turns < MaxTurnsPerFight)
            {
                turns++;
                var startEvents = controller.BeginTurn();
                var playerPlan = playerAutopilot.Plan(state, CombatantId.Player);

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

        private static bool TryLoadAssets(out BalanceConfig balance, out InteractionTable table,
            out EnemyDefinition enemy, out List<MoveDefinition> playerMoves)
        {
            balance = FindFirst<BalanceConfig>();
            table = FindFirst<InteractionTable>();
            enemy = FindFirst<EnemyDefinition>();
            playerMoves = FindAll<MoveDefinition>().Where(m => m.Type != MoveType.Idle).ToList();

            if (balance == null || table == null || enemy == null || playerMoves.Count == 0)
            {
                Debug.LogError("Combat simulator needs a BalanceConfig, an InteractionTable, an EnemyDefinition and some MoveDefinitions under Assets/_Project/Data.");
                return false;
            }

            return true;
        }

        private static T FindFirst<T>() where T : UnityEngine.Object => FindAll<T>().FirstOrDefault();

        private static List<T> FindAll<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/_Project/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();
        }
    }
}
