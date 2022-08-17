using System.IO;
using UnityEngine;
using Sketching;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;

public class ButtonHandler : MonoBehaviour {
    TouchAndHoldToSketch touchScript;
    SpatialAnchorsSetup asaScript;
    [SerializeField] GameObject Main;
    // [SerializeField] GameObject ASA;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;
    private string SavePath;


    void Awake() {
        touchScript = GameObject.Find("Main").GetComponent<TouchAndHoldToSketch>();
        asaScript = GameObject.Find("AzureSpatialAnchors").GetComponent<SpatialAnchorsSetup>();
    }
    public async void Save() {
        // SketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();

        Debug.Log("ButtonHandler SaveAnchorToCloudAsync");
        Debug.Log(asaScript.ToString());
        await asaScript.SaveCurrentObjectAnchorToCloudAsync();

        //Serialize the SketchWorld to a XML file
        // SavePath = System.IO.Path.Combine(Application.persistentDataPath, "Sketch-" + System.DateTime.UtcNow.Year + "-" + System.DateTime.UtcNow.Month + "-" + System.DateTime.UtcNow.Day + "-" + System.DateTime.UtcNow.Minute + "-" + System.DateTime.UtcNow.Second + ".xml");
        /*SavePath = System.IO.Path.Combine(Application.persistentDataPath, asaScript.currentAnchorIdToSave + ".xml");*/
        // SketchWorld.SetAnchorId(asaScript.currentAnchorIdToSave);
        SketchWorld.SaveSketchWorld(SavePath);

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

    public void LookForNearbySketches() {
        // start asa cloud session and start looking for nearby anchors
        asaScript.SetupCloudSessionAsync();
    }

    public void SetLineDiameter(float diameter) {
        TouchAndHoldToSketch.lineDiameter = diameter;
    }

    public void Undo() {
        touchScript.DeleteLastLineSketchObject();
    }

    public void Redo() {
        touchScript.RestoreLastDeletedSketchObject();
    }

    public void Clear() {
        // foreach (var obj in Resources.FindObjectsOfTypeAll(typeof(LineSketchObject)))
        // {
        //     Debug.Log(obj.name);
        //     Destroy(obj);
        // }
        // Destroy(SketchWorld);
        SketchWorld.ActiveSketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
    }

    public void StartASASession() {
        Debug.Log("StartASASession");
        asaScript.SetupCloudSessionAsync();
    }

    public void SaveAnchor() {
        Debug.Log("SaveAnchorToCloudAsync");
        asaScript.SaveAnchorToCloudAsync();
    }

    public void StopASASession() {
        Debug.Log("StopCloudSessionAsync");
        asaScript.StopCloudSessionAsync();
    }

    public void QueryAnchors() {
        Debug.Log("FindNearbyAnchors");
        // asaScript.FindNearbyAnchors();
    }

    public void SetAnchorProxyForRelativeSketching() {
        touchScript.SetAnchorProxy();
    }

    public void ToggleSketchingSpace() {
        if (touchScript.IsSketchingRelativelyInSpace()) {
            touchScript.DisableRelativeSketching();
        } else {
            touchScript.EnableRelativeSketching();
        }
    }

    public void ToggleMoveFreely() {
        touchScript.ToggleMoveFreely();
    }

}
