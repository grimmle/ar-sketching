//some credit for basic sketching implementation using VRSketchingGeometry to:
//https://github.com/tterpi/Sketchar

namespace Sketching {
    using UnityEngine;
    using TMPro;
    using UnityEngine.EventSystems;
    using UnityEngine.XR.ARFoundation;
    using System.Collections.Generic;
    using VRSketchingGeometry.SketchObjectManagement;
    using VRSketchingGeometry.Commands;
    using VRSketchingGeometry.Commands.Line;
    using VRSketchingGeometry.Serialization;
    using VRSketchingGeometry;

    public enum DrawingMode { Screen, Air }

    public class TouchAndHoldToSketch : MonoBehaviour {
        public Camera Camera;
        public SketchWorld SketchWorld;
        public GameObject SketchObjectPrefab;
        public DefaultReferences defaults;

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
        //for relative air drawing, keep relative position in between touch inputs
        private bool continueOnSameAnchor;
        //drawing relative to the actual device position
        private bool isSketchingRelatively = false;

        //BUTTONS
        private GameObject toggleSpaceBtn;
        private GameObject setProxyAnchorBtn;
        private GameObject toggleModeBtn;

        private ARAnchor worldAnchor;
        private LineSketchObject currentLineSketchObject;
        private CommandInvoker invoker;

        private bool isValidTouch = false;
        private bool startNewSketchObject = false;

        //needed for new input system
        private PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

        private DrawingMode drawingMode = DrawingMode.Air;

        public void Start() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            BrushMarker.transform.SetParent(Camera.transform);
            BrushMarker.transform.localPosition = Vector3.forward * 0.3f;
            invoker = new CommandInvoker();

            toggleSpaceBtn = GameObject.Find("Toggle Sketching Space");
            toggleModeBtn = GameObject.Find("Toggle Sketching Mode");
            setProxyAnchorBtn = GameObject.Find("Set Proxy Anchor");
            setProxyAnchorBtn.SetActive(false);
        }

