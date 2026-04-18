using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class LogEntry
{
    public string timestamp;
    public string level;
    public string message;
    public string stackTrace;
    public string deviceId;
    public string deviceModel;
    public string osVersion;
    public string appVersion;
}

[System.Serializable]
public class LogBatch
{
    public LogEntry[] logs;
}

public class RemoteLogger : MonoBehaviour
{
    [Header("Server Configuration")]
    public string baseUrl = "http://urconnex.com:3000";
    public string logEndpoint = "/api/ar/logs";
    public float flushIntervalSeconds = 30f;
    public int maxBatchSize = 50;
    public int maxQueueSize = 200;

    [Header("Log Levels")]
    public bool logErrors = true;
    public bool logWarnings = true;
    public bool logInfo = false;
    public bool flushOnError = true;

    [Header("Device Info")]
    public string deviceId;

    private Queue<LogEntry> logQueue = new();
    private bool isFlushing = false;
    private string fullEndpoint;

    public static RemoteLogger Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        fullEndpoint = $"{baseUrl}{logEndpoint}";
        deviceId = SystemInfo.deviceUniqueIdentifier;

        Application.logMessageReceived += OnLogMessage;
    }

    void Start()
    {
        StartCoroutine(PeriodicFlush());
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessage;
        FlushLogsImmediately();
    }

    void OnLogMessage(string message, string stackTrace, LogType type)
    {
        bool shouldLog = type switch
        {
            LogType.Error or LogType.Exception or LogType.Assert => logErrors,
            LogType.Warning => logWarnings,
            _ => logInfo
        };

        if (!shouldLog) return;

        var entry = new LogEntry
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            level = type.ToString(),
            message = message,
            stackTrace = stackTrace,
            deviceId = deviceId,
            deviceModel = SystemInfo.deviceModel,
            osVersion = SystemInfo.operatingSystem,
            appVersion = Application.version
        };

        lock (logQueue)
        {
            if (logQueue.Count >= maxQueueSize)
                logQueue.Dequeue();
            logQueue.Enqueue(entry);
        }

        if (flushOnError && (type == LogType.Error || type == LogType.Exception))
        {
            StartCoroutine(FlushLogsAsync());
        }
    }

    IEnumerator PeriodicFlush()
    {
        while (true)
        {
            yield return new WaitForSeconds(flushIntervalSeconds);
            yield return FlushLogsAsync();
        }
    }

    public void FlushLogsImmediately()
    {
        if (logQueue.Count > 0)
        {
            StartCoroutine(FlushLogsAsync());
        }
    }

    IEnumerator FlushLogsAsync()
    {
        if (isFlushing || logQueue.Count == 0) yield break;

        isFlushing = true;

        List<LogEntry> batch = new();
        lock (logQueue)
        {
            int count = Math.Min(logQueue.Count, maxBatchSize);
            for (int i = 0; i < count; i++)
            {
                batch.Add(logQueue.Dequeue());
            }
        }

        if (batch.Count == 0)
        {
            isFlushing = false;
            yield break;
        }

        var payload = new LogBatch { logs = batch.ToArray() };
        string json = JsonUtility.ToJson(payload);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest(fullEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[RemoteLogger] Flushed {batch.Count} logs to server");
        }
        else
        {
            Debug.LogError($"[RemoteLogger] Failed to send logs: {request.error}");
            lock (logQueue)
            {
                foreach (var entry in batch)
                {
                    if (logQueue.Count < maxQueueSize)
                        logQueue.Enqueue(entry);
                }
            }
        }

        isFlushing = false;
    }

    public void LogEvent(string eventName, string details = "")
    {
        Debug.Log($"[EVENT] {eventName}: {details}");
    }
}
