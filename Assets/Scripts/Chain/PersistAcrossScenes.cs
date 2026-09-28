using UnityEngine;

namespace DungeonCrawl.Chain
{
    /// <summary>Keeps this GameObject (and its chain-service components) alive across scene loads.</summary>
    public class PersistAcrossScenes : MonoBehaviour
    {
        private static bool _exists;

        private void Awake()
        {
            if (_exists)
            {
                Destroy(gameObject);
                return;
            }

            _exists = true;
            DontDestroyOnLoad(gameObject);
        }
    }
}
