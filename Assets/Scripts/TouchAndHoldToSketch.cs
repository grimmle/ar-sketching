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

    public enum DrawingMode { Display, Air }

    public class TouchAndHoldToSketch : MonoBehaviour {
        public Camera Camera;
        public SketchWorld SketchWorld;
        public GameObject SketchObjectPrefab;
        public DefaultReferences defaults;

        //indicator for where new objects are drawn from
        public GameObject Brush;

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

        private LineRenderer lineRenderer;
        private GameObject distanceDisplay;

        private ARAnchor worldAnchor;
        private LineSketchObject currentLineSketchObject;
        private CommandInvoker invoker;

        private bool isValidTouch = false;
        private bool startNewSketchObject = false;

        private DrawingMode drawingMode = DrawingMode.Display;

        public void Start() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Brush.transform.SetParent(Camera.transform);
            Brush.transform.localPosition = Vector3.forward * 0.3f;
            Brush.SetActive(false);

            invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
            lineRenderer = GameObject.Find("Line").GetComponent<LineRenderer>();
            distanceDisplay = GameObject.Find("Distance Display");
            lineRenderer.gameObject.SetActive(false);
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
            if (currentProxyAnchor) {
                if (currentProxyAnchorBrush) {
                    //update proxyAnchorBrush relative position
                    //get camera position relative to hitpoint origin
                    var relativeCameraPos = getRelativePosition(currentProxyAnchorNull.transform, Camera.transform.position);
                    currentProxyAnchorBrush.transform.localPosition = relativeCameraPos;
                }

                //scale canvas according to distance
                Vector3 defaultScale = new Vector3(0.05f, 1, 0.05f);
                float minDistance = 1f;
                var d = Vector3.Distance(Camera.transform.position, currentProxyAnchor.transform.position);
                if (d > minDistance) {
                    Vector3 scaleVector = new Vector3(1 + d - minDistance, 1, 1 + d - minDistance);
                    Vector3 newScale = Vector3.Scale(defaultScale, scaleVector);
                    currentProxyAnchor.transform.GetChild(0).localScale = newScale;
                    if (currentProxyAnchorBrush) {
                        currentProxyAnchorBrush.transform.localScale = Vector3.Scale(new Vector3(0.005f, 0.005f, 0.005f), scaleVector);
                    }
                } else {
                    currentProxyAnchor.transform.GetChild(0).localScale = defaultScale;
                    if (currentProxyAnchorBrush) {
                        currentProxyAnchorBrush.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
                    }
                }
            }

            if (Input.touchCount > 0 && !Eraser.SelectionActive) {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began) {
                    isValidTouch = Helpers.IsValidTouch(currentTouch);
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
                                // float enter = 0.0f;
                                Camera.transform.parent = null;

                                RaycastHit hit;
                                // if (currentProxyAnchorPlane.Raycast(ray, out enter)) {
                                // Vector3 dir = currentProxyAnchor.transform.position - Camera.main.transform.position;
                                // if (Physics.Raycast(Camera.main.transform.position, dir, out hit)) {
                                if (Physics.Raycast(ray, out hit)) {
                                    Debug.Log(hit.ToString());
                                    Debug.Log(hit.point);
                                    Debug.Log(hit.collider.gameObject.layer);
                                    // only continue if the hit object is a canvas
                                    if (hit.collider.gameObject.layer == 6) {
                                        // Vector3 hitPoint = ray.GetPoint(enter);
                                        //point where the user is starting a new sketch
                                        currentHitpoint = new GameObject();
                                        currentHitpoint.transform.position = hit.point;

                                        currentLineSketchObject.transform.position = hit.point;

                                        //null, new "origin" at hitpoint
                                        currentProxyAnchorNull = new GameObject();
                                        currentProxyAnchorNull.transform.position = Camera.transform.position;

                                        //brush from where new lines are drawn
                                        currentProxyAnchorBrush = Instantiate(Brush, hit.point, Quaternion.identity);
                                        currentProxyAnchorBrush.transform.SetParent(currentHitpoint.transform);
                                        continueOnSameAnchor = true;
                                        Brush.SetActive(false);
                                    }
                                }
                            }
                        } else if (currentLineSketchObject) {
                            //add new control point according to current device position or active proxy
                            if (isSketchingRelatively && currentProxyAnchorBrush != null) {
                                lineRenderer.gameObject.SetActive(false);
                                //create sketch from relative brush position
                                new AddControlPointContinuousCommand(currentLineSketchObject, currentProxyAnchorBrush.transform.position).Execute();
                            } else {
                                new AddControlPointContinuousCommand(currentLineSketchObject, Brush.transform.position).Execute();
                            }
                        }
                    } else if (drawingMode == DrawingMode.Display && (currentTouch.phase == TouchPhase.Stationary || currentTouch.phase == TouchPhase.Moved)) {
                        if (startNewSketchObject) {
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;
                        } else if (currentLineSketchObject) {
                            //if sketching relatively, raycast from screen touchPoint to anchorPlane
                            if (isSketchingRelatively && currentProxyAnchor != null) {
                                Ray ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                                RaycastHit hit;

                                if (Physics.Raycast(ray, out hit)) {
                                    // only continue if the hit object is a canvas
                                    if (hit.collider.gameObject.layer == 6) {
                                        new AddControlPointContinuousCommand(currentLineSketchObject, hit.point).Execute();
                                    }
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

                        if (drawingMode == DrawingMode.Air && currentProxyAnchorBrush) lineRenderer.gameObject.SetActive(true);

                        PostProcessSketchObject();
                        isValidTouch = false;
                    }
                }
            }
        }

        void LateUpdate() {
            // update distance display
            if (currentProxyAnchor && currentProxyAnchorBrush) {
                Vector3 center = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.3f));
                lineRenderer.SetPosition(0, center);
                lineRenderer.SetPosition(1, currentProxyAnchorBrush.transform.position);
                lineRenderer.startWidth = 0.00025f;
                lineRenderer.endWidth = 0.00025f;
                distanceDisplay.GetComponentInChildren<TMP_Text>().text = $"{(currentProxyAnchorBrush.transform.position - center).magnitude.ToString("F2")}m";
            }
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
            if (drawingMode == DrawingMode.Air) Brush.SetActive(true);
            continueOnSameAnchor = false;
        }

        private void HideDistanceDisplay() {
            lineRenderer.gameObject.SetActive(false);
            distanceDisplay.GetComponentInChildren<TMP_Text>().text = "";
        }

        public void SetProxyAnchor() {
            //set proxy anchor to current BrushMarker position
            if (currentProxyAnchor == null) {
                currentProxyAnchor = Instantiate(AnchorPlanePrefab, Brush.transform.position, Brush.transform.rotation);
                currentProxyAnchorPlane = new Plane(-Camera.transform.forward, currentProxyAnchor.transform.position);
                // Debug.Log($"PROXY ANCHOR CREATED AT {currentProxyAnchor.transform.position.ToString()}");
            } else {
                currentProxyAnchor.transform.position = Brush.transform.position;
                // Debug.Log($"PROXY ANCHOR MOVED TO {BrushMarker.transform.position.ToString()}");
            }
            currentProxyAnchor.transform.LookAt(Camera.transform.position);
            currentProxyAnchorPlane.SetNormalAndPosition(currentProxyAnchor.transform.forward, currentProxyAnchor.transform.position);
            isSketchingRelatively = true;
            ResetBrushmarker();
        }

        public void ToggleSketchingSpace() {
            if (isSketchingRelatively) {
                isSketchingRelatively = false;
                toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "ABSOLUTE";
                setProxyAnchorBtn.SetActive(false);
                if (currentProxyAnchor != null) currentProxyAnchor.SetActive(false);
                ResetBrushmarker();
                HideDistanceDisplay();
            } else {
                isSketchingRelatively = true;
                toggleSpaceBtn.GetComponentInChildren<TMP_Text>().text = "RELATIVE";
                setProxyAnchorBtn.SetActive(true);
                if (currentProxyAnchor == null) SetProxyAnchor();
                currentProxyAnchor.SetActive(true);
                //activate linerenderer to show distance to brushmaker
                lineRenderer.gameObject.SetActive(true);
            }
        }

        public void ToggleSketchingMode() {
            //always sets 'absolute' as default when switching modes
            if (drawingMode == DrawingMode.Air) {
                drawingMode = DrawingMode.Display;
                toggleModeBtn.GetComponentInChildren<TMP_Text>().text = "display";
                Brush.SetActive(false);
                if (isSketchingRelatively) ToggleSketchingSpace();
                if (currentProxyAnchor != null) currentProxyAnchor.SetActive(false);
                HideDistanceDisplay();
            } else {
                drawingMode = DrawingMode.Air;
                toggleModeBtn.GetComponentInChildren<TMP_Text>().text = "air";
                Brush.SetActive(true);
                if (isSketchingRelatively) ToggleSketchingSpace();
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
