//some credit for basic sketching implementation using VRSketchingGeometry to:
//https://github.com/tterpi/Sketchar

namespace Sketching {
    using UnityEngine;
    using TMPro;
    using UnityEngine.XR.ARFoundation;
    using VRSketchingGeometry.SketchObjectManagement;
    using VRSketchingGeometry.Commands;
    using VRSketchingGeometry.Commands.Line;
    using VRSketchingGeometry.Serialization;
    using VRSketchingGeometry;
    using UnityEngine.UI;

    public class TouchToSketch : MonoBehaviour {
        public Camera Camera;
        public SketchWorld SketchWorld;
        public GameObject SketchObjectPrefab;
        public DefaultReferences Defaults;
        private GameObject UI;

        [Tooltip("Visualization of the brush.")]
        public GameObject Brush;
        private float defaultBrushDistance = 0.15f;
        private float relativeBrushDistance;

        [Tooltip("Visualization of the canvas.")]
        public GameObject CanvasPrefab = null;
        //currently placed canvas
        private GameObject currentCanvas;

        //brush at current plane hit point
        private GameObject relativeBrush;
        //drawing relative to the actual device position
        private bool usingRelativePosition = false;

        //TOOLS
        private GameObject toggleMarkerButton;
        private bool markerActive = false;

        private GameObject toggleCanvasButton;
        private bool canvasActive = false;
        private GameObject setCanvasButton;

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

        public void Start() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Brush.transform.SetParent(Camera.transform);
            Brush.transform.localPosition = Vector3.forward * defaultBrushDistance;
            Brush.GetComponent<Renderer>().enabled = false;

            invoker = GameObject.Find("Main").GetComponent<GlobalCommandInvoker>().invoker;
            lineRenderer = GameObject.Find("Line").GetComponent<LineRenderer>();
            distanceDisplay = GameObject.Find("Distance Display");
            lineRenderer.gameObject.SetActive(false);

            UI = GameObject.Find("UI");
            toggleMarkerButton = GameObject.Find("Toggle Marker Tool");
            toggleCanvasButton = GameObject.Find("Toggle Canvas Tool");
            toggleConnectButton = GameObject.Find("Toggle Connect Tool");
            setCanvasButton = GameObject.Find("Place Canvas");
            setCanvasButton.SetActive(false);
        }

