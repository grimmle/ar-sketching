using Sketching;
using UnityEngine;
using UnityEngine.UI;

public class ColorMenu : MonoBehaviour {
    private GameObject options;
    public static bool IsOpen = false;

    [SerializeField]
    public GameObject ColorButton;
    private Image currentColorImage;
    public static Color CurrentColor;

    private Color32[] customColors = new Color32[] { Color.black, Color.white, new Color32(255, 220, 116, 255), new Color32(251, 172, 135, 255), new Color32(255, 140, 135, 255), new Color32(222, 172, 249, 255), new Color32(174, 181, 255, 255), new Color32(149, 200, 243, 255), new Color32(129, 227, 225, 255), new Color32(125, 225, 152, 255) };
    private Color[] colors = new Color[] { Color.black, Color.gray, Color.white, Color.red, Color.green, Color.blue };

    void Awake() {
        //set up color picker
        options = GameObject.Find("Color Options");
        currentColorImage = GameObject.Find("Current Color").GetComponent<Image>();
        CurrentColor = currentColorImage.color;
        foreach (var c in customColors) {
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
        Debug.Log("choosing color " + color.ToString());
        CurrentColor = color;
        currentColorImage.color = color;
        Close();
    }
}
