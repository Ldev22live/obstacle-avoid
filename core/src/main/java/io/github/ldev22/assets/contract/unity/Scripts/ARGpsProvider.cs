using System;
using System.Collections;
using UnityEngine;

public class ARGpsProvider : MonoBehaviour
{
    [Header("GPS Settings")]
    public float desiredAccuracyInMeters = 10f;
    public float updateDistanceInMeters = 10f;
    public int maxWaitTimeSeconds = 20;

    [Header("Debug")]
    public bool logLocationUpdates = true;

    public bool IsLocationServiceRunning { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double Altitude { get; private set; }
    public float HorizontalAccuracy { get; private set; }

    public event Action OnLocationInitialized;
    public event Action OnLocationUpdated;

    private Coroutine locationServiceCoroutine;

    void Start()
    {
        locationServiceCoroutine = StartCoroutine(InitializeLocationService());
    }

    void OnDestroy()
    {
        if (locationServiceCoroutine != null)
            StopCoroutine(locationServiceCoroutine);

        if (Input.location.isEnabledByUser)
            Input.location.Stop();
    }

    IEnumerator InitializeLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("[ARGpsProvider] Location service is not enabled by user");
            yield break;
        }

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        int waitTime = 0;
        while (Input.location.status == LocationServiceStatus.Initializing && waitTime < maxWaitTimeSeconds)
        {
            yield return new WaitForSeconds(1);
            waitTime++;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("[ARGpsProvider] Unable to determine device location");
            yield break;
        }

        if (waitTime >= maxWaitTimeSeconds)
        {
            Debug.LogError("[ARGpsProvider] Location service initialization timed out");
            yield break;
        }

        IsLocationServiceRunning = true;
        UpdateLocationData();

        Debug.Log($"[ARGpsProvider] GPS initialized: {Latitude}, {Longitude} (accuracy: {HorizontalAccuracy}m)");
        OnLocationInitialized?.Invoke();

        StartCoroutine(LocationUpdateLoop());
    }

    IEnumerator LocationUpdateLoop()
    {
        while (IsLocationServiceRunning)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                UpdateLocationData();
                OnLocationUpdated?.Invoke();
            }
            yield return new WaitForSeconds(1f);
        }
    }

    void UpdateLocationData()
    {
        var data = Input.location.lastData;
        Latitude = data.latitude;
        Longitude = data.longitude;
        Altitude = data.altitude;
        HorizontalAccuracy = data.horizontalAccuracy;

        if (logLocationUpdates)
        {
            Debug.Log($"[ARGpsProvider] Location: {Latitude:F6}, {Longitude:F6} (±{HorizontalAccuracy:F1}m)");
        }
    }

    public bool HasValidLocation()
    {
        return IsLocationServiceRunning && HorizontalAccuracy <= desiredAccuracyInMeters * 2;
    }
}