        public void Update() {
            UI.GetComponent<CanvasGroup>().alpha = 1;

            //show distance to existing lines when using marker & connect tools
            if (markerActive && connectActive && !isValidTouch) {
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                bool hitObject = false;
                if (Physics.Raycast(ray, out hit)) {
                    //only continue if the hit object is not a canvas and not the brush
                    if (hit.collider.gameObject.layer != 6 && hit.collider.gameObject.layer != 2) {
                        hitObject = true;
                        Brush.transform.parent = null;
                        Brush.transform.position = hit.point;
                        Vector3 center = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
                        distanceDisplay.GetComponentInChildren<TMP_Text>().text = $"{(hit.point - center).magnitude.ToString("F2")}m";
                    }
                }
                if (!hitObject) {
                    HideDistanceDisplay();
                    ResetBrush();
                }
            }

            //handle touch input
            if (Input.touchCount > 0 && !Eraser.IsEnabled) {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began) isValidTouch = Helpers.IsValidTouch(currentTouch);
                if (isValidTouch) {
                    // reduce UI opacity for better visibility
                    UI.GetComponent<CanvasGroup>().alpha = 0.3f;
                    if (currentTouch.phase == TouchPhase.Began) {
                        startNewSketchObject = true;
                    } else if (currentTouch.phase == TouchPhase.Stationary || currentTouch.phase == TouchPhase.Moved) {
                        if (startNewSketchObject) {
                            /* - - - - - - - - - - - - - - - - - - - */
                            /* - - - - - START A  NEW LINE - - - - - */
                            /* - - - - - - - - - - - - - - - - - - - */
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;

                            Ray ray;
                            if (markerActive) ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                            else ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));

                            //if extending an existing line, raycast from brush position, set anchorNull and start drawing from that hitpoint
                            if (canvasActive && !startedOnCanvas) {
                                //detect where if a sketch is started on a canvas
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
                            if (connectActive && !startedOnCanvas) {
                                RaycastHit hit;
                                if (Physics.Raycast(ray, out hit)) {
                                    //only continue if the hit object is not a canvas and not the brush
                                    if (hit.collider.gameObject.layer != 6 && hit.collider.gameObject.layer != 2) {
                                        currentLineSketchObject.transform.position = hit.point;

                                        //instantiate relative brush and make child of camera
                                        relativeBrush = Instantiate(Brush, hit.point, Quaternion.identity);
                                        relativeBrush.transform.SetParent(Camera.transform);

                                        relativeBrushDistance = Vector3.Distance(Camera.transform.position, relativeBrush.transform.position);

                                        usingRelativePosition = true;
                                        Brush.GetComponent<Renderer>().enabled = false;
                                    }
                                }
                            }
                        } else if (currentLineSketchObject) {
                            /* - - - - - - - - - - - - - - - - - - */
                            /* - - - - - CONTINUE A LINE - - - - - */
                            /* - - - - - - - - - - - - - - - - - - */
                            bool drawingOnCanvas = false;
                            if (canvasActive && startedOnCanvas) {
                                //CHECK FOR CANVAS DRAWING FIRST
                                Ray ray;
                                if (markerActive) ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                                else ray = Camera.main.ScreenPointToRay(new Vector3(currentTouch.position.x, currentTouch.position.y, 0f));

                                RaycastHit[] hits = Physics.RaycastAll(ray);
                                int i = 0;
                                while (i < hits.Length) {
                                    if (hits[i].collider.gameObject.layer == 6) {
                                        drawingOnCanvas = true;
                                        //draw on canvas using brush or touch position
                                        HideDistanceDisplay();
                                        ResetBrush();
                                        new AddControlPointContinuousCommand(currentLineSketchObject, hits[i].point).Execute();
                                        break;
                                    }
                                    i++;
                                }
                                if (!drawingOnCanvas) {
                                    FinishSketch();
                                    startNewSketchObject = true;
                                    currentLineSketchObject = null;
                                }
                            }
                            if (!drawingOnCanvas && currentLineSketchObject && connectActive && usingRelativePosition) {
                                //CHECK FOR CONNECT DRAWING
                                if (connectActive && usingRelativePosition) {
                                    if (markerActive && relativeBrushDistance > 0) {
                                        //draw at relative brush position
                                        new AddControlPointContinuousCommand(currentLineSketchObject, relativeBrush.transform.position).Execute();
                                    } else {
                                        //draw at relative brush position using current touch position
                                        var touchPos = Camera.main.ScreenToWorldPoint(new Vector3(currentTouch.position.x, currentTouch.position.y, relativeBrushDistance));
                                        new AddControlPointContinuousCommand(currentLineSketchObject, touchPos).Execute();
                                    }
                                }
                            } else if (!drawingOnCanvas && currentLineSketchObject) {
                                //CHECK FOR FREEHAND DRAWING
                                if (markerActive) {
                                    //draw at current absolute brush position
                                    new AddControlPointContinuousCommand(currentLineSketchObject, Brush.transform.position).Execute();
                                } else {
                                    //draw at current absolute touch position
                                    var touchPos = Camera.main.ScreenToWorldPoint(new Vector3(currentTouch.position.x, currentTouch.position.y, defaultBrushDistance));
                                    new AddControlPointContinuousCommand(currentLineSketchObject, touchPos).Execute();
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

            relativeBrushDistance = -1;
            usingRelativePosition = false;
            if (markerActive) Brush.GetComponent<Renderer>().enabled = true;
            Destroy(relativeBrush);
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
            currentLineSketchObject.minimumControlPointDistance = .0125f;
            LineBrush brush = currentLineSketchObject.GetBrush() as LineBrush;
            brush.SketchMaterial = new SketchMaterialData(newMat);
            brush.CrossSectionScale = DiameterMenu.CurrentDiameter;
            brush.InterpolationSteps = 8;
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

        private void ResetBrush() {
            Brush.transform.position = new Vector3(0, 0, 0);
            Brush.transform.SetParent(Camera.transform);
            Brush.transform.localPosition = new Vector3(0, 0, defaultBrushDistance);
        }

        private void HideDistanceDisplay() {
            lineRenderer.gameObject.SetActive(false);
            distanceDisplay.GetComponentInChildren<TMP_Text>().text = "";
        }

        public void SetCanvas() {
            //set proxy anchor to current brush position
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
                HideDistanceDisplay();
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