        public void Update() {
            // if (currentProxyAnchor) {
            //     // update proxy anchor plane so it always faces the camera
            //     currentProxyAnchor.transform.LookAt(Camera.transform.position);
            //     currentProxyAnchorPlane.SetNormalAndPosition(currentProxyAnchor.transform.forward, currentProxyAnchor.transform.position);
            // }
            if (currentProxyAnchorBrush && currentProxyAnchorNull) {
                //get camera position relative to hitpoint origin
                var relativeCameraPos = getRelativePosition(currentProxyAnchorNull.transform, Camera.transform.position);
                currentProxyAnchorBrush.transform.localPosition = relativeCameraPos;
            }

            if (Input.touchCount > 0) {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began) {
                    isValidTouch = IsValidTouch(currentTouch);
                }

                if (isValidTouch) {
                    if (currentTouch.phase == TouchPhase.Began) {
                        startNewSketchObject = true;
                    } else if (drawingMode == DrawingMode.Air && (currentTouch.phase == TouchPhase.Stationary || currentTouch.phase == TouchPhase.Moved)) {
                        if (startNewSketchObject) {
                            //create a new sketch object
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;

                            //if sketching relatively, raycast from viewport center to anchorPlane, set anchorNull and start drawing from that hitpoint
                            if (isSketchingRelatively && currentProxyAnchor != null && !continueOnSameAnchor) {
                                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                                float enter = 0.0f;
                                Camera.transform.parent = null;

                                if (currentProxyAnchorPlane.Raycast(ray, out enter)) {
                                    Vector3 hitPoint = ray.GetPoint(enter);
                                    //point where the user is starting a new sketch
                                    currentHitpoint = new GameObject();
                                    currentHitpoint.transform.position = hitPoint;

                                    currentLineSketchObject.transform.position = hitPoint;

                                    //null, new "origin" at hitpoint
                                    currentProxyAnchorNull = new GameObject();
                                    currentProxyAnchorNull.transform.position = Camera.transform.position;

                                    //brush from where new lines are drawn
                                    currentProxyAnchorBrush = Instantiate(BrushMarker, hitPoint, Quaternion.identity);
                                    currentProxyAnchorBrush.transform.SetParent(currentHitpoint.transform);
                                    continueOnSameAnchor = true;
                                    BrushMarker.SetActive(false);
                                }
                            }
                        } else if (currentLineSketchObject) {
                            //add new control point according to current device position or active proxy
                            if (isSketchingRelatively && currentProxyAnchorBrush != null) {
                                //create sketch from relative brush position
                                new AddControlPointContinuousCommand(currentLineSketchObject, currentProxyAnchorBrush.transform.position).Execute();
                            } else {
                                new AddControlPointContinuousCommand(currentLineSketchObject, BrushMarker.transform.position).Execute();
                            }
                        }
                    } else if (drawingMode == DrawingMode.Screen && (currentTouch.phase == TouchPhase.Stationary || currentTouch.phase == TouchPhase.Moved)) {
                        if (startNewSketchObject) {
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;
                        } else if (currentLineSketchObject) {
                            //if sketching relatively, raycast from screen touchPoint to anchorPlane
                            if (isSketchingRelatively && currentProxyAnchor != null) {
                                Ray ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                                float enter = 0.0f;

                                if (currentProxyAnchorPlane.Raycast(ray, out enter)) {
                                    Vector3 hitPoint = ray.GetPoint(enter);
                                    new AddControlPointContinuousCommand(currentLineSketchObject, hitPoint).Execute();
                                }
                            } else {
                                //draw at current absolute touch position
                                var touchPos = Camera.main.ScreenToWorldPoint(new Vector3(currentTouch.position.x, currentTouch.position.y, 0.3f));
                                new AddControlPointContinuousCommand(currentLineSketchObject, touchPos).Execute();
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
            var newMat = renderer.sharedMaterial;

            // set up brush with current color and diameter option
            newMat.color = ColorMenu.CurrentColor;
            renderer.material.color = ColorMenu.CurrentColor;
            currentLineSketchObject = gameObject.GetComponent<LineSketchObject>();
            currentLineSketchObject.minimumControlPointDistance = .01f;
            LineBrush brush = currentLineSketchObject.GetBrush() as LineBrush;
            brush.SketchMaterial = new SketchMaterialData(newMat);
            brush.CrossSectionScale = DiameterMenu.CurrentDiameter;
            brush.InterpolationSteps = 5;
            currentLineSketchObject.SetBrush(brush);
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

        public void ClearSketchWorld() {
            foreach (Transform child in SketchWorld.transform.Find("RootSketchObjectGroup").transform) {
                Destroy(child.gameObject);
            }
            //reset SketchWorld transform
            SketchWorld.transform.position = new Vector3(0, 0, 0);
            SketchWorld.transform.rotation = new Quaternion();
        }

        private void ResetBrushmarker() {
            Destroy(currentProxyAnchorBrush);
            if (drawingMode == DrawingMode.Air) BrushMarker.SetActive(true);
            continueOnSameAnchor = false;
        }

        public void SetProxyAnchor() {
            //set proxy anchor to current BrushMarker position
            if (currentProxyAnchor == null) {
                currentProxyAnchor = Instantiate(AnchorPlanePrefab, BrushMarker.transform.position, BrushMarker.transform.rotation);
                currentProxyAnchorPlane = new Plane(-Camera.transform.forward, currentProxyAnchor.transform.position);
                // Debug.Log($"PROXY ANCHOR CREATED AT {currentProxyAnchor.transform.position.ToString()}");
            } else {
                currentProxyAnchor.transform.position = BrushMarker.transform.position;
                // Debug.Log($"PROXY ANCHOR MOVED TO {BrushMarker.transform.position.ToString()}");
            }
            currentProxyAnchor.transform.LookAt(Camera.transform.position);
            currentProxyAnchorPlane.SetNormalAndPosition(currentProxyAnchor.transform.forward, currentProxyAnchor.transform.position);
            isSketchingRelatively = true;
            ResetBrushmarker();
        }

        public void ToggleAirSketchingSpace() {
            if (isSketchingRelatively) {
                isSketchingRelatively = false;
                toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "ABSOLUTE";
                setProxyAnchorBtn.SetActive(false);
                if (currentProxyAnchor != null) currentProxyAnchor.SetActive(false);
                ResetBrushmarker();
            } else {
                isSketchingRelatively = true;
                toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "RELATIVE";
                setProxyAnchorBtn.SetActive(true);
                if (currentProxyAnchor == null) SetProxyAnchor();
                currentProxyAnchor.SetActive(true);
            }
        }

        public void ToggleSketchingMode() {
            //always sets absolute air drawing as default when switching modes
            if (drawingMode == DrawingMode.Air) {
                drawingMode = DrawingMode.Screen;
                toggleModeBtn.GetComponentInChildren<TMP_Text>().text = "screen";
                BrushMarker.SetActive(false);
                if (isSketchingRelatively) ToggleAirSketchingSpace();
                if (currentProxyAnchor != null) currentProxyAnchor.SetActive(false);
            } else {
                drawingMode = DrawingMode.Air;
                toggleModeBtn.GetComponentInChildren<TMP_Text>().text = "air";
                BrushMarker.SetActive(true);
                if (isSketchingRelatively) ToggleAirSketchingSpace();
                if (currentProxyAnchor != null) currentProxyAnchor.SetActive(false);
            }
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
