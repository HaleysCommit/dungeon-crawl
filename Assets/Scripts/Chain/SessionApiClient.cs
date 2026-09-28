using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DungeonCrawl.Chain
{
    [Serializable]
    public class RunResult
    {
        public string gold;
        public int[] items;
        public int[] floorsCleared;
        public string[] txHashes;
    }

    [Serializable]
    internal class StartSessionRequest
    {
        public string player;
    }

    [Serializable]
    internal class StartSessionResponse
    {
        public string sessionId;
    }

    [Serializable]
    internal class KillEvent
    {
        public string type = "kill";
        public string enemyId;
    }

    [Serializable]
    internal class LootEvent
    {
        public string type = "loot";
        public int itemId;
        public int amount;
    }

    [Serializable]
    internal class FloorClearEvent
    {
        public string type = "floor_clear";
        public int floor;
    }

    /// <summary>
    /// Coroutine-based client for the backend's run-session REST API
    /// (mirrors frontend/src/net/sessionApi.ts). Attach to a persistent GameObject.
    /// </summary>
    public class SessionApiClient : MonoBehaviour
    {
        [SerializeField] private string backendUrl = "http://localhost:8787";

        public IEnumerator StartSession(string player, Action<string> onSuccess, Action<string> onError)
        {
            var body = JsonUtility.ToJson(new StartSessionRequest { player = player });
            using var request = BuildJsonRequest($"{backendUrl}/session/start", "POST", body);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
                yield break;
            }

            var response = JsonUtility.FromJson<StartSessionResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response.sessionId);
        }

        public IEnumerator ReportKill(string sessionId, string enemyId, Action onDone = null, Action<string> onError = null)
        {
            var body = JsonUtility.ToJson(new KillEvent { enemyId = enemyId });
            yield return SendEvent(sessionId, body, onDone, onError);
        }

        public IEnumerator ReportLoot(string sessionId, int itemId, int amount, Action onDone = null, Action<string> onError = null)
        {
            var body = JsonUtility.ToJson(new LootEvent { itemId = itemId, amount = amount });
            yield return SendEvent(sessionId, body, onDone, onError);
        }

        public IEnumerator ReportFloorClear(string sessionId, int floor, Action onDone = null, Action<string> onError = null)
        {
            var body = JsonUtility.ToJson(new FloorClearEvent { floor = floor });
            yield return SendEvent(sessionId, body, onDone, onError);
        }

        private IEnumerator SendEvent(string sessionId, string jsonBody, Action onDone, Action<string> onError)
        {
            using var request = BuildJsonRequest($"{backendUrl}/session/{sessionId}/event", "POST", jsonBody);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
                yield break;
            }

            onDone?.Invoke();
        }

        public IEnumerator EndSession(string sessionId, Action<RunResult> onSuccess, Action<string> onError)
        {
            using var request = new UnityWebRequest($"{backendUrl}/session/{sessionId}/end", "POST")
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
                yield break;
            }

            var result = JsonUtility.FromJson<RunResult>(request.downloadHandler.text);
            onSuccess?.Invoke(result);
        }

        private static UnityWebRequest BuildJsonRequest(string url, string method, string jsonBody)
        {
            var request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }
    }
}
