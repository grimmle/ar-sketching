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
        public GameObject AnchorPlanePrefab = null;
        //anchor in 3D space used for relative mid-air sketching
        private GameObject currentProxyAnchor;
        private Plane currentProxyAnchorPlane;

        //hit point
        private GameObject currentHitpoint;
        //brush at current plane hit point
        private GameObject currentProxyAnchorBrush;
        //null parented to camera movement
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
                                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                                float enter = 0.0f;
                                Camera.transform.parent = null;

                                if (currentProxyAnchorPlane.Raycast(ray, out enter)) {
                                    Vector3 hitPoint = ray.GetPoint(enter);
                                    // Debug.Log($"hit plane at {hitPoint.ToString()}");

                                    //point where the user is starting a new sketch
                                    currentHitpoint = new GameObject();
                                    currentHitpoint.transform.position = hitPoint;

                                    currentLineSketchObject.transform.position = hitPoint;

                                    //null, new "origin" at hitpoint
                                    currentProxyAnchorNull = new GameObject();
                                    currentProxyAnchorNull.transform.position = Camera.transform.position;

                                    //brush from where new lines are drawn
                                    currentProxyAnchorBrush = new GameObject();
                                    currentProxyAnchorBrush.transform.position = currentHitpoint.transform.position;
                                    currentProxyAnchorBrush.transform.SetParent(currentHitpoint.transform);

                                    //visualization for the brush marker relative to proxy anchor
                                    var brushMarkerRelativeSpace = Instantiate(BrushMarker, hitPoint, Quaternion.identity);
                                    brushMarkerRelativeSpace.transform.SetParent(currentProxyAnchorBrush.transform);
                                }
                            }
                        } else if (currentLineSketchObject) {
                            //add new control point according to current device position or active proxy
                            if (isSketchingRelativelyInSpace && currentProxyAnchorBrush != null) {
                                //get camera position relative to hitpoint origin and create sketch there
                                var relativeCameraPos = getRelativePosition(currentProxyAnchorNull.transform, Camera.transform.position);
                                currentProxyAnchorBrush.transform.localPosition = relativeCameraPos;
                                new AddControlPointContinuousCommand(currentLineSketchObject, currentProxyAnchorBrush.transform.position).Execute();
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
                currentProxyAnchor = Instantiate(AnchorPlanePrefab, BrushMarker.transform.position, BrushMarker.transform.rotation);
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
            toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "RELATIVE";

            setProxyAnchorBtn.SetActive(true);
            // freezeBtn.SetActive(true);
            // isFrozen = true;
        }
        public void DisableRelativeSketching() {
            isSketchingRelativelyInSpace = false;
            toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "ABSOLUTE";

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

        public static Vector3 getRelativePosition(Transform origin, Vector3 position) {
            Vector3 distance = position - origin.position;
            Vector3 relativePosition = Vector3.zero;
            relativePosition.x = Vector3.Dot(distance, origin.right.normalized);
            relativePosition.y = Vector3.Dot(distance, origin.up.normalized);
            relativePosition.z = Vector3.Dot(distance, origin.forward.normalized);

            return relativePosition;
        }

    }
}
