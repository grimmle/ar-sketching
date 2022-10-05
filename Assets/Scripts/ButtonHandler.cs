using UnityEngine;
using Sketching;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;
using Microsoft.Azure.SpatialAnchors;
using UnityEngine.UI;
using TMPro;

public class ButtonHandler : MonoBehaviour {
    TouchAndHoldToSketch TouchAndHoldToSketchScript;
    SpatialAnchorsSetup SpatialAnchorsSetup;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;

    private SketchWorldManager sketchWorldManager;
    private GameObject foundAnchorsOverlay;

    private ColorMenu ColorMenuScript;
    private DiameterMenu DiameterMenuScript;

    private GameObject diameterMenu;
    private bool diameterMenuIsOpen;

    private string savePath;

    void Awake() {
        TouchAndHoldToSketchScript = GameObject.Find("Main").GetComponent<TouchAndHoldToSketch>();
        ColorMenuScript = GameObject.Find("Main").GetComponent<ColorMenu>();
        DiameterMenuScript = GameObject.Find("Main").GetComponent<DiameterMenu>();

        SpatialAnchorsSetup = GameObject.Find("AzureSpatialAnchors").GetComponent<SpatialAnchorsSetup>();
        sketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
        foundAnchorsOverlay = GameObject.Find("Located Sketches List");
    }

    public async void Save() {
        await SpatialAnchorsSetup.SetupCloudSessionAsync();
        var anchorId = await SpatialAnchorsSetup.SaveCurrentObjectAnchorToCloudAsync();

        //serialize the SketchWorld to a XML file
        savePath = System.IO.Path.Combine(Application.persistentDataPath, anchorId + ".xml");
        SketchWorld.SaveSketchWorld(savePath);

        //export the SketchWorld as an OBJ file
        // SketchWorld.ExportSketchWorldToDefaultPath();
    }

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

    public void Undo() {
        TouchAndHoldToSketchScript.DeleteLastLineSketchObject();
    }

    public void Redo() {
        TouchAndHoldToSketchScript.RestoreLastDeletedSketchObject();
    }

    public void Clear() {
        TouchAndHoldToSketchScript.ClearSketchWorld();
        SpatialAnchorsSetup.CleanupSpawnedObjects();
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

    public void ToggleColorMenu() {
        if (ColorMenu.IsOpen) {
            ColorMenuScript.Close();
        } else {
            ColorMenuScript.Open();
        }
    }

    public void ChooseColor() {
        Color color = this.transform.Find("Mask").transform.Find("Color").GetComponent<Image>().color;
        ColorMenuScript.SetColor(color);
    }

    public void ToggleDiameterMenu() {
        if (DiameterMenu.IsOpen) {
            DiameterMenuScript.Close();
        } else {
            DiameterMenuScript.Open();
        }
    }

    public void ChooseDiameter() {
        string diameter = this.transform.Find("Diameter").GetComponent<TMP_Text>().text;
        DiameterMenuScript.SetDiameter(diameter);
    }
}
