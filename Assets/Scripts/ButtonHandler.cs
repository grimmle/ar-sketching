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

    private GameObject UI;

    public SketchWorld SketchWorld;
    public DefaultReferences Defaults;

    private SketchWorldManager sketchWorldManager;
    private CommandInvoker invoker;

    private string savePath;

    public GameObject resetSceneButton;

    void Start() {
        TouchAndHoldToSketchScript = GameObject.Find("Main").GetComponent<TouchToSketch>();
        ColorMenuScript = GameObject.Find("Main").GetComponent<ColorMenu>();
        DiameterMenuScript = GameObject.Find("Main").GetComponent<DiameterMenu>();
        EraserScript = GameObject.Find("Main").GetComponent<Eraser>();
        invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
        sketchWorldManager = GameObject.Find("Main").GetComponent<SketchWorldManager>();
        UI = GameObject.Find("UI");
        if (resetSceneButton) resetSceneButton.SetActive(false);
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
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().alpha = 1;
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().interactable = true;
        resetSceneButton.SetActive(false);
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

    public void OpenHelpMenu() {
        //hide other UI
        UI.transform.Find("Other").GetComponent<CanvasGroup>().alpha = .3f;
        UI.transform.Find("Other").GetComponent<CanvasGroup>().interactable = false;
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().alpha = .3f;
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().interactable = false;
        //show help menu
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().interactable = true;
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().blocksRaycasts = true;
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().alpha = 1;
    }
    public void CloseHelpMenu() {
        //show other UI
        UI.transform.Find("Other").GetComponent<CanvasGroup>().alpha = 1;
        UI.transform.Find("Other").GetComponent<CanvasGroup>().interactable = true;
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().alpha = 1;
        UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().interactable = true;
        //close help menu
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().interactable = false;
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().blocksRaycasts = false;
        UI.transform.Find("Help Center").GetComponent<CanvasGroup>().alpha = 0;
    }

    public void ToggleEraser() {
        if (Eraser.IsEnabled) {
            EraserScript.Disable();
            UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().alpha = 1;
            UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().interactable = true;
            resetSceneButton.SetActive(false);
        } else {
            EraserScript.Enable();
            UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().alpha = .3f;
            UI.transform.Find("Non-Destructive").GetComponent<CanvasGroup>().interactable = false;
            resetSceneButton.SetActive(true);
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
