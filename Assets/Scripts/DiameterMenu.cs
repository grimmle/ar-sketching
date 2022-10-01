using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DiameterMenu : MonoBehaviour {
    private GameObject options;
    public static bool IsOpen = false;

    [SerializeField]
    public GameObject DiameterButton;
    private TMP_Text currentDiameterText;
    public static float CurrentDiameter;

    class DiameterObject {
        public float diameter { get; }
        public string title { get; }

        public DiameterObject(float diameter, string title) {
            this.diameter = diameter;
            this.title = title;
        }
    }

    private Dictionary<string, float> diameters = new Dictionary<string, float>();

    void Awake() {
        diameters.Add("XS", 0.005f);
        diameters.Add("S", 0.01f);
        diameters.Add("M", 0.02f);
        diameters.Add("L", 0.03f);
        diameters.Add("XL", 0.04f);

        //set up diameter picker
        options = GameObject.Find("Diameter Options");
        currentDiameterText = GameObject.Find("Current Diameter").GetComponent<TMP_Text>();

        //default diameter is Medium
        currentDiameterText.text = "M";
        CurrentDiameter = diameters["M"];

        foreach (var title in diameters.Keys) {
            var btn = Instantiate(DiameterButton);
            btn.transform.Find("Diameter").GetComponent<TMP_Text>().text = title;
            btn.transform.SetParent(options.transform);
        }
    }

    public void Open() {
        IsOpen = true;
        CanvasGroup group = options.GetComponent<CanvasGroup>();
        group.alpha = 1;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    public void Close() {
        IsOpen = false;
        CanvasGroup group = options.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void SetDiameter(string diameter) {
        currentDiameterText.text = diameter;
        CurrentDiameter = diameters[diameter];
        Close();
    }
}
