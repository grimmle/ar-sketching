using UnityEngine;
using UnityEngine.UI;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Selection;
using VRSketchingGeometry.SketchObjectManagement;
using TMPro;

public class Eraser : MonoBehaviour {
    public static bool SelectionActive = false;
    public DefaultReferences Defaults;
    public SketchWorld SketchWorld;
    private CommandInvoker invoker;
    private GameObject eraserButton;
    private GameObject deleteButton;
    private SketchObjectSelection selection;

    void Start() {
        invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
        eraserButton = GameObject.Find("Eraser Button");
        deleteButton = GameObject.Find("Erase Selected Objects");
        deleteButton.SetActive(false);
        selection = Instantiate(Defaults.SketchObjectSelectionPrefab).GetComponent<SketchObjectSelection>();
    }

    public void EnableSelection() {
        SelectionActive = true;
        eraserButton.GetComponent<Image>().color = Color.red;
    }

    public void DisableSelection() {
        SelectionActive = false;
        eraserButton.GetComponent<Image>().color = Color.white;
        new DeactivateSelectionCommand(selection).Execute();
        foreach (SketchObject obj in selection.GetObjectsOfSelection()) {
            RemoveFromSelection(obj);
        }
        deleteButton.SetActive(false);
    }

    private void AddToSelection(SelectableObject obj) {
        new AddToSelectionAndHighlightCommand(selection, obj).Execute();
        // new ActivateSelectionCommand(selection).Execute();
        int count = selection.GetObjectsOfSelection().Count;
        if (count > 0) {
            deleteButton.SetActive(true);
            deleteButton.GetComponentInChildren<TMP_Text>().text = $"Delete {count} Selected";
        }
    }

    private void RemoveFromSelection(SelectableObject obj) {
        new RemoveFromSelectionAndRevertHighlightCommand(selection, obj).Execute();
        int count = selection.GetObjectsOfSelection().Count;
        if (count == 0) {
            deleteButton.SetActive(false);
        } else {
            deleteButton.GetComponentInChildren<TMP_Text>().text = $"Delete {count} Selected";
        }
    }

    public void DeleteSelectedObjects() {
        invoker.ExecuteCommand(new DeleteObjectsOfSelectionCommand(selection));
        DisableSelection();
    }

    void Update() {
        //look for touch input and raycast to hit lineobject
        if (SelectionActive) {
            if (Input.touchCount > 0) {
                Touch currentTouch = Input.GetTouch(0);
                if (Helpers.IsValidTouch(currentTouch) && currentTouch.phase == TouchPhase.Began) {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                    RaycastHit hit;
                    int layerMask = ~LayerMask.GetMask("Ignore Raycast");

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) {
                        Transform objectHit = hit.transform;
                        if (hit.transform.name == "LineSketchObject(Clone)") {
                            SketchObject line = hit.transform.gameObject.GetComponent<LineSketchObject>();
                            if (selection.GetObjectsOfSelection().Contains(line)) {
                                RemoveFromSelection(line);
                            } else {
                                AddToSelection(line);
                            }
                        }
                    }
                }
            }
        }
    }
}
