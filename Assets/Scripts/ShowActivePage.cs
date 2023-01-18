using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowActivePage : MonoBehaviour {
    public GameObject dot;
    public GameObject screens;
    private PageSwiper swiper;
    void Start() {
        swiper = screens.GetComponent<PageSwiper>();
        int count = screens.transform.childCount;
        for (int i = 0; i < count; i++) {
            Instantiate(dot, transform);
        }
    }

    void Update() {
        int current = swiper.getCurrentPage();
        for (int i = 0; i < transform.childCount; i++) {
            if (i == current) transform.GetChild(i).transform.Find("Color").GetComponent<Image>().color = new Color32(255, 255, 255, 200);
            else transform.GetChild(i).transform.Find("Color").GetComponent<Image>().color = new Color32(255, 255, 255, 60);
        }
    }
}
