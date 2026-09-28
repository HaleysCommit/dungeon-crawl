using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace DungeonCrawl.Chain
{
    /// <summary>
    /// Settles the run onchain when TopDownEngine signals GameOver or LevelComplete.
    /// </summary>
    public class RunEndTrigger : MonoBehaviour, MMEventListener<TopDownEngineEvent>
    {
        [SerializeField] private RunSessionBridge runSessionBridge;

        public event System.Action<RunResult> RunSettled;

        private void OnEnable() => this.MMEventStartListening<TopDownEngineEvent>();
        private void OnDisable() => this.MMEventStopListening<TopDownEngineEvent>();

        public void OnMMEvent(TopDownEngineEvent tdEvent)
        {
            if (tdEvent.EventType != TopDownEngineEventTypes.GameOver &&
                tdEvent.EventType != TopDownEngineEventTypes.LevelComplete)
            {
                return;
            }

            runSessionBridge.EndRun(result =>
            {
                if (result != null)
                {
                    Debug.Log($"Run settled onchain: {result.gold} gold, {result.items?.Length ?? 0} items, {result.txHashes?.Length ?? 0} tx(s)");
                }
                RunSettled?.Invoke(result);
            });
        }
    }
}
