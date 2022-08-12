//some credit for basic sketching implementation using VRSketchingGeometry to:
//https://github.com/tterpi/Sketchar

namespace Sketching {
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using UnityEngine.EventSystems;
    using UnityEngine.XR.ARFoundation;
    using System.Collections.Generic;
    using VRSketchingGeometry.SketchObjectManagement;
    using VRSketchingGeometry.Commands;
    using VRSketchingGeometry.Commands.Line;

    public class TouchAndHoldToSketch : MonoBehaviour {
        public Camera Camera;
        public SketchWorld SketchWorld;
        public GameObject SketchObjectPrefab;

        //color picker to extract current color
        public ColorPicker ColorPicker;
        //indicator for where new objects are drawn from
        public GameObject BrushMarker;

        [Tooltip("Visualization of the anchor plane.")]
        public GameObject anchorPlane = null;
        //anchor in 3D space used for relative mid-air sketching
        private GameObject currentProxyAnchor;
        private Plane currentProxyAnchorPlane;
        private GameObject currentProxyAnchorNull;
        private bool isSketchingRelativelyInSpace = false;
        private bool isFrozen = false;

        //BUTTONS
        private GameObject toggleSpaceBtn;
        private GameObject setProxyAnchorBtn;
        private GameObject freezeBtn;

        public static float lineDiameter = 0.02f;

        private ARAnchor worldAnchor;
        private LineSketchObject currentLineSketchObject;
        private CommandInvoker invoker;

        private bool isValidTouch = false;
        private bool startNewSketchObject = false;

        //needed for new input system
        private PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

        Color purple = new Color(0.66f, 0.12f, 0.96f);

        public void Start() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            BrushMarker.transform.SetParent(Camera.transform);
            BrushMarker.transform.localPosition = Vector3.forward * 0.3f;
            invoker = new CommandInvoker();

            toggleSpaceBtn = GameObject.Find("Toggle Sketching Space");
            setProxyAnchorBtn = GameObject.Find("Set Proxy Anchor");
            setProxyAnchorBtn.SetActive(false);
            freezeBtn = GameObject.Find("Move Freely");
            freezeBtn.SetActive(false);
        }

