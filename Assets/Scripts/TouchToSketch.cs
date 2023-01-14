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
    using UnityEngine.UI;

    public enum DrawingMode { Display, Air }

    public class TouchToSketch : MonoBehaviour {
        public Camera Camera;
        public SketchWorld SketchWorld;
        public GameObject SketchObjectPrefab;
        public DefaultReferences Defaults;

        //indicator for where new lines are drawn from
        public GameObject Brush;

        [Tooltip("Visualization of the canvas.")]
        public GameObject CanvasPrefab = null;
        private GameObject currentCanvas;
        // private Plane currentCanvasPlane;

        //hit point
        private GameObject currentHitpoint;
        //brush at current plane hit point
        private GameObject relativeBrush;
        //null parented to camera movement
        private GameObject currentProxyAnchorNull;
        //for relative air drawing, keep relative position in between touch inputs
        private bool continueOnSameAnchor;
        //drawing relative to the actual device position
        private bool usingRelativePosition = false;

        //tools
        private GameObject toggleMarkerButton;
        private bool markerActive = false;

        private GameObject setCanvasButton;
        private GameObject toggleCanvasButton;
        private bool canvasActive = false;

        private GameObject toggleConnectButton;
        private bool connectActive = false;



        private LineRenderer lineRenderer;
        private GameObject distanceDisplay;

        private ARAnchor worldAnchor;
        private LineSketchObject currentLineSketchObject;
        private CommandInvoker invoker;

        private bool isValidTouch = false;
        private bool startNewSketchObject = false;

        private bool startedOnCanvas = false;

        private float brushDistance = 0.2f;

        public void Start() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Brush.transform.SetParent(Camera.transform);
            Brush.transform.localPosition = Vector3.forward * brushDistance;
            // Brush.SetActive(false);
            Brush.GetComponent<Renderer>().enabled = false;

            invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
            lineRenderer = GameObject.Find("Line").GetComponent<LineRenderer>();
            distanceDisplay = GameObject.Find("Distance Display");
            lineRenderer.gameObject.SetActive(false);

            toggleMarkerButton = GameObject.Find("Toggle Marker Tool");
            toggleCanvasButton = GameObject.Find("Toggle Canvas Tool");
            toggleConnectButton = GameObject.Find("Toggle Connect Tool");
            setCanvasButton = GameObject.Find("Place Canvas");
            setCanvasButton.SetActive(false);
        }

        public void Update() {
            // if (currentProxyAnchorNull && relativeBrush && Brush.activeSelf) {
            if (currentProxyAnchorNull && relativeBrush) {
                //update proxyAnchorBrush relative position
                //get camera position relative to hitpoint origin
                if (markerActive) {
                    Vector3 relativeCameraPos = getRelativePosition(currentProxyAnchorNull.transform, Brush.transform.position);
                    relativeBrush.transform.position = relativeCameraPos;
                } else {
                    //TODO:
                    Vector3 relativeCameraPos = getRelativePosition(currentProxyAnchorNull.transform, Brush.transform.position);
                    relativeBrush.transform.position = relativeCameraPos;
                }
            }

            // if (connectActive && markerActive && !currentLineSketchObject) {
            //     Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            //     RaycastHit hit;
            //     Camera.transform.parent = null;
            //     if (Physics.Raycast(ray, out hit)) {
            //         //only continue if the hit object is not a canvas
            //         // if (hit.collider.gameObject.layer != 6) {
            //         Vector3 center = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, brushDistance));
            //         Vector3 realCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            //         // Vector3 bottomCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0f));
            //         // Vector3 bottomCenterLineStart = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, brushDistance));
            //         Brush.transform.localPosition = hit.point;
            //         lineRenderer.gameObject.SetActive(true);
            //         lineRenderer.SetPosition(0, center);
            //         lineRenderer.SetPosition(1, hit.point);
            //         lineRenderer.startWidth = 0.0003f;
            //         lineRenderer.endWidth = 0.0003f;
            //         distanceDisplay.GetComponentInChildren<TMP_Text>().text = $"{(hit.point - realCenter).magnitude.ToString("F2")}m";
            //         // }
            //     } else {
            //         HideDistanceDisplay();
            //     }
            // }

            if (Input.touchCount > 0 && !Eraser.IsEnabled) {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began) {
                    isValidTouch = Helpers.IsValidTouch(currentTouch);
                }

                if (isValidTouch) {
                    if (currentTouch.phase == TouchPhase.Began) {
                        startNewSketchObject = true;
                    } else if (currentTouch.phase == TouchPhase.Stationary || currentTouch.phase == TouchPhase.Moved) {
                        if (startNewSketchObject) {
                            //START NEW LINE
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;

                            //if extending an existing line, raycast from brush position, set anchorNull and start drawing from that hitpoint
                            Ray ray;
                            if (markerActive) {
                                ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                            } else {
                                ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                            }
                            if (connectActive) {
                                RaycastHit hit;

                                Camera.transform.parent = null;
                                if (Physics.Raycast(ray, out hit)) {
                                    //only continue if the hit object is not a canvas
                                    if (hit.collider.gameObject.layer != 6) {
                                        //point where the user is starting a new sketch
                                        currentHitpoint = new GameObject();
                                        currentHitpoint.transform.position = hit.point;

                                        currentLineSketchObject.transform.position = hit.point;

                                        //null, new "origin" at hitpoint
                                        currentProxyAnchorNull = new GameObject();
                                        currentProxyAnchorNull.transform.position = Brush.transform.position;
                                        // currentProxyAnchorNull.transform.position = Camera.transform.position;

                                        //brush from where new lines are drawn
                                        relativeBrush = Instantiate(Brush, hit.point, Quaternion.identity);
                                        relativeBrush.transform.SetParent(currentHitpoint.transform);
                                        // Brush.SetActive(false);
                                        usingRelativePosition = true;
                                        Brush.GetComponent<Renderer>().enabled = false;
                                    }
                                }
                            } else if (canvasActive && !startedOnCanvas) {
                                RaycastHit[] hits = Physics.RaycastAll(ray);
                                int i = 0;
                                while (i < hits.Length) {
                                    if (hits[i].collider.gameObject.layer == 6) {
                                        startedOnCanvas = true;
                                        break;
                                    }
                                    i++;
                                }
                            }
                        } else if (currentLineSketchObject) {
                            //CONTINUE A LINE
                            if (!markerActive) {
                                if (connectActive && usingRelativePosition) {
                                    //draw at relative brush pos
                                    new AddControlPointContinuousCommand(currentLineSketchObject, relativeBrush.transform.position).Execute();
                                } else {
                                    bool drawingOnCanvas = false;
                                    if (canvasActive && startedOnCanvas) {
                                        //draw on canvas using touch position
                                        Ray ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));
                                        RaycastHit[] hits = Physics.RaycastAll(ray);
                                        int i = 0;
                                        while (i < hits.Length) {
                                            if (hits[i].collider.gameObject.layer == 6) {
                                                drawingOnCanvas = true;
                                                new AddControlPointContinuousCommand(currentLineSketchObject, hits[i].point).Execute();
                                                break;
                                            }
                                            i++;
                                        }
                                        if (!drawingOnCanvas) {
                                            // if (startedOnCanvas) startNewSketchObject = true;
                                            FinishSketch();
                                            startNewSketchObject = true;
                                            currentLineSketchObject = null;
                                        }
                                    }
                                    if (!drawingOnCanvas && currentLineSketchObject) {
                                        //draw at current absolute touch position
                                        var touchPos = Camera.main.ScreenToWorldPoint(new Vector3(currentTouch.position.x, currentTouch.position.y, brushDistance));
                                        new AddControlPointContinuousCommand(currentLineSketchObject, touchPos).Execute();
                                    }
                                }
                            } else if (markerActive) {
                                if (connectActive && usingRelativePosition) {
                                    //draw at relative brush pos
                                    new AddControlPointContinuousCommand(currentLineSketchObject, relativeBrush.transform.position).Execute();
                                } else {
                                    bool drawingOnCanvas = false;
                                    if (canvasActive && startedOnCanvas) {
                                        //draw on canvas using brush position
                                        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                                        RaycastHit[] hits = Physics.RaycastAll(ray);
                                        int i = 0;
                                        // bool wentOffCanvas = false;
                                        while (i < hits.Length) {
                                            if (hits[i].collider.gameObject.layer == 6) {
                                                drawingOnCanvas = true;
                                                new AddControlPointContinuousCommand(currentLineSketchObject, hits[i].point).Execute();
                                                break;
                                            }
                                            i++;
                                        }
                                        if (!drawingOnCanvas) {
                                            // if (startedOnCanvas) startNewSketchObject = true;
                                            FinishSketch();
                                            startNewSketchObject = true;
                                            currentLineSketchObject = null;
                                        }
                                    }
                                    if (!drawingOnCanvas && currentLineSketchObject) {
                                        //draw at current absolute brush position
                                        new AddControlPointContinuousCommand(currentLineSketchObject, Brush.transform.position).Execute();
                                    }
                                }
                            }
                        }
                    } else if (currentTouch.phase == TouchPhase.Ended) {
                        FinishSketch();
                        isValidTouch = false;
                        startedOnCanvas = false;

                    }
                }
            }
        }

        private void FinishSketch() {
            //delete sketch object if empty
            if (startNewSketchObject == false && currentLineSketchObject.getNumberOfControlPoints() < 1) {
                Destroy(currentLineSketchObject.gameObject);
                currentLineSketchObject = null;
            }

            PostProcessSketchObject();

            usingRelativePosition = false;
            if (markerActive) Brush.GetComponent<Renderer>().enabled = true;
            Destroy(relativeBrush);
        }

        /*
        void LateUpdate() {
            // update distance display to show distance to any existing line or canvas
            // if (currentCanvas && currentProxyAnchorBrush) {
            // if (!(connectActive && markerActive) || !startNewSketchObject) return;
            // only update if using marker with connect mode and if not actively sketching
            if (!(connectActive && markerActive) || !startNewSketchObject) HideDistanceDisplay();

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Camera.transform.parent = null;
            if (Physics.Raycast(ray, out hit)) {
                //only continue if the hit object is not a canvas
                // if (hit.collider.gameObject.layer != 6) {
                Vector3 center = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, brushDistance));
                Vector3 realCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
                // Vector3 bottomCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0f));
                // Vector3 bottomCenterLineStart = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, brushDistance));
                Brush.transform.localPosition = hit.point;
                lineRenderer.gameObject.SetActive(true);
                lineRenderer.SetPosition(0, center);
                lineRenderer.SetPosition(1, hit.point);
                lineRenderer.startWidth = 0.0003f;
                lineRenderer.endWidth = 0.0003f;
                distanceDisplay.GetComponentInChildren<TMP_Text>().text = $"{(hit.point - realCenter).magnitude.ToString("F2")}m";
                // }
            } else {
                HideDistanceDisplay();
            }
        }*/

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

        // private void ResetBrush() {
        //     Destroy(currentProxyAnchorBrush);
        //     if (drawingMode == DrawingMode.Air) Brush.SetActive(true);
        //     continueOnSameAnchor = false;
        // }

        private void HideDistanceDisplay() {
            lineRenderer.gameObject.SetActive(false);
            distanceDisplay.GetComponentInChildren<TMP_Text>().text = "";
        }

        public void SetCanvas() {
            //set proxy anchor to current BrushMarker position
            if (currentCanvas == null) {
                currentCanvas = Instantiate(CanvasPrefab, Brush.transform.position, Brush.transform.rotation);
                // Debug.Log($"PROXY ANCHOR CREATED AT {currentProxyAnchor.transform.position.ToString()}");
            } else {
                currentCanvas.transform.position = Brush.transform.position;
                // Debug.Log($"PROXY ANCHOR MOVED TO {BrushMarker.transform.position.ToString()}");
            }
            currentCanvas.transform.LookAt(Camera.transform.position);
            usingRelativePosition = true;
            // ResetBrush();
            distanceDisplay.GetComponentInChildren<TMP_Text>().text = "";
        }

        public void ToggleMarker() {
            if (markerActive) {
                toggleMarkerButton.GetComponent<Image>().color = Color.white;
                markerActive = false;
                Brush.GetComponent<Renderer>().enabled = false;
            } else {
                toggleMarkerButton.GetComponent<Image>().color = Color.green;
                markerActive = true;
                Brush.GetComponent<Renderer>().enabled = true;
            }
        }

        public void ToggleCanvas() {
            if (canvasActive) {
                toggleCanvasButton.GetComponent<Image>().color = Color.white;
                canvasActive = false;
                setCanvasButton.SetActive(false);
                currentCanvas.SetActive(false);
            } else {
                toggleCanvasButton.GetComponent<Image>().color = Color.green;
                canvasActive = true;
                setCanvasButton.SetActive(true);
                currentCanvas.SetActive(true);
            }
        }

        public void ToggleConnect() {
            if (connectActive) {
                toggleConnectButton.GetComponent<Image>().color = Color.white;
                connectActive = false;
                lineRenderer.gameObject.SetActive(false);
            } else {
                toggleConnectButton.GetComponent<Image>().color = Color.green;
                connectActive = true;
                lineRenderer.gameObject.SetActive(true);
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
