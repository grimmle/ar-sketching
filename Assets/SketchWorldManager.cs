using UnityEngine;
using VRSketchingGeometry.SketchObjectManagement;

public class SketchWorldManager : MonoBehaviour {
    public SketchWorld SketchWorld;

    public void Load(string anchorId, Vector3 pos, Quaternion rot) {
        var LoadPath = System.IO.Path.Combine(Application.persistentDataPath, anchorId + ".xml");
        Debug.Log($"..... LoadPath: {LoadPath}");
        SketchWorld.LoadSketchWorld(LoadPath);
        SketchWorld.transform.position = pos;
        SketchWorld.transform.rotation = rot;
    }
}
