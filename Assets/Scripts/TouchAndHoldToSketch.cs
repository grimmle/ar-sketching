//https://github.com/tterpi/Sketchar

namespace Sketching
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using VRSketchingGeometry.SketchObjectManagement;
    using VRSketchingGeometry.Commands;
    using VRSketchingGeometry.Commands.Line;
    using UnityEngine.XR.ARFoundation;

    // Controls the creation and deletion of line sketch objects via touch gestures.
    public class TouchAndHoldToSketch : MonoBehaviour
    {
        // Color Picker to extract current color
        public ColorPicker ColorPicker;
        // The first-person camera being used to render the passthrough camera image (i.e. AR background).
        public Camera Camera;
        // Prefab that is instatiated to create a new line
        public GameObject SketchObjectPrefab;
        // Shows were new control points are added
        public GameObject pointMarker;
        public SketchWorld SketchWorld;

        public float lineDiameter;

        // The anchor that all sketch objects are attached to
        private ARAnchor worldAnchor;
        // The line sketch object that is currently being created.
        private LineSketchObject currentLineSketchObject;
        // Used to check if the touch interaction should be performed
        private CommandInvoker Invoker;

        private bool canStartTouchManipulation = false;
        private bool startNewSketchObject = false;



        public void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            //set up marker in the center of the screen
            pointMarker.transform.SetParent(Camera.transform);
            pointMarker.transform.localPosition = Vector3.forward * .3f;
            Invoker = new CommandInvoker();
        }

        public void Update()
        {
            //handle the touch input
            if (Input.touchCount > 0)
            {
                Touch currentTouch = Input.GetTouch(0);
                if (currentTouch.phase == TouchPhase.Began)
                {
                    canStartTouchManipulation = CanStartTouchManipulation(currentTouch);
                }

                if (canStartTouchManipulation)
                {
                    if (currentTouch.phase == TouchPhase.Began)
                    {
                        startNewSketchObject = true;
                    }
                    else if (currentTouch.phase == TouchPhase.Stationary || (currentTouch.phase == TouchPhase.Moved && startNewSketchObject == false && currentLineSketchObject.getNumberOfControlPoints() > 0))
                    {

                        if (startNewSketchObject)
                        {
                            //create a new sketch object
                            CreateNewLineSketchObject();
                            startNewSketchObject = false;
                        }
                        else if (currentLineSketchObject)
                        {
                            //Add new control point according to current device position
                            new AddControlPointContinuousCommand(currentLineSketchObject, Camera.transform.position + Camera.transform.forward * .3f)
                                .Execute();
                        }
                    }
                    else if (currentTouch.phase == TouchPhase.Ended)
                    {
                        //if an empty sketch object was created, delete it
                        if (startNewSketchObject == false && currentLineSketchObject.getNumberOfControlPoints() < 1)
                        {
                            Destroy(currentLineSketchObject.gameObject);
                            currentLineSketchObject = null;
                        }

                        //if a swipe occured and no new sketch object was created, delete the last sketch object
                        if (((startNewSketchObject == false && currentLineSketchObject == null) || startNewSketchObject == true))
                        {
                            if ((currentTouch.position - currentTouch.rawPosition).magnitude > Screen.width * 0.05)
                            {
                                if (Vector2.Dot(Vector2.left, (currentTouch.position - currentTouch.rawPosition)) > 0)
                                {
                                    DeleteLastLineSketchObject();
                                }
                                else if (Vector2.Dot(Vector2.right, (currentTouch.position - currentTouch.rawPosition)) > 0)
                                {
                                    RestoreLastDeletedSketchObject();
                                }
                            }
                        }
                        else
                        {
                            PostProcessSketchObject();
                        }

                        canStartTouchManipulation = false;
                    }
                }
            }
        }

        // Checks if a touch interaction can be started
        private bool CanStartTouchManipulation(Touch currentTouch)
        {
            // Should not handle input if the player is pointing on UI or if the AR session is not tracking the environment.
            if (ARSession.state != ARSessionState.SessionTracking || EventSystem.current.IsPointerOverGameObject(currentTouch.fingerId))
            {
                Debug.Log("Not starting tap gesture");
                return false;
            }
            return true;
        }

        // Instatiates a new LineSketchObject and parants it to the world anchor
        private void CreateNewLineSketchObject()
        {
            //see if an anchor exists
            if (!worldAnchor)
            {
                GameObject anchor = new GameObject();
                anchor.name = "WorldAnchor";
                worldAnchor = anchor.AddComponent<ARAnchor>();
                SketchWorld.transform.SetParent(worldAnchor.transform);
            }

            // Instantiate sketch object as child of anchor
            var gameObject = Instantiate(SketchObjectPrefab);
            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material.color = ColorPicker.color;
            currentLineSketchObject = gameObject.GetComponent<LineSketchObject>();
            currentLineSketchObject.minimumControlPointDistance = .02f;
            currentLineSketchObject.SetLineDiameter(lineDiameter);
            currentLineSketchObject.SetInterpolationSteps(5);
        }

        // Refines the latest sketch object
        private void PostProcessSketchObject()
        {
            //add the current line sketch object to the stack
            if (currentLineSketchObject != null && currentLineSketchObject.gameObject != null)
            {
                Invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(currentLineSketchObject, SketchWorld));
                if (currentLineSketchObject.getNumberOfControlPoints() > 2)
                {
                    new RefineMeshCommand(currentLineSketchObject).Execute();
                }
            }
        }

        // Delete the last line sketch object using the Invoker.
        public void DeleteLastLineSketchObject()
        {
            Invoker.Undo();
        }

        public void RestoreLastDeletedSketchObject()
        {
            Invoker.Redo();
        }
    }
}
