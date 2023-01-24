using UnityEngine;
using UnityEngine.UI;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.SketchObjectManagement;

public class Eraser : MonoBehaviour {
    public static bool IsEnabled = false;
    public DefaultReferences Defaults;
    public SketchWorld SketchWorld;
    private CommandInvoker invoker;
    private GameObject eraserButton;

    void Start() {
        invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
        eraserButton = GameObject.Find("Eraser Button");
    }

    public void Enable() {
        IsEnabled = true;
        eraserButton.GetComponent<Image>().color = Color.red;
    }

    public void Disable() {
        IsEnabled = false;
        eraserButton.GetComponent<Image>().color = Color.white;
    }

    private void DeleteHitObject(SketchObject obj) {
        invoker.ExecuteCommand(new DeleteObjectCommand(obj, SketchWorld));
    }

    void Update() {
        //look for touch input and raycast to hit lineobject
        if (IsEnabled) {
            if (Input.touchCount > 0) {
                Touch currentTouch = Input.GetTouch(0);
                if (Helpers.IsValidTouch(currentTouch) && currentTouch.phase == TouchPhase.Began) {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                    RaycastHit hit;
                    int layerMasks = ~LayerMask.GetMask("Canvas", "Ignore Raycast");

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMasks)) {
                        Transform objectHit = hit.transform;
                        SketchObject line = hit.transform.gameObject.GetComponent<LineSketchObject>();
                        DeleteHitObject(line);
                    }
                }
            }
        }
    }
}
