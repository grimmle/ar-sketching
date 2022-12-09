using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

public class Helpers : MonoBehaviour {

    public static bool IsValidTouch(Touch currentTouch) {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

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
}
