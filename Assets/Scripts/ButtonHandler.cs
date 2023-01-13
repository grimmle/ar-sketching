using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sketching;
using VRSketchingGeometry;
using VRSketchingGeometry.SketchObjectManagement;
using VRSketchingGeometry.Commands;

public class ButtonHandler : MonoBehaviour {
    TouchToSketch TouchAndHoldToSketchScript;
    ColorMenu ColorMenuScript;
    DiameterMenu DiameterMenuScript;
    Eraser EraserScript;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;

    private SketchWorldManager sketchWorldManager;
    private CommandInvoker invoker;

    private string savePath;

    void Start() {
        TouchAndHoldToSketchScript = GameObject.Find("Main").GetComponent<TouchToSketch>();
        ColorMenuScript = GameObject.Find("Main").GetComponent<ColorMenu>();
        DiameterMenuScript = GameObject.Find("Main").GetComponent<DiameterMenu>();
        EraserScript = GameObject.Find("Main").GetComponent<Eraser>();
        invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;

        sketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
    }

    public void Save() {
        //export the SketchWorld as an OBJ file
        SketchWorld.ExportSketchWorldToDefaultPath();
    }

    public void Undo() {
        invoker.Undo();
    }
    public void Redo() {
        invoker.Redo();
    }
    public void Clear() {
        TouchAndHoldToSketchScript.ClearSketchWorld();
    }

    public void ToggleMarker() {
        TouchAndHoldToSketchScript.ToggleMarker();
    }
    public void ToggleCanvas() {
        TouchAndHoldToSketchScript.ToggleCanvas();
    }
    public void ToggleConnect() {
        TouchAndHoldToSketchScript.ToggleConnect();
    }
    public void SetCanvas() {
        TouchAndHoldToSketchScript.SetCanvas();
    }

    public void ToggleEraser() {
        if (Eraser.IsEnabled) {
            EraserScript.Disable();
        } else {
            EraserScript.Enable();
        }
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
