using UnityEngine;
using UnityEditor;
using MoreMountains.TopDownEngine;
using DungeonCrawl.Gameplay;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup: adds Health + CharacterController + GoblinMeleeAI to the goblin prefab
    /// (previously it only had GoblinIdleAnim, so it had no way to take damage or attack the player).
    /// Both existing scene instances pick this up automatically since it's applied to the source prefab asset.</summary>
    public static class GoblinCombatSetup
    {
        private const string GoblinPrefabPath = "Assets/Art/Goblin/SnarlingGoblinFighter.prefab";

        [MenuItem("Dungeon Crawl/Setup Goblin Combat AI")]
        public static void SetupGoblinCombat()
        {
            var root = PrefabUtility.LoadPrefabContents(GoblinPrefabPath);
            if (root == null)
            {
                Debug.LogError("Could not load goblin prefab at " + GoblinPrefabPath);
                return;
            }

            // reuse the same arm bone GoblinIdleAnim already sways, so the attack windup and idle sway share one bone
            Transform weaponArm = null;
            var idleAnim = root.GetComponent<GoblinIdleAnim>();
            if (idleAnim != null)
            {
                var idleSo = new SerializedObject(idleAnim);
                weaponArm = idleSo.FindProperty("weaponArm").objectReferenceValue as Transform;
            }

            var health = root.GetComponent<Health>();
            if (health == null)
            {
                health = root.AddComponent<Health>();
            }
            health.InitialHealth = 40;
            health.MaximumHealth = 40;

            var controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = root.AddComponent<CharacterController>();
                controller.center = new Vector3(0f, 0.65f, 0f);
                controller.radius = 0.3f;
                controller.height = 1.3f;
                controller.skinWidth = 0.03f;
            }

            var ai = root.GetComponent<GoblinMeleeAI>();
            if (ai == null)
            {
                ai = root.AddComponent<GoblinMeleeAI>();
            }

            if (weaponArm != null)
            {
                var aiSo = new SerializedObject(ai);
                aiSo.FindProperty("weaponArm").objectReferenceValue = weaponArm;
                aiSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, GoblinPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log("Goblin combat AI (Health + CharacterController + GoblinMeleeAI) added to SnarlingGoblinFighter. Both scene instances will pick it up automatically.");
        }
    }
}
