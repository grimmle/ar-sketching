using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

public class SpatialAnchorsSetup : MonoBehaviour
{
    [SerializeField]
    [Tooltip("SpatialAnchorManager instance to use. This is required.")]
    private SpatialAnchorManager spatialAnchorManager; //CloudManager

    // private CloudSpatialAnchor currentCloudSpatialAnchor;
    private CloudSpatialAnchorWatcher currentWatcher;
    private AnchorLocateCriteria anchorLocateCriteria;
    private List<string> anchorIdsToLocate = new List<string>();
    private PlatformLocationProvider locationProvider;

    public GameObject anchoredObjectPrefab = null; // SpatialAnchorGroup prefab
    protected GameObject spawnedAnchorObject = null;

    private string currentAnchorId = "";


    void Awake()
    {
        // appStateManager = FindObjectOfType<AppStateManager>();
        // generalConfiguration = FindObjectOfType<GeneralConfiguration>();
        spatialAnchorManager = FindObjectOfType<SpatialAnchorManager>();
        spatialAnchorManager.LogDebug += (sender, args) => Debug.Log($"ASA - Debug: {args.Message}");
        spatialAnchorManager.Error += (sender, args) => Debug.LogError($"ASA - Error: {args.ErrorMessage}");

        // SetupCloudSessionAsync();
    }

    public void StopCloudSessionAsync()
    {
        spatialAnchorManager.StopSession();
        currentWatcher = null;
        locationProvider = null;
    }

    public async void SetupCloudSessionAsync()
    {
        // spatialAnchorManager.AnchorLocated += CloudManagerAnchorLocated;

        anchorLocateCriteria = new AnchorLocateCriteria();

        if (spatialAnchorManager.Session == null)
        {
            await spatialAnchorManager.CreateSessionAsync();
        }

        currentAnchorId = "";
        // currentCloudSpatialAnchor = null;

        ConfigureSession();

        await spatialAnchorManager.StartSessionAsync();

        ConfigureSensors();

        // SensorPermissionHelper.RequestSensorPermissions();
        // ConfigureSensors();

        // currentWatcher = CreateWatcher();

        // FindNearbyAnchors();

        // SaveAnchorToCloudAsync();

        //stop session
        // spatialAnchorManager.StopSession();
        // CleanupSpawnedObjects();
        // await spatialAnchorManager.ResetSessionAsync();
        // locationProvider = null;
    }

    private void ConfigureSession()
    {
        List<string> anchorsToFind = new List<string>();
        anchorsToFind.Add(currentAnchorId);

        SetAnchorIdsToLocate(anchorsToFind);
    }
    private void ConfigureSensors()
    {
        Debug.Log("--- ConfigureSensors ---");
        locationProvider = new PlatformLocationProvider();
        // Allow GPS
        locationProvider.Sensors.GeoLocationEnabled = true;
        // Allow WiFi scanning
        // locationProvider.Sensors.WifiEnabled = true;
        // Allow a set of known BLE beacons
        // locationProvider.Sensors.BluetoothEnabled = true;
        // locationProvider.Sensors.KnownBeaconProximityUuids = new[]{
        //     "22e38f1a-c1b3-452b-b5ce-fdb0f39535c1",
        //     "a63819b9-8b7b-436d-88ec-ea5d8db2acb0"
        // };

        spatialAnchorManager.Session.LocationProvider = locationProvider;

        NearDeviceCriteria criteria = new NearDeviceCriteria();
        criteria.DistanceInMeters = 30;
        criteria.MaxResultCount = 20;

        anchorLocateCriteria = new AnchorLocateCriteria();
        anchorLocateCriteria.NearDevice = criteria;
        CreateWatcher();
        // spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
    }

    protected void SetAnchorIdsToLocate(IEnumerable<string> anchorIds)
    {
        if (anchorIds == null)
        {
            throw new ArgumentNullException(nameof(anchorIds));
        }

        anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();

        anchorIdsToLocate.Clear();
        anchorIdsToLocate.AddRange(anchorIds);

        anchorLocateCriteria.Identifiers = anchorIdsToLocate.ToArray();
    }

