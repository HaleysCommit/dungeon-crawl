using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DungeonCrawl.Gameplay;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup: swaps the primitive-built goblin placeholder for the real imported
    /// CC-BY-licensed glTF model (Assets/Art/Goblin/SnarlingGoblinFighter_Source/scene.gltf, requires
    /// com.unity.cloud.gltfast to have finished importing it), keeping the existing Health/CharacterController/
    /// GoblinMeleeAI combat setup intact. The source model has no rig, so GoblinIdleAnim (which animates
    /// separate torso/head/ear bones) is removed - it doesn't apply to a single static mesh.</summary>
    public static class GoblinRealModelSetup
    {
        private const string GoblinPrefabPath = "Assets/Art/Goblin/SnarlingGoblinFighter.prefab";
        private const string RealModelPath = "Assets/Art/Goblin/SnarlingGoblinFighter_Source/scene.gltf";

        [MenuItem("Dungeon Crawl/Swap In Real Goblin Model")]
        public static void SwapInRealModel()
        {
            var realModel = AssetDatabase.LoadAssetAtPath<GameObject>(RealModelPath);
            if (realModel == null)
            {
                Debug.LogError("Could not load the imported goblin model at " + RealModelPath
                    + ". Make sure com.unity.cloud.gltfast has resolved and finished importing scene.gltf.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(GoblinPrefabPath);
            if (root == null)
            {
                Debug.LogError("Could not load goblin prefab at " + GoblinPrefabPath);
                return;
            }

            // remove the old primitive-built visuals, keep root-level gameplay components (Health/CharacterController/GoblinMeleeAI)
            var childrenToRemove = new List<GameObject>();
            foreach (Transform child in root.transform)
            {
                childrenToRemove.Add(child.gameObject);
            }
            foreach (var child in childrenToRemove)
            {
                Object.DestroyImmediate(child);
            }

            var idleAnim = root.GetComponent<GoblinIdleAnim>();
            if (idleAnim != null)
            {
                Object.DestroyImmediate(idleAnim);
            }

            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(realModel, root.transform);
            modelInstance.name = "GoblinModel";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(root, GoblinPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log("Swapped in the real CC-BY goblin model on " + GoblinPrefabPath + ". Health/CharacterController/GoblinMeleeAI kept intact - retune GoblinModel's scale/position if needed to match the CharacterController.");
        }
    }
}
