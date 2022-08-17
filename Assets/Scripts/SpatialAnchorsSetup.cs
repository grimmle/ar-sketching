using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class SpatialAnchorsSetup : MonoBehaviour {
    [SerializeField]
    [Tooltip("SpatialAnchorManager instance to use. Required.")]
    private SpatialAnchorManager spatialAnchorManager;

    // private CloudSpatialAnchor currentCloudSpatialAnchor;
    private CloudSpatialAnchorWatcher currentWatcher;
    private AnchorLocateCriteria anchorLocateCriteria;
    private List<string> anchorIdsToLocate = new List<string>();
    private PlatformLocationProvider locationProvider;

    [Tooltip("Object to spawn as anchor visualization.")]
    public GameObject anchoredObjectPrefab = null;
    private GameObject spawnedAnchorObject = null;

    private string currentAnchorId = "";
    // public string currentAnchorIdToSave = "";

    List<CloudSpatialAnchor> spatialAnchors = new List<CloudSpatialAnchor>();
    public TMPro.TMP_Text foundAnchors;

    private SketchWorldManager SketchWorldManager;


    void Start() {
        SketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
        spatialAnchorManager = FindObjectOfType<SpatialAnchorManager>();
        // spatialAnchorManager.LogDebug += (sender, args) => Debug.Log($"ASA - Debug: {args.Message}");
        spatialAnchorManager.Error += (sender, args) => Debug.LogError($"ASA - Error: {args.ErrorMessage}");

        // SetupCloudSessionAsync();
    }

    public void StopCloudSessionAsync() {
        spatialAnchorManager.StopSession();
        currentWatcher = null;
        locationProvider = null;
    }

    public async void SetupCloudSessionAsync() {
        anchorLocateCriteria = new AnchorLocateCriteria();

        if (spatialAnchorManager.Session == null) {
            await spatialAnchorManager.CreateSessionAsync();
            Debug.Log("### SESSION CREATED ###");
        }

        currentAnchorId = "";

        await spatialAnchorManager.StartSessionAsync();

        Debug.Log("### SESSION STARTED ###");

        ConfigureSensors();

        //stop session
        // spatialAnchorManager.StopSession();
        // CleanupSpawnedObjects();
        // await spatialAnchorManager.ResetSessionAsync();
        // locationProvider = null;
    }

    private void ConfigureSensors() {
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
        criteria.DistanceInMeters = 1000;
        criteria.MaxResultCount = 20;

        anchorLocateCriteria = new AnchorLocateCriteria();
        // anchorLocateCriteria.Identifiers = new string[] { "id1" };
        anchorLocateCriteria.NearDevice = criteria;

        currentWatcher = CreateWatcher();
    }

    public void SetAnchorIdsToLocate(string[] anchorIds) {
        if (anchorIds == null) {
            throw new ArgumentNullException(nameof(anchorIds));
        }

        // anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();

        anchorIdsToLocate.Clear();
        anchorIdsToLocate.AddRange(anchorIds);

        anchorLocateCriteria.Identifiers = anchorIds;
        Debug.Log("anchorLocateCriteria.Identifiers");
        for (int i = 0; i < anchorLocateCriteria.Identifiers.Length; i++) {
            Debug.Log(anchorLocateCriteria.Identifiers[i]);
        }
        currentWatcher = CreateWatcher();
    }

    public async Task SaveAnchorToCloudAsync() {
        await SaveCurrentObjectAnchorToCloudAsync();
    }

    // protected async void SaveCurrentObjectAnchorToCloudAsync()
    public async Task SaveCurrentObjectAnchorToCloudAsync() {
        Debug.Log("+++ started SaveCurrentObjectAnchorToCloudAsync");
        if (spawnedAnchorObject == null) {
            spawnedAnchorObject = SpawnNewAnchoredObject(new Vector3(), new Quaternion());
        }

        CloudNativeAnchor cloudNativeAnchor = spawnedAnchorObject.GetComponent<CloudNativeAnchor>();
        if (cloudNativeAnchor.CloudAnchor == null) { await cloudNativeAnchor.NativeToCloud(); }

        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;

        while (!spatialAnchorManager.IsReadyForCreate) {
            await Task.Delay(200);
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"IsReadyForCreate : {spatialAnchorManager.IsReadyForCreate}");
            Debug.Log($"Move your device to capture more data points in the environment : {createProgress:0%}");
        }

        Debug.Log("Saving anchor to cloud ...");

        await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);
        // currentAnchorIdToSave = cloudSpatialAnchor.Identifier;
        // currentCloudSpatialAnchor = cloudSpatialAnchor;
        bool saveSucceeded = cloudSpatialAnchor != null;
        if (!saveSucceeded) {
            Debug.LogError("ASA - Failed to save, but no exception was thrown.");
            return;
        }

        Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
        spawnedAnchorObject.GetComponent<MeshRenderer>().material.color = Color.green;

        Debug.Log("Saved hihi");

    }

    protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, [Optional] bool found) {
        Debug.Log("+++ spawning new game obj");
        // Create the prefab
        GameObject newGameObject = GameObject.Instantiate(anchoredObjectPrefab, worldPos, worldRot);
        if (found) {
            newGameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        }
        // Attach a cloud-native anchor behavior to help keep cloud
        // and native anchors in sync.
        newGameObject.AddComponent<CloudNativeAnchor>();
        // Return created object
        return newGameObject;
    }

    public async Task FindNearbyAnchors() {
        Debug.LogFormat("--- FindNearbyAnchors ---");
        NearDeviceCriteria criteria = new NearDeviceCriteria();
        criteria.DistanceInMeters = 500;
        criteria.MaxResultCount = 20;

        IList<string> spatialAnchorIds = await spatialAnchorManager.Session.GetNearbyAnchorIdsAsync(criteria);

        Debug.LogFormat($"Got ids for {0} anchors", spatialAnchorIds.Count);

        foreach (string anchorId in spatialAnchorIds) {
            CloudSpatialAnchor anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
            Debug.LogFormat($"Received information about spatial anchor {0}", anchor.Identifier);
            spawnedAnchorObject = SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation);
            foundAnchors.text += $"\n {anchor.Identifier}";
        }
    }

    public async Task<CloudSpatialAnchor> getAnchorWithId(string anchorId) {
        Debug.Log($"---- getAnchorWithId {anchorId}");
        // doesnt locate the actual anchor in 3D space
        CloudSpatialAnchor anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
        Debug.Log("found anchor props " + anchor.Identifier);
        // Debug.Log("pose " + anchor.GetPose().ToString());
        // Debug.Log("pose " + anchor.GetPose().position.ToString());
        // SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation, true);
        return anchor;
    }

    protected CloudSpatialAnchorWatcher CreateWatcher() {
        Debug.Log("CreateWatcher");
        if ((spatialAnchorManager != null) && (spatialAnchorManager.Session != null)) {
            Debug.Log("+++ creating cloud anchor watcher");
            spatialAnchorManager.Session.AnchorLocated += OnAnchorLocated;
            spatialAnchorManager.AnchorLocated += OnAnchorLocated;
            return spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
        } else {
            return null;
        }
    }

    protected void OnAnchorLocated(object sender, AnchorLocatedEventArgs e) {
        Debug.Log($"--- OnAnchorLocated --- {e.Status}, {e.Anchor}");
        LocateAnchorStatus status = e.Status;
        switch (status) {
            case LocateAnchorStatus.Located: {
                    CloudSpatialAnchor anchor = e.Anchor;
                    Debug.Log("Cloud Anchor found! Identifier : " + anchor.Identifier);
                    foundAnchors.text += $"\n {anchor.Identifier}";
                    spatialAnchors.Add(anchor);
                    Debug.Log($"spatialAnchors.Count {spatialAnchors.Count}");
                    if (spatialAnchors.Count == 1) {
                        Debug.Log($"Loading Sketch for {anchor.Identifier}");
                        SketchWorldManager.Load(anchor.Identifier);
                    }
                    SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation, true);
                    break;
                }
            case LocateAnchorStatus.AlreadyTracked:
                Debug.Log("LocateAnchorStatus.AlreadyTracked");
                // This anchor has already been reported and is being tracked
                break;
            case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                Debug.Log("LocateAnchorStatus.NotLocatedAnchorDoesNotExist");
                // The anchor was deleted or never existed in the first place
                // Drop it, or show UI to ask user to anchor the content anew
                break;
            case LocateAnchorStatus.NotLocated:
                Debug.Log("LocateAnchorStatus.NotLocated");
                // The anchor hasn't been found given the location data
                // The user might in the wrong location, or maybe more data will help
                // Show UI to tell user to keep looking around
                break;
        }
    }
}
