using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARWorldSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject treePrefab;
    public GameObject rockPrefab;

    [Header("AR Components")]
    public ARAnchorManager anchorManager;
    public ARRaycastManager raycastManager;

    [Header("Settings")]
    public string currentZoneKey = "default";
    public bool useAnchors = true;
    public float maxPlacementDistance = 100f;

    private Dictionary<string, GameObject> spawnedObjects = new();
    private Dictionary<string, ARAnchor> objectAnchors = new();

    void Start()
    {
        if (anchorManager == null)
            anchorManager = FindObjectOfType<ARAnchorManager>();
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();

        ARBootstrap.GpsProvider.OnLocationInitialized += OnGpsReady;
    }

    void OnDestroy()
    {
        if (ARBootstrap.GpsProvider != null)
            ARBootstrap.GpsProvider.OnLocationInitialized -= OnGpsReady;
    }

    void OnGpsReady()
    {
        var gps = ARBootstrap.GpsProvider;
        StartCoroutine(ARBootstrap.WorldApi.FetchZone(
            currentZoneKey,
            gps.Latitude,
            gps.Longitude,
            SpawnWorld
        ));
    }

    void SpawnWorld()
    {
        ClearSpawnedObjects();

        var objects = ARBootstrap.WorldApi.worldData?.objects;
        if (objects == null) return;

        foreach (var obj in objects)
        {
            SpawnObject(obj);
        }

        Debug.Log($"[ARWorldSpawner] Spawned {spawnedObjects.Count} objects");
    }

    void SpawnObject(ARWorldObject objData)
    {
        GameObject prefab = GetPrefab(objData.prefab);
        Vector3 worldPosition = GeoToWorldPosition(objData.lat, objData.lon, objData.height);

        if (worldPosition.magnitude > maxPlacementDistance)
        {
            Debug.LogWarning($"[ARWorldSpawner] Object {objData.id} too far ({worldPosition.magnitude:F1}m), skipping");
            return;
        }

        GameObject instance;

        if (useAnchors && anchorManager != null)
        {
            Pose pose = new Pose(worldPosition, Quaternion.identity);
            ARAnchor anchor = anchorManager.AttachAnchor(null, pose);

            if (anchor != null)
            {
                instance = Instantiate(prefab, anchor.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                objectAnchors[objData.id] = anchor;
            }
            else
            {
                Debug.LogWarning($"[ARWorldSpawner] Failed to create anchor for {objData.id}, spawning without anchor");
                instance = Instantiate(prefab, worldPosition, Quaternion.identity);
            }
        }
        else
        {
            instance = Instantiate(prefab, worldPosition, Quaternion.identity);
        }

        instance.name = $"ARObject_{objData.name}_{objData.id}";
        spawnedObjects[objData.id] = instance;

        var interactable = instance.AddComponent<ARObjectInteractable>();
        interactable.Initialize(objData, this);
    }

    public void UpdateObjectPosition(string objectId, Vector3 newWorldPosition)
    {
        if (!spawnedObjects.TryGetValue(objectId, out GameObject obj))
        {
            Debug.LogError($"[ARWorldSpawner] Object {objectId} not found");
            return;
        }

        Vector3 geoPos = WorldToGeoPosition(newWorldPosition);

        var update = new ObjectUpdateRequest
        {
            id = objectId,
            lat = geoPos.x,
            lon = geoPos.z,
            height = geoPos.y
        };

        StartCoroutine(ARBootstrap.WorldApi.SaveObjectUpdate(update, success =>
        {
            if (success)
            {
                Debug.Log($"[ARWorldSpawner] Updated object {objectId} position saved");
            }
            else
            {
                Debug.LogError($"[ARWorldSpawner] Failed to save object {objectId} position");
            }
        }));
    }

    Vector3 GeoToWorldPosition(float lat, float lon, float height)
    {
        if (ARBootstrap.GpsProvider == null || !ARBootstrap.GpsProvider.IsLocationServiceRunning)
        {
            Debug.LogError("[ARWorldSpawner] GPS not available for coordinate conversion");
            return Vector3.zero;
        }

        double originLat = ARBootstrap.GpsProvider.Latitude;
        double originLon = ARBootstrap.GpsProvider.Longitude;

        const double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * Math.Cos(originLat * Math.PI / 180.0);

        float x = (float)((lon - originLon) * metersPerDegreeLon);
        float z = (float)((lat - originLat) * metersPerDegreeLat);

        return new Vector3(x, height, z);
    }

    Vector3 WorldToGeoPosition(Vector3 worldPosition)
    {
        if (ARBootstrap.GpsProvider == null)
            return Vector3.zero;

        double originLat = ARBootstrap.GpsProvider.Latitude;
        double originLon = ARBootstrap.GpsProvider.Longitude;

        const double metersPerDegreeLat = 111320.0;
        double metersPerDegreeLon = 111320.0 * Math.Cos(originLat * Math.PI / 180.0);

        float lat = (float)(originLat + worldPosition.z / metersPerDegreeLat);
        float lon = (float)(originLon + worldPosition.x / metersPerDegreeLon);
        float height = worldPosition.y;

        return new Vector3(lat, height, lon);
    }

    GameObject GetPrefab(string name)
    {
        return name switch
        {
            "TreePrefab" => treePrefab,
            "RockPrefab" => rockPrefab,
            _ => treePrefab
        };
    }

    void ClearSpawnedObjects()
    {
        foreach (var anchor in objectAnchors.Values)
        {
            if (anchor != null)
                Destroy(anchor.gameObject);
        }

        foreach (var obj in spawnedObjects.Values)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();
        objectAnchors.Clear();
    }
}