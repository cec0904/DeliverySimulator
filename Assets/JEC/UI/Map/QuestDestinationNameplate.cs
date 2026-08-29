using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestDestinationNameplate : MonoBehaviour
{
    private QuestDestination target;
    private RectTransform screenRoot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera targetCamera;
    private float headHeight;
    private const float MaxVisibleDistance = 30f;

    public static QuestDestinationNameplate Create(
        QuestDestination destination,
        RectTransform screenRoot,
        TMP_FontAsset font,
        Sprite compactUiSprite
    )
    {
        GameObject root = new(
            $"QuestNpcName_{destination.DisplayName}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(QuestDestinationNameplate)
        );
        root.layer = screenRoot.gameObject.layer;

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(screenRoot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(160f, 58f);

        Image background = root.GetComponent<Image>();
        background.sprite = compactUiSprite;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        GameObject textObject = new(
            "NpcNameText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = root.layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 9f);
        textRect.offsetMax = new Vector2(-24f, -9f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = destination.DisplayName;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 17f;
        text.fontSizeMax = 24f;
        text.raycastTarget = false;

        QuestDestinationNameplate nameplate = root.GetComponent<QuestDestinationNameplate>();
        nameplate.target = destination;
        nameplate.screenRoot = screenRoot;
        nameplate.rectTransform = rect;
        nameplate.canvasGroup = root.GetComponent<CanvasGroup>();
        nameplate.headHeight = CalculateHeadHeight(destination);
        nameplate.UpdateScreenPosition();

        rect.SetAsLastSibling();
        return nameplate;
    }

    private void LateUpdate()
    {
        if (target == null || screenRoot == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateScreenPosition();
    }

    private void UpdateScreenPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            SetVisible(false);
            return;
        }

        Transform anchor = target.QuestUIAnchor;
        Vector3 worldPosition = anchor != null
            ? anchor.position
            : target.transform.position + Vector3.up * headHeight;
        Vector3 screenPoint = targetCamera.WorldToScreenPoint(worldPosition);

        float sqrDistance =
            (target.transform.position - targetCamera.transform.position).sqrMagnitude;

        if (sqrDistance > MaxVisibleDistance * MaxVisibleDistance)
        {
            SetVisible(false);
            return;
        }

        bool visible = screenPoint.z > 0f &&
                       screenPoint.x >= 0f && screenPoint.x <= Screen.width &&
                       screenPoint.y >= 0f && screenPoint.y <= Screen.height;

        if (!visible || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenRoot,
                screenPoint,
                null,
                out Vector2 localPoint
            ))
        {
            SetVisible(false);
            return;
        }

        rectTransform.anchoredPosition = localPoint;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private static float CalculateHeadHeight(QuestDestination destination)
    {
        float highestPoint = destination.transform.position.y + 2.2f;

        foreach (Renderer childRenderer in destination.GetComponentsInChildren<Renderer>(true))
        {
            highestPoint = Mathf.Max(highestPoint, childRenderer.bounds.max.y);
        }

        return highestPoint - destination.transform.position.y + 0.25f;
    }
}
