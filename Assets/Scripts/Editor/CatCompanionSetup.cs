using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.TopDownEngine;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup: adds a Cat companion that follows the player into Assets/Scenes/Dungeon.unity.</summary>
    public static class CatCompanionSetup
    {
        private const string DungeonScenePath = "Assets/Scenes/Dungeon.unity";
        private static readonly Color CatColor = new Color(0.85f, 0.55f, 0.25f);

        [MenuItem("Dungeon Crawl/Setup Cat Companion In Dungeon Scene")]
        public static void SetupCatCompanion()
        {
            var scene = EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Single);

            if (GameObject.Find("Cat") != null)
            {
                Debug.Log("Cat companion already present in Dungeon scene, skipping.");
                return;
            }

            Character player = null;
            foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
            {
                if (character.CharacterType == Character.CharacterTypes.Player)
                {
                    player = character;
                    break;
                }
            }

            if (player == null)
            {
                Debug.LogWarning("No Player character found in Dungeon scene. Add the Cat manually once a player character is present.");
                return;
            }

            // placeholder body until real cat art/model is dropped in - see Assets/Art/Splash for the reference look
            var cat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cat.name = "Cat";
            cat.transform.localScale = new Vector3(0.35f, 0.25f, 0.5f);
            cat.transform.position = player.transform.position - player.transform.forward * 1.5f;

            var renderer = cat.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = CatColor };

            Object.DestroyImmediate(cat.GetComponent<CapsuleCollider>());

            var companion = cat.AddComponent<CatCompanion>();
            var so = new SerializedObject(companion);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Cat companion added to Dungeon scene, following " + player.name + ".");
        }
    }
}