        public void Update() {
            if (currentProxyAnchor) {
                currentProxyAnchor.transform.LookAt(Camera.transform.position);
                currentProxyAnchorPlane.SetNormalAndPosition(currentProxyAnchor.transform.forward, currentProxyAnchor.transform.position);
            }

            if (Input.touchCount > 0) {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began) {
                    isValidTouch = IsValidTouch(currentTouch);
                }

                if (isValidTouch) {
                    if (currentTouch.phase == TouchPhase.Began) {
                        startNewSketchObject = true;
                    } else if (currentTouch.phase == TouchPhase.Stationary || (currentTouch.phase == TouchPhase.Moved && startNewSketchObject == false && currentLineSketchObject.getNumberOfControlPoints() > 0)) {
                        if (startNewSketchObject) {
                            //create a new sketch object
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;

                            //if sketching relatively, raycast from viewport center to anchorPlane, set anchorNull and start drawing from that hitpoint
                            if (isSketchingRelativelyInSpace && currentProxyAnchor != null) {
                                // Ray ray = Camera.ScreenPointToRay(BrushMarker.transform.position);
                                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                                // float enter = currentProxyAnchorPlane.GetDistanceToPoint(Camera.transform.position);
                                float enter = 0.0f;

                                if (currentProxyAnchorPlane.Raycast(ray, out enter)) {
                                    Vector3 hitPoint = ray.GetPoint(enter);
                                    Debug.Log($"hit plane at {hitPoint.ToString()}");

                                    var x = Instantiate(BrushMarker, hitPoint, Quaternion.identity);

                                    currentLineSketchObject.transform.position = hitPoint;
                                    currentLineSketchObject.transform.SetParent(currentProxyAnchor.transform);

                                    currentProxyAnchorNull = new GameObject();
                                    currentProxyAnchorNull.transform.position = hitPoint;
                                    currentProxyAnchorNull.transform.SetParent(Camera.transform);
                                    // isFrozen = false;
                                    // freezeBtn.SetActive(false);
                                }
                            }

                            // if (currentProxyAnchorNull == null || !isFrozen) {
                            //     currentProxyAnchorNull.transform.position = currentProxyAnchor.transform.position;
                            //     Debug.Log($"PROXY NULL MOVED BACK TO ORIGINAL ANCHOR POSIITON {currentProxyAnchorNull.transform.position.ToString()}");
                            // }
                        } else if (currentLineSketchObject) {
                            //add new control point according to current device position or active proxy
                            if (isSketchingRelativelyInSpace && currentProxyAnchorNull != null) {
                                // currentProxyAnchor.transform.rotation = Camera.transform.rotation;
                                // Debug.Log($"currentProxyAnchorNull {currentProxyAnchorNull.transform.position.ToString()}");
                                new AddControlPointContinuousCommand(currentLineSketchObject, currentProxyAnchorNull.transform.position).Execute();
                            } else {
                                new AddControlPointContinuousCommand(currentLineSketchObject, BrushMarker.transform.position).Execute();
                            }
                        }
                    } else if (currentTouch.phase == TouchPhase.Ended) {
                        //delete sketch object if empty
                        if (startNewSketchObject == false && currentLineSketchObject.getNumberOfControlPoints() < 1) {
                            Destroy(currentLineSketchObject.gameObject);
                            currentLineSketchObject = null;
                        }

                        PostProcessSketchObject();
                        isValidTouch = false;
                    }
                }
            }
        }

        private bool IsValidTouch(Touch currentTouch) {
            //ignore touch if it is on UI or the AR session is not tracking the environment
            var hits = new List<RaycastResult>();
            pointerEventData.position = currentTouch.position;
            EventSystem.current.RaycastAll(pointerEventData, hits);
            if (ARSession.state != ARSessionState.SessionTracking || EventSystem.current.IsPointerOverGameObject(currentTouch.fingerId) || hits.Count > 0) {
                // Debug.Log("Invalid Touch!");
                return false;
            }
            return true;
        }

        private void CreateNewLineSketchObject() {
            if (!worldAnchor) {
                //create world anchor
                GameObject anchor = new GameObject();
                anchor.name = "WorldAnchor";
                worldAnchor = anchor.AddComponent<ARAnchor>();
                SketchWorld.transform.SetParent(worldAnchor.transform);
            }

            //instantiate sketch object and set configuration
            var gameObject = Instantiate(SketchObjectPrefab);
            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material.color = ColorPicker.color;

            currentLineSketchObject = gameObject.GetComponent<LineSketchObject>();
            currentLineSketchObject.minimumControlPointDistance = .02f;
            currentLineSketchObject.SetLineDiameter(lineDiameter);
            currentLineSketchObject.SetInterpolationSteps(5);
        }

        //refines the latest sketch object
        private void PostProcessSketchObject() {
            if (currentLineSketchObject != null && currentLineSketchObject.gameObject != null) {
                invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(currentLineSketchObject, SketchWorld));
                if (currentLineSketchObject.getNumberOfControlPoints() > 2) {
                    new RefineMeshCommand(currentLineSketchObject).Execute();
                }
            }
        }

        public void DeleteLastLineSketchObject() {
            invoker.Undo();
        }

        public void RestoreLastDeletedSketchObject() {
            invoker.Redo();
        }

        public void SetAnchorProxy() {
            // set proxy anchor to current BrushMarker position
            if (currentProxyAnchor == null) {
                currentProxyAnchor = Instantiate(anchorPlane, BrushMarker.transform.position, BrushMarker.transform.rotation);
                currentProxyAnchorPlane = new Plane(-Camera.transform.forward, currentProxyAnchor.transform.position);
                Debug.Log($"PROXY ANCHOR CREATED AT {currentProxyAnchor.transform.position.ToString()}");
            } else {
                currentProxyAnchor.transform.position = BrushMarker.transform.position;
                Debug.Log($"PROXY ANCHOR MOVED TO {BrushMarker.transform.position.ToString()}");
            }
            isSketchingRelativelyInSpace = true;
        }

        public bool IsSketchingRelativelyInSpace() {
            return isSketchingRelativelyInSpace;
        }
        public void EnableRelativeSketching() {
            isSketchingRelativelyInSpace = true;

            if (currentProxyAnchor == null) SetAnchorProxy();
            // BrushMarker.SetActive(false);

            toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "RELATIVE";
            toggleSpaceBtn.GetComponent<Image>().color = purple;

            setProxyAnchorBtn.SetActive(true);
            // freezeBtn.SetActive(true);
            // isFrozen = true;
        }
        public void DisableRelativeSketching() {
            isSketchingRelativelyInSpace = false;
            BrushMarker.SetActive(true);

            toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "ABSOLUTE";
            toggleSpaceBtn.GetComponent<Image>().color = Color.white;

            setProxyAnchorBtn.SetActive(false);
            freezeBtn.SetActive(false);
            isFrozen = false;
        }

        public void ToggleMoveFreely() {
            if (isFrozen) {
                freezeBtn.GetComponent<Image>().color = Color.white;
            } else {
                freezeBtn.GetComponent<Image>().color = purple;
            }
            isFrozen = !isFrozen;
        }

    }
}
