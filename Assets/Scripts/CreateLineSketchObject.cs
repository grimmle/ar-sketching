using UnityEngine;
using VRSketchingGeometry.SketchObjectManagement;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;

public class CreateLineSketchObject : MonoBehaviour
{
    public DefaultReferences Defaults;
    private LineSketchObject LineSketchObject;
    private SketchWorld SketchWorld;
    private CommandInvoker Invoker;

    void Start()
    {
        SketchWorld = Instantiate(Defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
        LineSketchObject = Instantiate(Defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
        Invoker = new CommandInvoker();
        Invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(LineSketchObject, SketchWorld));
        Invoker.ExecuteCommand(new AddControlPointCommand(this.LineSketchObject, new Vector3(1, 2, 3)));
        Invoker.ExecuteCommand(new AddControlPointCommand(this.LineSketchObject, new Vector3(1, 4, 2)));
        Invoker.ExecuteCommand(new AddControlPointCommand(this.LineSketchObject, new Vector3(1, 5, 3)));
        Invoker.ExecuteCommand(new AddControlPointCommand(this.LineSketchObject, new Vector3(1, 5, 2)));
        Invoker.Undo();
    }
}