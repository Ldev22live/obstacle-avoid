using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSceneManager : MonoBehaviour
{
    [Header("Required Components")]
    public ARSession arSession;
    public ARSessionOrigin arOrigin;
    public ARAnchorManager anchorManager;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Custom Scripts")]
    public WorldApi worldApi;
    public ARGpsProvider gpsProvider;
    public ARWorldSpawner worldSpawner;
    public ARBootstrap bootstrap;

    [Header("UI")]
    public GameObject loadingPanel;
    public GameObject placementIndicator;

    void OnValidate()
    {
        if (arSession == null) arSession = FindObjectOfType<ARSession>();
        if (arOrigin == null) arOrigin = FindObjectOfType<ARSessionOrigin>();
        if (anchorManager == null) anchorManager = FindObjectOfType<ARAnchorManager>();
        if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
        if (worldApi == null) worldApi = FindObjectOfType<WorldApi>();
        if (gpsProvider == null) gpsProvider = FindObjectOfType<ARGpsProvider>();
        if (worldSpawner == null) worldSpawner = FindObjectOfType<ARWorldSpawner>();
        if (bootstrap == null) bootstrap = FindObjectOfType<ARBootstrap>();
    }

    void Start()
    {
        ShowLoading(true);

        if (gpsProvider != null)
            gpsProvider.OnLocationInitialized += OnLocationReady;
    }

    void OnDestroy()
    {
        if (gpsProvider != null)
            gpsProvider.OnLocationInitialized -= OnLocationReady;
    }

    void OnLocationReady()
    {
        ShowLoading(false);
        Debug.Log("[ARSceneManager] Location acquired, ready to spawn world objects");
    }

    void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);
    }

    public void ReloadZone(string zoneKey)
    {
        if (worldSpawner != null)
        {
            worldSpawner.currentZoneKey = zoneKey;
            OnLocationReady();
        }
    }
}
