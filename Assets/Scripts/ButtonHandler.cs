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
    public DefaultReferences Defaults;
    private string SavePath;


    void Awake()
    {
        touchScript = Main.GetComponent<TouchAndHoldToSketch>();
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
        // Debug.Log(DeserializedSketchWorld.ToString());
        // Debug.Log(SketchWorld.ToString());
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

}
