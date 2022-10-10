using UnityEngine;
using UnityEngine.UI;

public class ColorMenu : MonoBehaviour {
    private GameObject options;
    public static bool IsOpen = false;

    [SerializeField]
    public GameObject ColorButton;
    private Image currentColorImage;
    public static Color CurrentColor;
    private Color32[] customColors = new Color32[] {
        Color.black,
        Color.white,
        new Color32(218, 255, 0, 255),
        new Color32(255, 165, 0, 255),
        new Color32(255, 0, 90, 255),
        new Color32(165, 0, 255, 255),
        new Color32(37, 0, 255, 255),
        new Color32(0, 218, 255, 255),
        new Color32(0, 255, 165, 255)
    };
    private Color[] colors = new Color[] { Color.black, Color.gray, Color.white, Color.red, Color.green, Color.blue };

    void Awake() {
        //set up color picker
        options = GameObject.Find("Color Options");
        currentColorImage = GameObject.Find("Current Color").GetComponent<Image>();
        CurrentColor = currentColorImage.color;
        foreach (Color32 c in customColors) {
            var btn = Instantiate(ColorButton);
            btn.transform.Find("Mask").transform.Find("Color").GetComponent<Image>().color = c;
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

    public void SetColor(Color color) {
        CurrentColor = color;
        currentColorImage.color = color;
        Close();
    }
}
