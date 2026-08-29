using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuestMapMarkerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject tooltip;
    private RectTransform markerRect;
    private bool isHovered;

    private void Awake()
    {
        markerRect = transform as RectTransform;
    }

    private void Update()
    {
        if (markerRect == null)
        {
            return;
        }

        bool pointerInside = RectTransformUtility.RectangleContainsScreenPoint(
            markerRect,
            Input.mousePosition,
            null
        );

        SetHovered(pointerInside);
    }

    public void Initialize(string displayName, TMP_FontAsset font, Sprite panelSprite)
    {
        tooltip = new GameObject(
            "MapMarkerNameTooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        tooltip.layer = gameObject.layer;

        RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
        tooltipRect.SetParent(transform, false);
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0.5f, 0f);
        tooltipRect.anchoredPosition = new Vector2(0f, 32f);
        tooltipRect.sizeDelta = new Vector2(160f, 52f);

        Image background = tooltip.GetComponent<Image>();
        background.sprite = panelSprite;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        GameObject textObject = new(
            "NameText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(tooltipRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 8f);
        textRect.offsetMax = new Vector2(-24f, -8f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = displayName;
        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 22f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        tooltip.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    private void SetHovered(bool value)
    {
        if (isHovered == value)
        {
            return;
        }

        isHovered = value;

        if (tooltip == null)
        {
            return;
        }

        if (isHovered)
        {
            transform.SetAsLastSibling();
            tooltip.SetActive(true);
            tooltip.transform.SetAsLastSibling();
        }
        else
        {
            tooltip.SetActive(false);
        }
    }
}
