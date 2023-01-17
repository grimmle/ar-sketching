using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PageSwiper : MonoBehaviour, IDragHandler, IEndDragHandler {
    private Vector3 panelLocation;
    private float dragThreshold = 0.2f;
    public float easingSeconds = 0.1f;
    private int currentPage = 0;

    void Start() {
        panelLocation = transform.position;
        //place help screens (children) next to each other
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, Screen.width * i, Screen.width);
        }
    }

    public void OnDrag(PointerEventData data) {
        float diff = data.pressPosition.x - data.position.x;
        // float clampedDiff = Mathf.Clamp(diff, -Screen.width, Screen.width);
        transform.position = panelLocation - new Vector3(diff, 0, 0);
    }
    public void OnEndDrag(PointerEventData data) {
        float diff = data.pressPosition.x - data.position.x;
        float percentageDragged = diff / Screen.width;
        if (Mathf.Abs(percentageDragged) >= dragThreshold) {
            Vector3 newLocation = panelLocation;
            if (percentageDragged > 0 && currentPage < transform.childCount - 1) {
                newLocation += new Vector3(-Screen.width, 0, 0);
                currentPage++;
            } else if (percentageDragged < 0 && currentPage > 0) {
                newLocation += new Vector3(Screen.width, 0, 0);
                currentPage--;
            }
            StartCoroutine(SmoothMove(transform.position, newLocation, easingSeconds));
            panelLocation = newLocation;
        } else {
            StartCoroutine(SmoothMove(transform.position, panelLocation, easingSeconds));
        }
    }
    IEnumerator SmoothMove(Vector3 startPos, Vector3 endPos, float seconds) {
        float t = 0f;
        while (t <= 1.0) {
            t += Time.deltaTime / seconds;
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }
}
