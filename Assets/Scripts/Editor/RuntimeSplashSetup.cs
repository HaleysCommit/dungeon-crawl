using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonCrawl.EditorTools
{
    /// <summary>
    /// Wires up the runtime-built DungeonSplashScreen (full-screen artwork + transparent PLAY
    /// hitbox) into Splash.unity, replacing the earlier code-drawn placeholder UI.
    /// </summary>
    public static class RuntimeSplashSetup
    {
        private const string SplashScenePath = "Assets/Scenes/Splash.unity";
        private const string ArtworkPath = "Assets/Art/UI/DungeonSplashArtwork.png";
        private const string ButtonFontPath = "Assets/Fonts/RoadRage-Regular.ttf";
        private const string ButtonSpritePath = "Assets/Art/UI/PlayButtonText.png";

        [MenuItem("Dungeon Crawl/Setup Runtime Splash Screen")]
        public static void SetupRuntimeSplashScreen()
        {
            var scene = EditorSceneManager.OpenScene(SplashScenePath, OpenSceneMode.Single);

            // Permanently remove the old code-drawn placeholder canvas (from SplashArtSetup) -
            // destroying it (not just disabling) so re-running that older command can't resurrect
            // a duplicate on top of the real artwork. Guarded against duplicates/nested matches
            // where destroying one entry invalidates another still queued in this same loop.
            foreach (var t in Object.FindObjectsOfType<Transform>(true))
            {
                if (t != null && t.name == "DungeonCrawlTitleCanvas")
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            var splashGO = GameObject.Find("SplashScreen");
            if (splashGO == null)
            {
                splashGO = new GameObject("SplashScreen");
            }

            var splash = splashGO.GetComponent<DungeonSplashScreen>();
            if (splash == null)
            {
                splash = splashGO.AddComponent<DungeonSplashScreen>();
            }

            var artwork = AssetDatabase.LoadAssetAtPath<Sprite>(ArtworkPath);
            var buttonFont = AssetDatabase.LoadAssetAtPath<Font>(ButtonFontPath);
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
            var so = new SerializedObject(splash);
            so.FindProperty("splashArtwork").objectReferenceValue = artwork;
            so.FindProperty("buttonFont").objectReferenceValue = buttonFont;
            so.FindProperty("playButtonSprite").objectReferenceValue = buttonSprite;
            so.FindProperty("gameSceneName").stringValue = "Dungeon";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (artwork == null)
            {
                Debug.LogWarning($"No artwork found at {ArtworkPath} - assign DungeonSplashScreen.splashArtwork manually in the Inspector.");
            }
            else
            {
                Debug.Log("Runtime splash screen set up: SplashScreen GameObject wired with DungeonSplashScreen, artwork assigned, gameSceneName = \"Dungeon\".");
            }
        }
    }
}
