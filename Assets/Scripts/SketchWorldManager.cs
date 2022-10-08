using UnityEngine;
using VRSketchingGeometry.SketchObjectManagement;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;

public class SketchWorldManager : MonoBehaviour {
    public SketchWorld SketchWorld;

    public void Load(CloudSpatialAnchor anchor) {
        var LoadPath = System.IO.Path.Combine(Application.persistentDataPath, anchor.Identifier + ".xml");
        Debug.Log($"..... LoadPath: {LoadPath}");
        SketchWorld.LoadSketchWorld(LoadPath);
        SketchWorld.transform.position = anchor.GetPose().position;
        SketchWorld.transform.rotation = anchor.GetPose().rotation;
    }

    public void Load(string anchorId, Vector3 pos, Quaternion rot) {
        var LoadPath = System.IO.Path.Combine(Application.persistentDataPath, anchorId + ".xml");
        Debug.Log($"..... LoadPath: {LoadPath}");
        SketchWorld.LoadSketchWorld(LoadPath);
        SketchWorld.transform.position = pos;
        SketchWorld.transform.rotation = rot;
    }
}
