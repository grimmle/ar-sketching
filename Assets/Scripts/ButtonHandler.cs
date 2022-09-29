using System.IO;
using UnityEngine;
using Sketching;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;

public class ButtonHandler : MonoBehaviour {
    TouchAndHoldToSketch TouchAndHoldToSketchScript;
    SpatialAnchorsSetup SpatialAnchorsSetup;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;

    private SketchWorldManager sketchWorldManager;
    private GameObject foundAnchorsOverlay;

    private string savePath;

    void Awake() {
        TouchAndHoldToSketchScript = GameObject.Find("Main").GetComponent<TouchAndHoldToSketch>();
        SpatialAnchorsSetup = GameObject.Find("AzureSpatialAnchors").GetComponent<SpatialAnchorsSetup>();
        sketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
        foundAnchorsOverlay = GameObject.Find("Located Sketches List");
    }
    public async void Save() {
        await SpatialAnchorsSetup.SetupCloudSessionAsync();
        var anchorId = await SpatialAnchorsSetup.SaveCurrentObjectAnchorToCloudAsync();

        //Serialize the SketchWorld to a XML file
        //SavePath = System.IO.Path.Combine(Application.persistentDataPath, "Sketch-" + System.DateTime.UtcNow.Year + "-" + System.DateTime.UtcNow.Month + "-" + System.DateTime.UtcNow.Day + "-" + System.DateTime.UtcNow.Minute + "-" + System.DateTime.UtcNow.Second + ".xml");
        savePath = System.IO.Path.Combine(Application.persistentDataPath, anchorId + ".xml");
        SketchWorld.SaveSketchWorld(savePath);

        //Export the SketchWorld as an OBJ file
        // SketchWorld.ExportSketchWorldToDefaultPath();
    }

    /*public async void Load(string anchorId) {
        //Create another SketchWorld and load the serialized SketchWorld
        // DeserializedSketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
        DirectoryInfo d = new DirectoryInfo(Application.persistentDataPath);
        string fileName = "";
        foreach (var file in d.GetFiles("*.xml")) {
            Debug.Log("file.Name");
            fileName = Path.GetFileNameWithoutExtension(file.FullName);
            Debug.Log(fileName);
            Debug.Log(file.DirectoryName);
            SavePath = System.IO.Path.Combine(Application.persistentDataPath, file.Name);
        }
        string id = fileName;
        CloudSpatialAnchor anchor = await asaScript.getAnchorWithId(id);
        Debug.Log($"returned anchor {anchor.Identifier}");
        foreach (var kvp in anchor.AppProperties) {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
        }

        asaScript.SetAnchorIdsToLocate(new string[] { id });
        SketchWorld.LoadSketchWorld(SavePath);
    }*/

    public async void LookForNearbySketches() {
        // start asa cloud session and start looking for nearby anchors
        Debug.Log("LookForNearbySketches");
        await SpatialAnchorsSetup.SetupCloudSessionAsync();
        SpatialAnchorsSetup.ConfigureSensors();
    }

    public void LoadSketchWithIndex() {
        int index = this.transform.GetSiblingIndex();
        Debug.Log("index:" + index);
        CloudSpatialAnchor anchor = SpatialAnchorsSetup.spatialAnchors[index];
        Debug.Log("load anchor with id: " + anchor.Identifier);
        sketchWorldManager.Load(SpatialAnchorsSetup.spatialAnchors[index]);

        CanvasGroup group = foundAnchorsOverlay.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void SetLineDiameter(float diameter) {
        TouchAndHoldToSketch.lineDiameter = diameter;
    }

    public void Undo() {
        TouchAndHoldToSketchScript.DeleteLastLineSketchObject();
    }

    public void Redo() {
        TouchAndHoldToSketchScript.RestoreLastDeletedSketchObject();
    }

    public void Clear() {
        TouchAndHoldToSketchScript.ClearSketchWorld();
    }

    public void SetAnchorProxyForRelativeSketching() {
        TouchAndHoldToSketchScript.SetAnchorProxy();
    }

    public void ToggleAirSketchingSpace() {
        TouchAndHoldToSketchScript.ToggleAirSketchingSpace();
    }

    public void ToggleSketchingMode() {
        TouchAndHoldToSketchScript.ToggleSketchingMode();
    }

    public void CloseFoundSketchesList() {
        // SpatialAnchorsSetup.StopCurrentWatcher();
        CanvasGroup group = foundAnchorsOverlay.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }
}
