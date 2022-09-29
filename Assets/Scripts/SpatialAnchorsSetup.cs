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

    private SketchWorldManager sketchWorldManager;

    //list of all found anchors
    [SerializeField]
    public List<CloudSpatialAnchor> spatialAnchors = new List<CloudSpatialAnchor>();
    [SerializeField]
    public TMPro.TMP_Text anchorCreationProgress;

    [SerializeField]
    public GameObject foundAnchorButtonPrefab;
    private GameObject foundAnchorsOverlay;
    private GameObject listOfFoundAnchors;

    void Start() {
        sketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
        foundAnchorsOverlay = GameObject.Find("Located Sketches List");
        listOfFoundAnchors = GameObject.Find("Panel");

        spatialAnchorManager = FindObjectOfType<SpatialAnchorManager>();
        // spatialAnchorManager.LogDebug += (sender, args) => Debug.Log($"ASA - Debug: {args.Message}");
        spatialAnchorManager.Error += (sender, args) => Debug.LogError($"ASA - Error: {args.ErrorMessage}");
    }

    // void Update() {
    //     GeoLocation loc = locationProvider.GetLocationEstimate();
    //     Debug.Log($"LOCATION: {loc.Latitude}, {loc.Longitude}");
    //     Debug.Log($"spatialAnchors.Count {spatialAnchors.Count}");
    // }

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
        if (spatialAnchorManager.Session.LocationProvider == null) {
            // setup location provider, enabling geolocation
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
            Debug.Log("Set up Location Provider");
        }

        NearDeviceCriteria criteria = new NearDeviceCriteria();
        criteria.DistanceInMeters = 2000;
        criteria.MaxResultCount = 10;

        anchorLocateCriteria = new AnchorLocateCriteria();

        // anchorLocateCriteria.Identifiers = new string[] { "d9ace388-0e20-4148-a06b-c5520a135a95" };
        anchorLocateCriteria.Identifiers = new string[] {
            "246f7ba6-8854-40e6-b6b2-c2f9b394aec6",
            "ee66b20a-8fbb-42e9-bbb1-e68f5683b363",
            "c3641b09-0585-40af-af4b-735afaab9b33",
            "fbdce9a0-e005-4b84-9184-59751e90974a",
            "17f3901f-29ed-4ca8-907f-4b4f2ff27fdd",
            "ba02780e-2c5d-4146-9113-711599607d20",
            "d1e9a626-3ac2-4de9-93fe-3cc43c5eca49",
        };
        // anchorLocateCriteria.NearDevice = criteria;

        if (spatialAnchorManager.Session.GetActiveWatchers().Count < 1) {
            currentWatcher = CreateWatcher();
        } else {
            //watcher already active
            DisplayFoundAnchors();
        }
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
        cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(1);

        while (!spatialAnchorManager.IsReadyForCreate) {
            anchorCreationProgress.gameObject.SetActive(true);
            await Task.Delay(200);
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            // Debug.Log($"IsReadyForCreate : {spatialAnchorManager.IsReadyForCreate}");
            // Debug.Log($"createProgress: {createProgress:0%}");
            anchorCreationProgress.text = $"Move your device around slowly to capture more data points in the environment.\nProgress: {createProgress:0%}";
        }
        anchorCreationProgress.text = "Anchor sucessfully created!";
        // anchorCreationProgress.gameObject.SetActive(false);

        //TODO: delete active watcher because that interferes with saving
        Debug.Log("Saving anchor to cloud ...");

        await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);
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
        }
    }

    public async Task<CloudSpatialAnchor> getAnchorWithId(string anchorId) {
        Debug.Log($"---- getAnchorWithId {anchorId}");
        // doesnt locate the actual anchor in 3D space
        CloudSpatialAnchor anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
        Debug.Log("found anchor props " + anchor.Identifier);
        return anchor;
    }

    protected CloudSpatialAnchorWatcher CreateWatcher() {
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
                    Vector3 origin = sketchWorldManager.SketchWorld.transform.position;
                    Debug.Log("Cloud Anchor found! Identifier : " + anchor.Identifier);
                    try {
                        Pose anchorPose = anchor.GetPose();
                        float distance = Vector3.Distance(origin, anchorPose.position);
                        if (System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, anchor.Identifier + ".xml"))) {
                            Debug.Log($"+ + + FOUND SKETCH WITH GIVEN ID {anchor.Identifier}!");
                            spatialAnchors.Add(anchor);
                            spatialAnchors.Sort((a, b) => {
                                float dA = Vector3.Distance(origin, a.GetPose().position);
                                float dB = Vector3.Distance(origin, b.GetPose().position);
                                int sort = (int)(dB - dA);
                                return sort;
                            });
                            DisplayFoundAnchors();
                        } else {
                            Debug.Log($"# # # NO SKETCH WITH GIVEN ID {anchor.Identifier} FOUND :( deleting that anchor");
                            // DeleteAnchorWithoutLocating(anchor.Identifier);
                        }
                    } catch (Exception ex) {
                        Debug.LogError(ex);
                    }
                    break;
                }
            case LocateAnchorStatus.AlreadyTracked:
                Debug.Log("LocateAnchorStatus.AlreadyTracked");
                // This anchor has already been reported and is being tracked
                // SpawnNewAnchoredObject(anchor.GetPose().position, anchor.GetPose().rotation, true);
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

    public void StopCurrentWatcher() {
        IReadOnlyList<CloudSpatialAnchorWatcher> watchers = spatialAnchorManager.Session.GetActiveWatchers();
        if (watchers.Count > 0) {
            foreach (var w in watchers) {
                Debug.Log("stopping watcher - " + w.Identifier);
                w.Stop();
            }
        }
    }

    private async void DeleteAnchorWithoutLocating(string anchorId) {
        var anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
        await spatialAnchorManager.Session.DeleteAnchorAsync(anchor);
        Debug.Log($"- - - ANCHOR {anchor.Identifier} DELETED");
    }

    public void DisplayFoundAnchors() {
        //show overlay for anchor list
        CanvasGroup group = foundAnchorsOverlay.GetComponent<CanvasGroup>();
        group.alpha = 1;
        group.blocksRaycasts = true;
        group.interactable = true;

        //remove all previous buttons from list
        foreach (Transform child in listOfFoundAnchors.transform) {
            Destroy(child.gameObject);
        }
        //add button with correct list-order
        for (int i = 0; i < spatialAnchors.Count; i++) {
            Debug.Log(spatialAnchors[i].Identifier);
            GameObject newButton = Instantiate(foundAnchorButtonPrefab);
            newButton.GetComponentInChildren<TMPro.TMP_Text>().text = spatialAnchors[i].Identifier;
            newButton.transform.SetParent(listOfFoundAnchors.transform);
        }
    }
}
