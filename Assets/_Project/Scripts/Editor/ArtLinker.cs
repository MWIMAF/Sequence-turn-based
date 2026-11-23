using System.Collections.Generic;
using System.Linq;
using Game.Combat;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Art pipeline: PNGs under Assets/_Project/Art import as sprites, and each sprite is linked to the
    /// asset with the same name (Move_Slash.png → Move_Slash.asset's icon, Enemy_Grunt.png → the
    /// Enemy_Grunt prefab's portrait). The names come from the Figma generator (Tools/FigmaAssetGenerator).
    /// </summary>
    public sealed class ArtLinker : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Art";

        // BattleController field → sprite name.
        private static readonly Dictionary<string, string> BattleArt = new Dictionary<string, string>
        {
            { "_hitVfx", "Vfx_Hit" },
            { "_slashVfx", "Vfx_Slash" },
            { "_parryVfx", "Vfx_ParryFlash" },
            { "_staggeredIcon", "Status_Staggered" },
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot + "/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Any(p => p.StartsWith(ArtRoot + "/") && p.EndsWith(".png")))
            {
                EditorApplication.delayCall += Link; // after the import finishes, so the sprites can be loaded
            }
        }

        [MenuItem("Sequence/Link Art", priority = 30)]
        public static void Link()
        {
            var sprites = LoadSprites();
            int changed = 0;

            foreach (var type in new[] { "MoveDefinition", "EquipmentDefinition" })
            {
                foreach (var asset in FindAssets(type))
                {
                    changed += Assign(asset, "_icon", sprites, asset.name);
                }
            }

            foreach (var asset in FindAssets("PlayerDefinition"))
            {
                changed += Assign(asset, "_portrait", sprites, asset.name);
            }

            foreach (var enemy in BattleSceneBuilder.FindEnemyPrefabs())
            {
                changed += Assign(enemy, "_portrait", sprites, enemy.gameObject.name);
            }

            foreach (var controller in Object.FindObjectsByType<BattleController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int n = LinkBattleController(controller, sprites);
                if (n > 0 && !Application.isPlaying) EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                changed += n;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ArtLinker] {sprites.Count} sprites, {changed} link(s) updated." +
                      (changed > 0 ? " Save the Battle scene if it changed." : string.Empty));
        }

        /// <summary>Gives a battle its VFX and status sprites. Also used by the scene builder.</summary>
        public static int LinkBattleController(BattleController controller, Dictionary<string, Sprite> sprites = null)
        {
            sprites = sprites ?? LoadSprites();
            return BattleArt.Sum(pair => Assign(controller, pair.Key, sprites, pair.Value));
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var sprites = new Dictionary<string, Sprite>();
            if (!AssetDatabase.IsValidFolder(ArtRoot)) return sprites;

            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sprite != null && !sprites.ContainsKey(sprite.name)) sprites.Add(sprite.name, sprite);
            }

            return sprites;
        }

        private static IEnumerable<Object> FindAssets(string type) =>
            AssetDatabase.FindAssets($"t:{type}", new[] { "Assets/_Project/Data" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)));

        /// <summary>Sets a sprite field when a sprite with that name exists. Returns 1 if anything changed.</summary>
        private static int Assign(Object target, string field, Dictionary<string, Sprite> sprites, string spriteName)
        {
            if (target == null || !sprites.TryGetValue(spriteName, out var sprite)) return 0;

            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null || property.objectReferenceValue == sprite) return 0;

            property.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return 1;
        }
    }
}
