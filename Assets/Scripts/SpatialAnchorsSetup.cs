using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;

public class SpatialAnchorsSetup : MonoBehaviour {
    [SerializeField]
    [Tooltip("SpatialAnchorManager instance to use. Required.")]
    private SpatialAnchorManager spatialAnchorManager;

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

    public async Task SetupCloudSessionAsync([Optional] bool locate) {
        // if (spatialAnchorManager.Session != null) {
        //     await StopCloudSessionAsync();
        // }
        if (spatialAnchorManager.Session == null) {
            anchorLocateCriteria = new AnchorLocateCriteria();
            await spatialAnchorManager.CreateSessionAsync();
            Debug.Log("### SESSION CREATED ###");
            await spatialAnchorManager.StartSessionAsync();
            Debug.Log("### SESSION STARTED ###");
        }
        if (locate) {
            ConfigureSensors();
            StartWatcher();
        }
    }

    public async Task StopCloudSessionAsync() {
        Debug.Log("stopping session");
        StopCurrentWatcher();
        spatialAnchorManager.StopSession();
        currentWatcher = null;
        locationProvider = null;
        spatialAnchors = null;
        CleanupSpawnedObjects();
        await spatialAnchorManager.ResetSessionAsync();
    }

    public void CleanupSpawnedObjects() {
        Destroy(spawnedAnchorObject);
        spawnedAnchorObject = null;
        anchorCreationProgress.text = "";
    }

    public void ConfigureSensors() {
        if (spatialAnchorManager.Session.LocationProvider == null) {
            //setup location provider, enabling geolocation
            locationProvider = new PlatformLocationProvider();
            // Allow GPS
            locationProvider.Sensors.GeoLocationEnabled = true;
            // Allow WiFi scanning (does not work with iOS)
            // locationProvider.Sensors.WifiEnabled = true;
            // Allow a set of known BLE beacons (does not work with iOS)
            // locationProvider.Sensors.BluetoothEnabled = true;
            // locationProvider.Sensors.KnownBeaconProximityUuids = new[]{
            //     "22e38f1a-c1b3-452b-b5ce-fdb0f39535c1",
            //     "a63819b9-8b7b-436d-88ec-ea5d8db2acb0"
            // };

            spatialAnchorManager.Session.LocationProvider = locationProvider;
            Debug.Log("Set up Location Provider");
        }

        NearDeviceCriteria criteria = new NearDeviceCriteria();
        criteria.DistanceInMeters = 1000;
        criteria.MaxResultCount = 25;

        anchorLocateCriteria = new AnchorLocateCriteria();

        //find ids of all local sketches 
        //!!! looking for more than 40 ids seems to throw an unexpected ASA error !!!
        FindLocalSketches();
        //FindNearbyAnchors();
        if (anchorIdsToLocate.Count > 0) {
            Debug.Log($"anchorIdsToLocate.Count: {anchorIdsToLocate.Count}");
            //look for specified anchorIds
            anchorLocateCriteria.Identifiers = anchorIdsToLocate.ToArray();
        } else {
            //look for anchors via geolocalization
            anchorLocateCriteria.NearDevice = criteria;
        }
    }

    private void StartWatcher() {
        Debug.Log($"watchers: {spatialAnchorManager.Session.GetActiveWatchers().Count}");
        if (spatialAnchorManager.Session.GetActiveWatchers().Count < 1) {
            currentWatcher = CreateWatcher();
        } else {
            //watcher already active
            DisplayFoundAnchors();
        }
    }
    private void StopCurrentWatcher() {
        IReadOnlyList<CloudSpatialAnchorWatcher> watchers = spatialAnchorManager.Session.GetActiveWatchers();
        Debug.Log($"active watchers - {watchers.Count}");
        if (watchers.Count > 0) {
            foreach (var w in watchers) {
                Debug.Log($"stopping watcher - {w.Identifier}");
                w.Stop();
            }
            currentWatcher = null;
            anchorIdsToLocate = null;
            spatialAnchors = null;
        }
    }

