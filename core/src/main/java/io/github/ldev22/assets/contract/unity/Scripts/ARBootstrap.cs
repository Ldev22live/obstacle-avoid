using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARBootstrap : MonoBehaviour
{
    public static WorldApi WorldApi;
    public static ARSessionOrigin SessionOrigin;
    public static ARGpsProvider GpsProvider;
    public static ARSession Session;
    public static RemoteLogger RemoteLogger;

    [Header("Auto-Initialize")]
    public bool initializeOnStart = true;

    void Awake()
    {
        CacheReferences();
    }

    void Start()
    {
        if (initializeOnStart)
        {
            InitializeAR();
        }
    }

    void CacheReferences()
    {
        WorldApi = FindObjectOfType<WorldApi>();
        SessionOrigin = FindObjectOfType<ARSessionOrigin>();
        GpsProvider = FindObjectOfType<ARGpsProvider>();
        Session = FindObjectOfType<ARSession>();
        RemoteLogger = FindObjectOfType<RemoteLogger>();

        if (WorldApi == null)
            Debug.LogWarning("[ARBootstrap] WorldApi not found in scene");
        if (SessionOrigin == null)
            Debug.LogWarning("[ARBootstrap] ARSessionOrigin not found in scene");
        if (GpsProvider == null)
            Debug.LogWarning("[ARBootstrap] ARGpsProvider not found in scene");
        if (Session == null)
            Debug.LogWarning("[ARBootstrap] ARSession not found in scene");
        if (RemoteLogger == null)
            Debug.LogWarning("[ARBootstrap] RemoteLogger not found in scene");
    }

    public void InitializeAR()
    {
        Debug.Log("[ARBootstrap] AR System Initialized");
        Debug.Log($"  - WorldApi: {WorldApi != null}");
        Debug.Log($"  - SessionOrigin: {SessionOrigin != null}");
        Debug.Log($"  - GpsProvider: {GpsProvider != null}");
        Debug.Log($"  - ARSession: {Session != null}");
        Debug.Log($"  - RemoteLogger: {RemoteLogger != null}");

        RemoteLogger?.LogEvent("AR_Initialized", $"Device: {SystemInfo.deviceModel}");
    }
}