using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.SketchObjectManagement;
using Sketching;

public class ButtonHandler : MonoBehaviour
{
    TouchAndHoldToSketch touchScript;
    [SerializeField] GameObject Main;

    public SketchWorld SketchWorld;
    // public SketchWorld DeserializedSketchWorld;
    public DefaultReferences Defaults;
    private string SavePath;


    void Awake()
    {
        touchScript = Main.GetComponent<TouchAndHoldToSketch>();
    }
    public void Save()
    {
        SketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();

        //Serialize the SketchWorld to a XML file
        SavePath = System.IO.Path.Combine(Application.persistentDataPath, "YourSketch.xml");
        SketchWorld.SaveSketchWorld(SavePath);

        //Create another SketchWorld and load the serialized SketchWorld
        // DeserializedSketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
        // DeserializedSketchWorld.LoadSketchWorld(SavePath);
        // DeserializedSketchWorld.transform.position += new Vector3(5, 0, 0);

        //Export the SketchWorld as an OBJ file
        // SketchWorld.ExportSketchWorldToDefaultPath();
    }

    public void Undo()
    {
        touchScript.DeleteLastLineSketchObject();
    }

    public void Redo()
    {
        touchScript.RestoreLastDeletedSketchObject();
    }

}
