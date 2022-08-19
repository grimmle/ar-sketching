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
    public string currentAnchorIdToSave = "";

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
    // void Update() {
    //     GeoLocation loc = locationProvider.GetLocationEstimate();
    //     Debug.Log($"LOCATION: {loc.Latitude}, {loc.Longitude}");
    //     Debug.Log($"spatialAnchors.Count {spatialAnchors.Count}");
    // }

    async Task LoadSketchWithAnchor(string anchorId, Vector3 pos, Quaternion rot) {
        SketchWorldManager.Load(anchorId, pos, rot);
    }

    public async Task SetupCloudSessionAsync() {
        if (spatialAnchorManager.Session == null) {
            anchorLocateCriteria = new AnchorLocateCriteria();
            await spatialAnchorManager.CreateSessionAsync();
            Debug.Log("### SESSION CREATED ###");
            await spatialAnchorManager.StartSessionAsync();
            Debug.Log("### SESSION STARTED ###");
        }
    }

    public async void StopCloudSessionAsync() {
        spatialAnchorManager.StopSession();
        await spatialAnchorManager.ResetSessionAsync();
        currentWatcher = null;
        locationProvider = null;
        CleanupSpawnedObjects();
    }

    private void CleanupSpawnedObjects() {
        Destroy(spawnedAnchorObject);
        spawnedAnchorObject = null;
    }

    public void ConfigureSensors() {
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
        criteria.DistanceInMeters = 30000;
        criteria.MaxResultCount = 20;

        anchorLocateCriteria = new AnchorLocateCriteria();

        anchorLocateCriteria.Identifiers = new string[] { "d9ace388-0e20-4148-a06b-c5520a135a95" };
        // anchorLocateCriteria.NearDevice = criteria;

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
    public async Task<string> SaveCurrentObjectAnchorToCloudAsync() {
        Debug.Log("+++ started SaveCurrentObjectAnchorToCloudAsync");
        if (spawnedAnchorObject == null) {
            spawnedAnchorObject = SpawnNewAnchoredObject(new Vector3(), new Quaternion());
        }

        CloudNativeAnchor cloudNativeAnchor = spawnedAnchorObject.GetComponent<CloudNativeAnchor>();
        if (cloudNativeAnchor.CloudAnchor == null) { await cloudNativeAnchor.NativeToCloud(); }

        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
        cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(3);

        while (!spatialAnchorManager.IsReadyForCreate) {
            await Task.Delay(200);
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"IsReadyForCreate : {spatialAnchorManager.IsReadyForCreate}");
            Debug.Log($"Move your device to capture more data points in the environment : {createProgress:0%}");
        }

        Debug.Log("Saving anchor to cloud ...");

        await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);
        currentAnchorIdToSave = cloudSpatialAnchor.Identifier;
        // currentCloudSpatialAnchor = cloudSpatialAnchor;
        bool saveSucceeded = cloudSpatialAnchor != null;

        if (!saveSucceeded) {
            Debug.LogError("ASA - Failed to save, but no exception was thrown.");
            return null;
        }

        Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
        spawnedAnchorObject.GetComponent<MeshRenderer>().material.color = Color.green;

        Debug.Log("Saved hihi");
        return cloudSpatialAnchor.Identifier;
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
        criteria.DistanceInMeters = 30000;
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
            Debug.Log($"looking for id {anchorLocateCriteria.Identifiers.ToString()}");
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
        CloudSpatialAnchor anchor = e.Anchor;
        switch (status) {
            case LocateAnchorStatus.Located: {
                    Debug.Log("Cloud Anchor found! Identifier : " + anchor.Identifier);
                    foundAnchors.text += $"\n {anchor.Identifier}";
                    try {
                        Debug.Log("onLocated pose pos: " + anchor.GetPose().position.ToString());
                        Debug.Log("onLocated pose rot: " + anchor.GetPose().rotation.ToString());
                        spatialAnchors.Add(anchor);
                        // SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation, true);
                        if (spatialAnchors.Count == 1) {
                            LoadSketchWithAnchor(anchor.Identifier, anchor.GetPose().position, anchor.GetPose().rotation);
                        }
                    } catch (Exception ex) {
                        Debug.LogError(ex);
                    }
                    break;
                }
            case LocateAnchorStatus.AlreadyTracked:
                Debug.Log("LocateAnchorStatus.AlreadyTracked");
                // This anchor has already been reported and is being tracked
                SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation, true);
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

    private async void deleteAnchorWithoutLocating(string anchorId) {
        var anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
        await spatialAnchorManager.Session.DeleteAnchorAsync(anchor);
    }
}
