using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;

public class SketchWorldManager : MonoBehaviour {
    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;

    public void Load(string anchorId) {
        var LoadPath = System.IO.Path.Combine(Application.persistentDataPath, anchorId + ".xml");
        Debug.Log("LoadPath");
        Debug.Log(LoadPath);
        SketchWorld.LoadSketchWorld(LoadPath);
    }
}