    public async void SaveAnchorToCloudAsync()
    {
        await SaveCurrentObjectAnchorToCloudAsync();
    }

    // protected async void SaveCurrentObjectAnchorToCloudAsync()
    protected async Task SaveCurrentObjectAnchorToCloudAsync()
    {
        Debug.Log("+++ started SaveCurrentObjectAnchorToCloudAsync");
        if (spawnedAnchorObject == null)
        {
            spawnedAnchorObject = SpawnNewAnchoredObject(new Vector3(), new Quaternion());
        }

        CloudNativeAnchor cloudNativeAnchor = spawnedAnchorObject.GetComponent<CloudNativeAnchor>();
        if (cloudNativeAnchor.CloudAnchor == null) { await cloudNativeAnchor.NativeToCloud(); }

        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;

        while (!spatialAnchorManager.IsReadyForCreate)
        {
            await Task.Delay(200);
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"IsReadyForCreate : {spatialAnchorManager.IsReadyForCreate}");
            Debug.Log($"Move your device to capture more data points in the environment : {createProgress:0%}");
        }

        Debug.Log("Saving anchor to cloud ...");

        await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);
        // currentCloudSpatialAnchor = cloudSpatialAnchor;
        bool saveSucceeded = cloudSpatialAnchor != null;
        if (!saveSucceeded)
        {
            Debug.LogError("ASA - Failed to save, but no exception was thrown.");
            return;
        }

        Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
        spawnedAnchorObject.GetComponent<MeshRenderer>().material.color = Color.green;

        Debug.Log("Saved hihi");

    }

    protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
    {
        Debug.Log("+++ spawning new game obj");
        // Create the prefab
        GameObject newGameObject = GameObject.Instantiate(anchoredObjectPrefab, worldPos, worldRot);
        // Attach a cloud-native anchor behavior to help keep cloud
        // and native anchors in sync.
        newGameObject.AddComponent<CloudNativeAnchor>();
        // Return created object
        return newGameObject;
    }

    // public async void FindNearbyAnchors()
    // {
    //     NearDeviceCriteria criteria = new NearDeviceCriteria();
    //     criteria.DistanceInMeters = 30;
    //     criteria.MaxResultCount = 20;

    //     IList<string> spatialAnchorIds = await spatialAnchorManager.Session.GetNearbyAnchorIdsAsync(criteria);

    //     Debug.LogFormat("Got ids for {0} anchors", spatialAnchorIds.Count);

    //     // List<CloudSpatialAnchor> spatialAnchors = new List<CloudSpatialAnchor>();

    //     foreach (string anchorId in spatialAnchorIds)
    //     {
    //         CloudSpatialAnchor anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
    //         Debug.LogFormat("Received information about spatial anchor {0}", anchor.Identifier);
    //         // spatialAnchors.Add(anchor);
    //         spawnedAnchorObject = SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation);
    //     }
    // }

    protected CloudSpatialAnchorWatcher CreateWatcher()
    {
        Debug.Log("+++ creating cloud anchor watcher");
        if ((spatialAnchorManager != null) && (spatialAnchorManager.Session != null))
        {
            spatialAnchorManager.AnchorLocated += OnAnchorLocated;
            return spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
        }
        else
        {
            return null;
        }
    }

    protected void OnAnchorLocated(object sender, AnchorLocatedEventArgs e)
    {
        Debug.Log($"--- OnAnchorLocated --- {e.Status}, {e.Anchor}");
        LocateAnchorStatus status = e.Status;
        switch (status)
        {
            case LocateAnchorStatus.AlreadyTracked:
                break;

            case LocateAnchorStatus.Located:
                {
                    CloudSpatialAnchor anchor = e.Anchor;
                    Debug.Log("Cloud Anchor found! Identifier : " + anchor.Identifier);
                    SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation);
                    break;
                }

            case LocateAnchorStatus.NotLocated:
            case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                break;
        }
    }
}