using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ARWorldObject
{
    public string id;
    public string name;
    public float lat;
    public float lon;
    public float height;
    public string prefab;
    public string zoneKey;
}

[System.Serializable]
public class ObjectUpdateRequest
{
    public string id;
    public float lat;
    public float lon;
    public float height;
}

[System.Serializable]
public class ARWorldResponse
{
    public ARWorldObject[] objects;
}

public class WorldApi : MonoBehaviour
{
    [Header("API Configuration")]
    public string baseUrl = "http://urconnex.com";
    public string apiPath = "/api/ar";

    [Header("Request Settings")]
    public float timeoutSeconds = 30f;

    public ARWorldResponse worldData;

    private string FullApiUrl => $"{baseUrl}{apiPath}";

    public IEnumerator FetchWorld(Action onComplete)
    {
        string url = $"{FullApiUrl}/world";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = (int)timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WorldApi] FetchWorld Error: {request.error}");
            yield break;
        }

        worldData = JsonUtility.FromJson<ARWorldResponse>(request.downloadHandler.text);
        Debug.Log($"[WorldApi] Loaded {worldData?.objects?.Length ?? 0} objects");

        onComplete?.Invoke();
    }

    public IEnumerator FetchZone(string zoneKey, double userLat, double userLon, Action onComplete)
    {
        string url = $"{FullApiUrl}/zones/{zoneKey}/objects?lat={userLat}&lon={userLon}";
        Debug.Log($"[WorldApi] Fetching zone: {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = (int)timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[WorldApi] FetchZone Error: {request.error}");
            yield break;
        }

        worldData = JsonUtility.FromJson<ARWorldResponse>(request.downloadHandler.text);
        Debug.Log($"[WorldApi] Loaded {worldData?.objects?.Length ?? 0} objects for zone {zoneKey}");

        onComplete?.Invoke();
    }

    public IEnumerator SaveObjectUpdate(ObjectUpdateRequest update, Action<bool> onComplete = null)
    {
        string url = $"{FullApiUrl}/objects/{update.id}";
        string json = JsonUtility.ToJson(update);

        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)timeoutSeconds;

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (!success)
        {
            Debug.LogError($"[WorldApi] SaveObjectUpdate Error: {request.error}");
        }
        else
        {
            Debug.Log($"[WorldApi] Saved object {update.id} position");
        }

        onComplete?.Invoke(success);
    }

    public IEnumerator SaveNewObject(ARWorldObject newObject, Action<bool, string> onComplete = null)
    {
        string url = $"{FullApiUrl}/objects";
        string json = JsonUtility.ToJson(newObject);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)timeoutSeconds;

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string responseId = null;

        if (!success)
        {
            Debug.LogError($"[WorldApi] SaveNewObject Error: {request.error}");
        }
        else
        {
            responseId = request.downloadHandler.text;
            Debug.Log($"[WorldApi] Created object with ID: {responseId}");
        }

        onComplete?.Invoke(success, responseId);
    }
}