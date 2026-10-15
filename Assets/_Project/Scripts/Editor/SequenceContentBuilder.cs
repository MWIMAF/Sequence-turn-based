using System.Collections.Generic;
using System.IO;
using Game.Combat;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BodyPart = Game.Combat.BodyPart;

namespace Game.Editor
{
    /// <summary>
    /// Creates the starting content from the GDD: moves, equipment, the player asset and enemy prefabs.
    /// Runs once automatically (when Player_Default doesn't exist yet), then only from the Sequence menu.
    /// </summary>
    /// <remarks>
    /// Every number here is a placeholder starting point. Tune them on the assets/prefabs afterwards;
    /// "Create Missing Content" never overwrites anything that already exists.
    /// </remarks>
    [InitializeOnLoad]
    public static class SequenceContentBuilder
    {
        public const string PlayerPath = DataRoot + "/Players/Player_Default.asset";
        public const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";

        private const string DataRoot = "Assets/_Project/Data";
        private const string AutoRunKey = "Sequence.GddContentAutoRun";

        static SequenceContentBuilder()
        {
            EditorApplication.delayCall += AutoRunOnce;
        }

        /// <summary>
        /// The full GDD setup, by hand: moves to GDD values, equipment, Player_Default, enemy prefabs,
        /// GDD clash table, and a rebuilt battle scene. Safe to run again: existing equipment, player
        /// and enemy prefabs are kept; only the default moves and the clash table are reset.
        /// </summary>
        [MenuItem("Sequence/Set Up GDD Content", priority = 19)]
        public static void SetUpGddContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Sequence] Stop Play mode first, then run  Sequence ▸ Set Up GDD Content.");
                return;
            }

            Debug.Log("[Sequence] Setting up GDD content...");
            Build(overwriteMoves: true);
            ResetInteractionTable();
            BattleSceneBuilder.RebuildBattleScene();
            Debug.Log("[Sequence] GDD content ready: moves, equipment, Player_Default and enemy prefabs. " +
                      "Use  Sequence ▸ Play Battle  to test.");
        }

        [MenuItem("Sequence/Create Missing Content", priority = 20)]
        public static void CreateMissingContent()
        {
            Build(overwriteMoves: false);
            Debug.Log("[Sequence] Created any missing moves, equipment, player and enemy prefabs.");
        }

        [MenuItem("Sequence/Reset Interaction Table to GDD", priority = 21)]
        public static void ResetInteractionTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<InteractionTable>(DataRoot + "/Balance/InteractionTable_Default.asset");
            if (table == null)
            {
                Debug.LogError("[Sequence] InteractionTable_Default.asset not found.");
                return;
            }

            table.SetRules(DefaultInteractionRules.Create());
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sequence] Interaction table reset to the GDD clash table.");
        }

        /// <summary>
        /// Runs the GDD setup automatically the first time (Player_Default doesn't exist yet).
        /// </summary>
        /// <remarks>
        /// If Unity is entering or in Play mode when this fires (e.g. Play was pressed while scripts were
        /// still compiling), it waits for Play mode to end instead of giving up: leaving Play mode doesn't
        /// reload scripts, so a skipped run would otherwise never happen.
        /// </remarks>
        private static void AutoRunOnce()
        {
            if (File.Exists(PlayerPath) || SessionState.GetBool(AutoRunKey, false)) return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= RunAfterPlayMode;
                EditorApplication.playModeStateChanged += RunAfterPlayMode;
                Debug.Log("[Sequence] GDD content will be set up when you leave Play mode.");
                return;
            }

            SessionState.SetBool(AutoRunKey, true);
            try
            {
                SetUpGddContent();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Sequence] Automatic GDD setup failed. Run  Sequence ▸ Set Up GDD Content  " +
                               "to try again.\n" + e);
            }
        }

        private static void RunAfterPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= RunAfterPlayMode;
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void Build(bool overwriteMoves)
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder(DataRoot, "Moves");
            EnsureFolder(DataRoot, "Equipment");
            EnsureFolder(DataRoot, "Players");
            EnsureFolder(DataRoot, "Balance");
            EnsureFolder("Assets/_Project", "Prefabs");
            EnsureFolder("Assets/_Project/Prefabs", "Enemies");

            // Moves (GDD action list). Name, type, stamina cost, skill power, stamina gain, heal.
            var idle = Move("Idle", MoveType.Idle, 0, 0, 0, 0, overwriteMoves);
            var slash = Move("Slash", MoveType.Attack, 2, 3, 0, 0, overwriteMoves);
            var bash = Move("Bash", MoveType.Attack, 1, 2, 0, 0, overwriteMoves);
            var kick = Move("Kick", MoveType.Attack, 1, 2, 0, 0, overwriteMoves);
            var parry = Move("Parry", MoveType.Parry, 2, 0, 0, 0, overwriteMoves);
            var evade = Move("Evade", MoveType.Dodge, 0, 0, 1, 0, overwriteMoves);
            var rest = Move("Rest", MoveType.Utility, 0, 0, 3, 2, overwriteMoves);

            // Equipment. The GDD table doesn't list a Parry source yet, so both hand items offer it.
            var sword = Equipment("Sword", EquipSlot.Hand, slash, parry);
            var shield = Equipment("Shield", EquipSlot.Hand, bash, parry);
            var armor = Equipment("Armor", EquipSlot.Body, rest);
            var boots = Equipment("Boots", EquipSlot.Leg, kick, evade);

            Player(new[]
            {
                new LoadoutSlot(BodyPart.RightHand, sword, slash),
                new LoadoutSlot(BodyPart.LeftHand, shield, parry),
                new LoadoutSlot(BodyPart.Body, armor, rest),
                new LoadoutSlot(BodyPart.RightLeg, boots, kick),
                new LoadoutSlot(BodyPart.LeftLeg, boots, evade),
            });

            Enemy("Enemy_Grunt", "Grunt", EnemyTier.Normal,
                CombatStats.Create(maxHp: 18, attack: 3, defense: 2),
                new PatternTurn("Wind-up", slash, slash, rest),
                new PatternTurn("Feint", kick, parry, slash),
                new PatternTurn("Breather", evade, rest, kick));

            Enemy("Enemy_Duelist", "Duelist", EnemyTier.Alpha,
                CombatStats.Create(maxHp: 26, attack: 4, defense: 4, critChancePercent: 10),
                new PatternTurn("Guarded opener", parry, slash, slash),
                new PatternTurn("Bait", evade, parry, kick),
                new PatternTurn("Pressure", slash, bash, slash),
                new PatternTurn("Reset", rest, parry, evade));

            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(DataRoot + "/Balance/Balance_Default.asset");
            if (balance == null)
            {
                balance = BalanceConfig.CreateRuntime(idle);
                AssetDatabase.CreateAsset(balance, DataRoot + "/Balance/Balance_Default.asset");
            }
            else if (balance.IdleMove == null || overwriteMoves)
            {
                balance.SetIdleMove(idle);
                EditorUtility.SetDirty(balance);
            }

            if (AssetDatabase.LoadAssetAtPath<InteractionTable>(DataRoot + "/Balance/InteractionTable_Default.asset") == null)
            {
                AssetDatabase.CreateAsset(InteractionTable.CreateRuntime(DefaultInteractionRules.Create()),
                    DataRoot + "/Balance/InteractionTable_Default.asset");
            }

            AssetDatabase.SaveAssets();
        }

        private static MoveDefinition Move(string name, MoveType type, int cost, int skillPower, int gain, int heal,
            bool overwrite)
        {
            string path = $"{DataRoot}/Moves/Move_{name}.asset";
            var move = AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);
            if (move != null && !overwrite)
            {
                return move;
            }

            bool isNew = move == null;
            if (isNew)
            {
                move = ScriptableObject.CreateInstance<MoveDefinition>();
            }

            move.Configure(name, name, type, DamageKind.Physical, cost, skillPower, gain, heal);
            if (isNew) AssetDatabase.CreateAsset(move, path);
            else EditorUtility.SetDirty(move);
            return move;
        }

        private static EquipmentDefinition Equipment(string name, EquipSlot slot, params MoveDefinition[] actions)
        {
            string path = $"{DataRoot}/Equipment/Equip_{name}.asset";
            var equipment = AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(path);
            if (equipment != null) return equipment;

            equipment = ScriptableObject.CreateInstance<EquipmentDefinition>();
            equipment.Configure(name, slot, actions);
            AssetDatabase.CreateAsset(equipment, path);
            return equipment;
        }

        private static void Player(IEnumerable<LoadoutSlot> loadout)
        {
            if (AssetDatabase.LoadAssetAtPath<PlayerDefinition>(PlayerPath) != null) return;

            var player = ScriptableObject.CreateInstance<PlayerDefinition>();
            player.Configure("You",
                CombatStats.Create(maxHp: 24, attack: 3, defense: 4, critChancePercent: 10, critMultiplierPercent: 150,
                    maxStamina: 8, startingStamina: 4),
                loadout);
            AssetDatabase.CreateAsset(player, PlayerPath);
        }

        private static void Enemy(string fileName, string displayName, EnemyTier tier, CombatStats stats,
            params PatternTurn[] pattern)
        {
            string path = $"{EnemyFolder}/{fileName}.prefab";
            if (File.Exists(path)) return;

            // Build in a preview scene so the open scene isn't touched.
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject(fileName);
                SceneManager.MoveGameObjectToScene(go, preview);
                go.AddComponent<EnemyUnit>().Configure(displayName, tier, stats, new EnemyPattern(pattern));
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
