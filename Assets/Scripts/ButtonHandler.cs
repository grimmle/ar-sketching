using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.SketchObjectManagement;
using Sketching;
using System.Threading.Tasks;

public class ButtonHandler : MonoBehaviour
{
    TouchAndHoldToSketch touchScript;
    SpatialAnchorsSetup asaScript;
    [SerializeField] GameObject Main;
    [SerializeField] GameObject ASA;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;
    private string SavePath;


    void Awake()
    {
        touchScript = Main.GetComponent<TouchAndHoldToSketch>();
        asaScript = ASA.GetComponent<SpatialAnchorsSetup>();
    }
    public void Save()
    {
        // SketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();

        //Serialize the SketchWorld to a XML file
        SavePath = System.IO.Path.Combine(Application.persistentDataPath, "Sketch-" + System.DateTime.UtcNow.Month + "-" + System.DateTime.UtcNow.Day + "-" + System.DateTime.UtcNow.Minute + "-" + System.DateTime.UtcNow.Second + ".xml");
        SketchWorld.SaveSketchWorld(SavePath);


        //Export the SketchWorld as an OBJ file
        // SketchWorld.ExportSketchWorldToDefaultPath();
    }

    public void Load()
    {
        //Create another SketchWorld and load the serialized SketchWorld
        // DeserializedSketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
        DirectoryInfo d = new DirectoryInfo(Application.persistentDataPath);
        foreach (var file in d.GetFiles("*.xml"))
        {
            Debug.Log("#######################");
            Debug.Log("file.Name");
            Debug.Log(file.Name);
            Debug.Log("file.DirectoryName");
            Debug.Log(file.DirectoryName);
            SavePath = System.IO.Path.Combine(Application.persistentDataPath, file.Name);
            // SavePath = file.DirectoryName;
        }
        SketchWorld.LoadSketchWorld(SavePath);
        // DeserializedSketchWorld.transform.position += new Vector3(5, 0, 0);

    }

    public void SetLineDiameter(float diameter)
    {
        TouchAndHoldToSketch.lineDiameter = diameter;
    }

    public void Undo()
    {
        touchScript.DeleteLastLineSketchObject();
    }

    public void Redo()
    {
        touchScript.RestoreLastDeletedSketchObject();
    }

    public void Clear()
    {
        // foreach (var obj in Resources.FindObjectsOfTypeAll(typeof(LineSketchObject)))
        // {
        //     Debug.Log(obj.name);
        //     Destroy(obj);
        // }
        // Destroy(SketchWorld);
        SketchWorld.ActiveSketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
    }

    public void StartASASession()
    {
        Debug.Log("StartASASession");
        asaScript.SetupCloudSessionAsync();
    }

    public void SaveAnchor()
    {
        Debug.Log("SaveAnchorToCloudAsync");
        asaScript.SaveAnchorToCloudAsync();
    }

    public void StopASASession()
    {
        Debug.Log("StopCloudSessionAsync");
        asaScript.StopCloudSessionAsync();
    }

    public void QueryAnchors()
    {
        Debug.Log("FindNearbyAnchors");
        // asaScript.FindNearbyAnchors();
    }

}
