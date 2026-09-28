using DungeonCrawl.Chain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup: wires wallet/session bootstrap components into Assets/Scenes/Splash.unity.</summary>
    public static class ChainSceneSetup
    {
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string AppKitPrefabPath = "Packages/com.reown.appkit.unity/Prefabs/Reown AppKit.prefab";

        [MenuItem("Dungeon Crawl/Setup Chain Services In Splash Scene")]
        public static void SetupChainServices()
        {
            var scene = EditorSceneManager.OpenScene(SplashScenePath, OpenSceneMode.Single);

            if (GameObject.Find("ChainServices") != null)
            {
                Debug.Log("ChainServices already present in Splash scene, skipping.");
                return;
            }

            var chainServices = new GameObject("ChainServices");
            chainServices.AddComponent<PersistAcrossScenes>();
            chainServices.AddComponent<SessionApiClient>();
            var runBridge = chainServices.AddComponent<RunSessionBridge>();
            var runEndTrigger = chainServices.AddComponent<RunEndTrigger>();
            var wallet = chainServices.AddComponent<WalletConnectBootstrap>();

            var runEndTriggerSO = new SerializedObject(runEndTrigger);
            runEndTriggerSO.FindProperty("runSessionBridge").objectReferenceValue = runBridge;
            runEndTriggerSO.ApplyModifiedPropertiesWithoutUndo();

            var walletSO = new SerializedObject(wallet);
            walletSO.FindProperty("runSessionBridge").objectReferenceValue = runBridge;
            walletSO.ApplyModifiedPropertiesWithoutUndo();

            var appKitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AppKitPrefabPath);
            if (appKitPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(appKitPrefab, scene);
            }
            else
            {
                Debug.LogWarning($"Could not find Reown AppKit prefab at {AppKitPrefabPath}. Drag it into the Splash scene manually.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ChainServices set up in Splash scene and saved. Remember to fill in the Project ID on WalletConnectBootstrap.");
        }
    }
}