    public async Task<string> SaveCurrentObjectAnchorToCloudAsync() {
        if (spawnedAnchorObject == null) {
            spawnedAnchorObject = SpawnNewAnchoredObject(new Vector3(), new Quaternion());
        }

        CloudNativeAnchor cloudNativeAnchor = spawnedAnchorObject.GetComponent<CloudNativeAnchor>();
        if (cloudNativeAnchor.CloudAnchor == null) { await cloudNativeAnchor.NativeToCloud(); }

        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
        // cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

        while (!spatialAnchorManager.IsReadyForCreate) {
            anchorCreationProgress.gameObject.SetActive(true);
            await Task.Delay(200);
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            // Debug.Log($"IsReadyForCreate : {spatialAnchorManager.IsReadyForCreate}");
            // Debug.Log($"createProgress: {createProgress:0%}");
            anchorCreationProgress.text = $"Move your device around slowly to capture more data points in the environment.\nProgress: {createProgress:0%}";
        }
        anchorCreationProgress.text = "Saving Anchor...";
        Debug.Log("ASA - Saving anchor to cloud ...");

        await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);
        bool saveSucceeded = cloudSpatialAnchor != null;

        if (!saveSucceeded) {
            Debug.LogError("ASA - Failed to save, but no exception was thrown.");
            return null;
        }
        //add id, so anchor can be found in the same session
        anchorIdsToLocate.Add(cloudSpatialAnchor.Identifier);
        anchorCreationProgress.text = "Anchor sucessfully created!";

        Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
        spawnedAnchorObject.GetComponent<MeshRenderer>().material.color = Color.green;

        //somewhat working
        // StopCurrentWatcher();
        // await StopCloudSessionAsync();

        return cloudSpatialAnchor.Identifier;
    }

    protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, [Optional] bool found) {
        Debug.Log("+++ spawning new game obj");
        //create anchor prefab
        GameObject newGameObject = GameObject.Instantiate(anchoredObjectPrefab, worldPos, worldRot);
        if (found) {
            newGameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        }
        //attach a cloud-native anchor behavior to help keep cloud and native anchors in sync
        newGameObject.AddComponent<CloudNativeAnchor>();
        return newGameObject;
    }

    //doesnt work
    private async Task FindNearbyAnchors() {
        Debug.LogFormat("--- FindNearbyAnchors ---");
        NearDeviceCriteria criteria = new NearDeviceCriteria();
        criteria.DistanceInMeters = 1000;
        criteria.MaxResultCount = 25;

        IList<string> spatialAnchorIds = await spatialAnchorManager.Session.GetNearbyAnchorIdsAsync(criteria);

        Debug.LogFormat($"Got ids for {0} anchors", spatialAnchorIds.Count);

        foreach (string anchorId in spatialAnchorIds) {
            Debug.Log("anchorId " + anchorId);
        }
    }

    private async Task<CloudSpatialAnchor> getAnchorWithId(string anchorId) {
        //doesnt locate the actual anchor in 3D space (contains no pose)
        CloudSpatialAnchor anchor = await spatialAnchorManager.Session.GetAnchorPropertiesAsync(anchorId);
        return anchor;
    }

    protected CloudSpatialAnchorWatcher CreateWatcher() {
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
        CloudSpatialAnchor anchor = e.Anchor;

        switch (status) {
            case LocateAnchorStatus.Located: {
                    Vector3 origin = sketchWorldManager.SketchWorld.transform.position;
                    Debug.Log($"Cloud Anchor found! Identifier: {anchor.Identifier}");
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
                            // DeleteAnchorWithId(anchor.Identifier);
                        }
                    } catch (Exception ex) {
                        Debug.LogError(ex);
                    }
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

    private async void DeleteAnchorWithId(string anchorId) {
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

    private void FindLocalSketches() {
        DirectoryInfo d = new DirectoryInfo(Application.persistentDataPath);
        foreach (var file in d.GetFiles("*.xml")) {
            string fileName = Path.GetFileNameWithoutExtension(file.FullName);
            Debug.Log($"sketch: {fileName}");
            if (anchorIdsToLocate.Count <= 20 && !anchorIdsToLocate.Contains(fileName)) anchorIdsToLocate.Add(fileName);
        }
    }
}
