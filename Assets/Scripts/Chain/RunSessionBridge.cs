using UnityEngine;

namespace DungeonCrawl.Chain
{
    /// <summary>
    /// Bridges TopDown Engine gameplay events (wired via UnityEvents in the Inspector, e.g. a
    /// Health component's death event) to the backend session API. Keep one instance alive for
    /// the whole run (e.g. on the same GameObject as SessionApiClient / GameManager).
    /// </summary>
    [RequireComponent(typeof(SessionApiClient))]
    public class RunSessionBridge : MonoBehaviour
    {
        private SessionApiClient _api;
        private string _sessionId;
        private int _floor = 1;

        private void Awake() => _api = GetComponent<SessionApiClient>();

        public void BeginRun(string sessionId, int startingFloor = 1)
        {
            _sessionId = sessionId;
            _floor = startingFloor;
        }

        /// <summary>Wire this to WalletConnectBootstrap.AccountConnected to start a backend run-session as soon as a wallet connects.</summary>
        public void StartSessionForPlayer(string playerAddress, int startingFloor = 1)
        {
            StartCoroutine(_api.StartSession(playerAddress,
                onSuccess: sessionId => BeginRun(sessionId, startingFloor),
                onError: err => Debug.LogWarning($"StartSession failed: {err}")));
        }

        /// <summary>Wire an enemy's death UnityEvent to this from the Inspector.</summary>
        public void ReportKill(string enemyId)
        {
            if (string.IsNullOrEmpty(_sessionId)) return;
            StartCoroutine(_api.ReportKill(_sessionId, enemyId,
                onError: err => Debug.LogWarning($"ReportKill failed: {err}")));
        }

        /// <summary>Wire a pickup's collection UnityEvent to this from the Inspector.</summary>
        public void ReportLoot(int itemId, int amount)
        {
            if (string.IsNullOrEmpty(_sessionId)) return;
            StartCoroutine(_api.ReportLoot(_sessionId, itemId, amount,
                onError: err => Debug.LogWarning($"ReportLoot failed: {err}")));
        }

        /// <summary>Wire your level-exit trigger's activation UnityEvent to this from the Inspector.</summary>
        public void ReportFloorClear()
        {
            if (string.IsNullOrEmpty(_sessionId)) return;
            StartCoroutine(_api.ReportFloorClear(_sessionId, _floor,
                onError: err => Debug.LogWarning($"ReportFloorClear failed: {err}")));

            // Local, instant UI feedback via TDE's own achievement system, independent of the
            // onchain mint which settles asynchronously. Requires an AchievementList with a
            // matching AchievementID (see TDE's "Setting up your first achievement" recipe).
            MoreMountains.Tools.MMAchievementManager.UnlockAchievement($"Floor{_floor}Cleared");

            _floor++;
        }

        /// <summary>Call when the player banks/ends the run, to mint accrued rewards.</summary>
        public void EndRun(System.Action<RunResult> onSettled)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                onSettled?.Invoke(null);
                return;
            }

            StartCoroutine(_api.EndSession(_sessionId,
                onSuccess: result =>
                {
                    _sessionId = null;
                    onSettled?.Invoke(result);
                },
                onError: err =>
                {
                    Debug.LogWarning($"EndRun failed: {err}");
                    onSettled?.Invoke(null);
                }));
        }
    }
}
