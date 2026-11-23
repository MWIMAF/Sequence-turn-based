using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Combat;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Creates the test battle scene (camera + BattleController wired to the data assets).
    /// Runs once automatically if the scene doesn't exist yet, and is available from the Sequence menu.
    /// </summary>
    [InitializeOnLoad]
    public static class BattleSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Battle.unity";

        private const string DataRoot = "Assets/_Project/Data";
        private const string AutoBuildAttemptedKey = "Sequence.BattleSceneAutoBuildAttempted";

        static BattleSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        [MenuItem("Sequence/Play Battle", priority = 0)]
        public static void PlayBattle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!File.Exists(ScenePath) && !TryBuild(out string error))
            {
                Debug.LogError("[Sequence] " + error);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Sequence/Rebuild Battle Scene", priority = 1)]
        public static void RebuildBattleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Sequence] Stop Play mode before rebuilding the battle scene.");
                return;
            }

            var open = SceneManager.GetSceneByPath(ScenePath);
            bool wasOpen = open.IsValid() && open.isLoaded;
            if (wasOpen)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                // The scene file can't be rebuilt while it's the open scene.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (TryBuild(out string error))
            {
                Debug.Log($"[Sequence] Rebuilt {ScenePath}.");
            }
            else
            {
                Debug.LogError("[Sequence] " + error);
            }

            if (wasOpen && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static void AutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath))
            {
                return;
            }

            // Only try once per editor session, so a failure doesn't repeat on every recompile.
            if (SessionState.GetBool(AutoBuildAttemptedKey, false))
            {
                return;
            }

            SessionState.SetBool(AutoBuildAttemptedKey, true);
            if (TryBuild(out string error))
            {
                Debug.Log($"[Sequence] Created {ScenePath}. Use  Sequence ▸ Play Battle  to test it.");
            }
            else
            {
                Debug.LogWarning($"[Sequence] Couldn't create the battle scene automatically: {error} " +
                                 "Use  Sequence ▸ Rebuild Battle Scene  to try again.");
            }
        }

        private static bool TryBuild(out string error)
        {
            var balance = FindFirst<BalanceConfig>();
            var table = FindFirst<InteractionTable>();
            var player = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(SequenceContentBuilder.PlayerPath)
                         ?? FindFirst<PlayerDefinition>();
            var enemies = FindEnemyPrefabs();

            if (balance == null || table == null || player == null || enemies.Count == 0)
            {
                error = $"Missing data: needs a BalanceConfig, an InteractionTable, a PlayerDefinition and enemy prefabs " +
                        $"in {SequenceContentBuilder.EnemyFolder}. Try  Sequence ▸ Create Missing Content.";
                return false;
            }

            EnsureFolder("Assets/_Project", "Scenes");

            Scene scene;
            try
            {
                // Additive, so whatever scene the user has open is left untouched.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            catch (System.Exception e)
            {
                error = "Unity couldn't open a new scene alongside the current one (" + e.Message +
                        "). Save your open scene and try again.";
                return false;
            }

            try
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
                SceneManager.MoveGameObjectToScene(cameraGo, scene);

                var battleGo = new GameObject("Battle");
                var controller = battleGo.AddComponent<BattleController>();
                controller.Configure(balance, table, player, enemies);
                ArtLinker.LinkBattleController(controller);
                SceneManager.MoveGameObjectToScene(battleGo, scene);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    error = "Saving the scene failed.";
                    return false;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            AddToBuildSettings();
            AssetDatabase.Refresh();
            error = null;
            return true;
        }

        /// <summary>Every prefab in the enemy folder that has an EnemyUnit, sorted by name.</summary>
        public static List<EnemyUnit> FindEnemyPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(SequenceContentBuilder.EnemyFolder))
            {
                return new List<EnemyUnit>();
            }

            return AssetDatabase.FindAssets("t:Prefab", new[] { SequenceContentBuilder.EnemyFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(go => go != null)
                .Select(go => go.GetComponent<EnemyUnit>())
                .Where(unit => unit != null)
                .OrderBy(unit => unit.name)
                .ToList();
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static T FindFirst<T>() where T : Object => FindAll<T>().FirstOrDefault();

        private static List<T> FindAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();
        }
    }
}
