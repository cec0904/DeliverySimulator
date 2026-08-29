using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestMapMarkerController : MonoBehaviour
{
    [Header("마커 리소스")]
    [SerializeField] private Texture2D maleNpcMarkerTexture;
    [SerializeField] private Texture2D femaleNpcMarkerTexture;
    [SerializeField] private Texture2D defaultStoreMarkerTexture;
    [SerializeField] private Sprite compactUiSprite;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private TMP_FontAsset font;

    [Header("전체 지도 월드 영역")]
    [SerializeField] private Vector2 mapWorldCenter = new(-371f, 10f);
    [SerializeField] private Vector2 capturedMapSize = new(1280f, 720f);
    [SerializeField] private float mapOrthographicSize = 300f;

    [Header("마커 크기")]
    [SerializeField] private float fullMapMarkerSize = 36f;
    [SerializeField] private float minimapMarkerSize = 34f;

    private sealed class MarkerBinding
    {
        public Transform worldTarget;
        public RectTransform fullMapMarker;
        public RectTransform minimapMarker;
    }

    private readonly List<MarkerBinding> markerBindings = new();
    private readonly Dictionary<QuestDestination, QuestDestinationNameplate> nameplates = new();

    private PlayerQuestList playerQuestList;
    private RectTransform fullMapRoot;
    private RectTransform fullMapContent;
    private RectTransform minimapImageRect;
    private RectTransform minimapOverlay;
    private Camera minimapCamera;
    private PlayerMapMarker fullMapProjection;
    private GameObject legend;
    private float nextResolveTime;

    private void Start()
    {
        BindPlayerQuestList();
        ResolveMapReferences();
        RebuildMarkers();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextResolveTime)
        {
            nextResolveTime = Time.unscaledTime + 1f;

            bool changed = BindPlayerQuestList();
            changed |= ResolveMapReferences();

            if (changed)
            {
                RebuildMarkers();
            }
        }

        UpdateMarkerPositions();
        UpdateNameplateVisibilityForMap();
    }

    private void OnDestroy()
    {
        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged -= RebuildMarkers;
        }

        ClearDynamicObjects();
    }

    private bool BindPlayerQuestList()
    {
        PlayerQuestList found = FindAnyObjectByType<PlayerQuestList>();

        if (found == playerQuestList)
        {
            return false;
        }

        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged -= RebuildMarkers;
        }

        playerQuestList = found;

        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged += RebuildMarkers;
        }

        return true;
    }

    private bool ResolveMapReferences()
    {
        bool changed = false;

        RectTransform foundMapRoot = FindSceneRectTransform("C_UIMAP");
        RectTransform foundMapContent = FindDescendant(foundMapRoot, "Map_Content");

        if (foundMapRoot != fullMapRoot || foundMapContent != fullMapContent)
        {
            fullMapRoot = foundMapRoot;
            fullMapContent = foundMapContent;
            DestroyLegend();
            changed = true;
        }

        PlayerMapMarker foundProjection = fullMapContent != null
            ? fullMapContent.GetComponentInChildren<PlayerMapMarker>(true)
            : null;

        if (foundProjection != fullMapProjection)
        {
            fullMapProjection = foundProjection;
            changed = true;
        }

        RectTransform minimapRoot = FindSceneRectTransform("C_MiniMap");
        RawImage minimapImage = FindRenderTextureImage(minimapRoot);
        RectTransform foundMinimapImageRect = minimapImage != null ? minimapImage.rectTransform : null;
        Camera foundMinimapCamera = FindCameraForTexture(minimapImage != null ? minimapImage.texture : null);

        if (foundMinimapImageRect != minimapImageRect || foundMinimapCamera != minimapCamera)
        {
            minimapImageRect = foundMinimapImageRect;
            minimapCamera = foundMinimapCamera;
            DestroyMinimapOverlay();
            changed = true;
        }

        // EnsureLegend();
        EnsureMinimapOverlay();

        return changed;
    }

    private void RebuildMarkers()
    {
        ClearDynamicObjects();
        DestroyLegend();
        // EnsureLegend();
        EnsureMinimapOverlay();

        if (playerQuestList == null)
        {
            return;
        }

        HashSet<QuestPickUpPoint> addedStores = new();
        HashSet<QuestDestination> addedDestinations = new();

        foreach (QuestRuntimeData quest in playerQuestList.SelectedQuests)
        {
            if (quest == null)
            {
                continue;
            }

            if (quest.pickupPoint != null && addedStores.Add(quest.pickupPoint))
            {
                Texture2D storeTexture = quest.questData != null && quest.questData.icon != null
                    ? quest.questData.icon
                    : quest.pickupPoint.RepresentativeIcon;
                storeTexture ??= defaultStoreMarkerTexture;

                markerBindings.Add(CreateMarker(
                    quest.pickupPoint.transform,
                    storeTexture,
                    quest.pickupPoint.DisplayName,
                    true
                ));
            }

            if (quest.destination != null && addedDestinations.Add(quest.destination))
            {
                Texture2D npcTexture = GetNpcMarkerTexture(quest.destination);

                markerBindings.Add(CreateMarker(
                    quest.destination.transform,
                    npcTexture,
                    quest.destination.DisplayName,
                    true
                ));

                if (font != null && compactUiSprite != null && transform is RectTransform screenRoot)
                {
                    nameplates[quest.destination] = QuestDestinationNameplate.Create(
                        quest.destination,
                        screenRoot,
                        font,
                        compactUiSprite
                    );
                }
            }
        }

        UpdateMarkerPositions();
    }

    private Texture2D GetNpcMarkerTexture(QuestDestination destination)
    {
        if (destination != null && destination.MarkerGender == QuestNpcGender.Female)
        {
            return femaleNpcMarkerTexture != null
                ? femaleNpcMarkerTexture
                : maleNpcMarkerTexture;
        }

        return maleNpcMarkerTexture != null
            ? maleNpcMarkerTexture
            : femaleNpcMarkerTexture;
    }

    private MarkerBinding CreateMarker(
        Transform worldTarget,
        Texture texture,
        string displayName,
        bool enableHoverName
    )
    {
        MarkerBinding binding = new() { worldTarget = worldTarget };

        if (fullMapContent != null)
        {
            binding.fullMapMarker = CreateRawMarker(
                fullMapContent,
                texture,
                fullMapMarkerSize,
                enableHoverName
            );

            if (enableHoverName && font != null && compactUiSprite != null)
            {
                QuestMapMarkerHover hover = binding.fullMapMarker.gameObject.AddComponent<QuestMapMarkerHover>();
                hover.Initialize(displayName, font, compactUiSprite);
            }
        }

        if (minimapOverlay != null)
        {
            binding.minimapMarker = CreateRawMarker(
                minimapOverlay,
                texture,
                minimapMarkerSize,
                false
            );
        }

        return binding;
    }

    private static RectTransform CreateRawMarker(
        RectTransform parent,
        Texture texture,
        float size,
        bool raycastTarget
    )
    {
        GameObject marker = new(
            "QuestMarker",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage)
        );
        marker.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * size;

        RawImage image = marker.GetComponent<RawImage>();
        image.texture = texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.raycastTarget = raycastTarget;

        return rect;
    }

    private void UpdateMarkerPositions()
    {
        foreach (MarkerBinding binding in markerBindings)
        {
            if (binding.worldTarget == null)
            {
                continue;
            }

            if (binding.fullMapMarker != null && fullMapContent != null)
            {
                binding.fullMapMarker.anchoredPosition = WorldToFullMapPosition(binding.worldTarget.position);
            }

            if (binding.minimapMarker != null && minimapImageRect != null && minimapCamera != null)
            {
                Vector3 viewport = minimapCamera.WorldToViewportPoint(binding.worldTarget.position);
                bool visible = viewport.z > 0f;

                binding.minimapMarker.gameObject.SetActive(visible);

                if (visible)
                {
                    Rect rect = minimapImageRect.rect;
                    Vector2 localPosition = new(
                        (viewport.x - 0.5f) * rect.width,
                        (viewport.y - 0.5f) * rect.height
                    );

                    float radius = Mathf.Max(
                        0f,
                        Mathf.Min(rect.width, rect.height) * 0.5f - minimapMarkerSize * 0.65f
                    );

                    if (localPosition.sqrMagnitude > radius * radius)
                    {
                        localPosition = localPosition.normalized * radius;
                    }

                    binding.minimapMarker.anchoredPosition = localPosition;
                }
            }
        }
    }

    private Vector2 WorldToFullMapPosition(Vector3 worldPosition)
    {
        if (fullMapProjection != null &&
            fullMapProjection.TryWorldToMapPosition(worldPosition, out Vector2 compositeMapPosition))
        {
            return compositeMapPosition;
        }

        float aspect = capturedMapSize.x / capturedMapSize.y;
        float halfHeight = mapOrthographicSize;
        float halfWidth = mapOrthographicSize * aspect;

        float normalizedX = Mathf.InverseLerp(
            mapWorldCenter.x - halfWidth,
            mapWorldCenter.x + halfWidth,
            worldPosition.x
        );
        float normalizedY = Mathf.InverseLerp(
            mapWorldCenter.y - halfHeight,
            mapWorldCenter.y + halfHeight,
            worldPosition.z
        );

        return new Vector2(
            (Mathf.Clamp01(normalizedX) - 0.5f) * fullMapContent.rect.width,
            (Mathf.Clamp01(normalizedY) - 0.5f) * fullMapContent.rect.height
        );
    }

    private void EnsureMinimapOverlay()
    {
        if (minimapOverlay != null || minimapImageRect == null)
        {
            return;
        }

        GameObject overlay = new("QuestMarkerOverlay", typeof(RectTransform));
        overlay.layer = LayerMask.NameToLayer("UI");
        minimapOverlay = overlay.GetComponent<RectTransform>();
        RectTransform overlayParent = minimapImageRect.parent as RectTransform;

        if (overlayParent == null)
        {
            Destroy(overlay);
            minimapOverlay = null;
            return;
        }

        minimapOverlay.SetParent(overlayParent, false);
        minimapOverlay.anchorMin = minimapImageRect.anchorMin;
        minimapOverlay.anchorMax = minimapImageRect.anchorMax;
        minimapOverlay.pivot = minimapImageRect.pivot;
        minimapOverlay.anchoredPosition = minimapImageRect.anchoredPosition;
        minimapOverlay.sizeDelta = minimapImageRect.sizeDelta;
        minimapOverlay.SetAsLastSibling();

        Transform playerArrow = overlayParent.Find("PlayerArrowUI");
        if (playerArrow != null)
        {
            playerArrow.SetAsLastSibling();
        }
    }

    private void EnsureLegend()
    {
        if (legend != null || fullMapContent == null || panelSprite == null || font == null)
        {
            return;
        }

        legend = new GameObject(
            "QuestMarkerLegend",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        legend.layer = LayerMask.NameToLayer("UI");

        RectTransform legendRect = legend.GetComponent<RectTransform>();
        legendRect.SetParent(fullMapContent, false);
        legendRect.anchorMin = new Vector2(0.32f, 0.78f);
        legendRect.anchorMax = new Vector2(0.98f, 0.98f);
        legendRect.pivot = new Vector2(0.5f, 0.5f);
        legendRect.anchoredPosition = Vector2.zero;
        legendRect.sizeDelta = Vector2.zero;
        legendRect.SetAsLastSibling();

        Image background = legend.GetComponent<Image>();
        background.sprite = panelSprite;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        TMP_Text title = CreateText(
            legendRect,
            "퀘스트 위치",
            new Vector2(0f, 60f),
            new Vector2(760f, 38f),
            26f,
            true
        );
        title.color = Color.black;

        CreateAcceptedQuestLegendRows(legendRect);
    }

    private void CreateAcceptedQuestLegendRows(RectTransform legendRect)
    {
        if (playerQuestList == null || playerQuestList.SelectedQuests.Count == 0)
        {
            TMP_Text emptyText = CreateText(
                legendRect,
                "수락한 퀘스트가 없습니다",
                Vector2.zero,
                new Vector2(760f, 42f),
                20f,
                false
            );
            emptyText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            return;
        }

        int questCount = Mathf.Min(playerQuestList.SelectedQuests.Count, 5);
        const float columnSpacing = 152f;
        float firstColumnX = -(questCount - 1) * columnSpacing * 0.5f;

        for (int i = 0; i < questCount; i++)
        {
            QuestRuntimeData quest = playerQuestList.SelectedQuests[i];

            if (quest == null)
            {
                continue;
            }

            float columnX = firstColumnX + i * columnSpacing;
            string questName = quest.questData != null &&
                               !string.IsNullOrWhiteSpace(quest.questData.displayName)
                ? quest.questData.displayName
                : $"퀘스트 {i + 1}";

            TMP_Text questTitle = CreateText(
                legendRect,
                $"{i + 1}. {questName}",
                new Vector2(columnX, 20f),
                new Vector2(142f, 30f),
                18f,
                true
            );
            questTitle.color = new Color(1f, 0.83f, 0.42f, 1f);

            Texture2D storeTexture = quest.questData != null && quest.questData.icon != null
                ? quest.questData.icon
                : quest.pickupPoint != null
                    ? quest.pickupPoint.RepresentativeIcon
                    : defaultStoreMarkerTexture;
            storeTexture ??= defaultStoreMarkerTexture;

            string pickupName = quest.pickupPoint != null
                ? quest.pickupPoint.DisplayName
                : "위치 없음";
            CreateHorizontalLegendRow(
                legendRect,
                $"픽업 {pickupName}",
                storeTexture,
                columnX,
                -18f
            );

            string destinationName = quest.destination != null
                ? quest.destination.DisplayName
                : "위치 없음";
            CreateHorizontalLegendRow(
                legendRect,
                $"배달 {destinationName}",
                GetNpcMarkerTexture(quest.destination),
                columnX,
                -58f
            );
        }
    }

    private void CreateHorizontalLegendRow(
        RectTransform parent,
        string label,
        Texture texture,
        float x,
        float y
    )
    {
        RectTransform icon = CreateRawMarker(parent, texture, 30f, false);
        icon.anchoredPosition = new Vector2(x - 54f, y);

        TMP_Text text = CreateText(
            parent,
            label,
            new Vector2(x + 18f, y),
            new Vector2(108f, 30f),
            14f,
            false
        );
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 14f;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private RawImage CreateLegendRow(
        RectTransform parent,
        string label,
        Texture texture,
        float y
    )
    {
        RectTransform icon = CreateRawMarker(parent, texture, 60f, false);
        icon.anchoredPosition = new Vector2(-85f, y);

        TMP_Text text = CreateText(
            parent,
            label,
            new Vector2(32f, y),
            new Vector2(170f, 46f),
            23f,
            false
        );
        text.alignment = TextAlignmentOptions.MidlineLeft;

        return icon.GetComponent<RawImage>();
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        bool bold
    )
    {
        GameObject textObject = new(
            "LegendText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private void ClearDynamicObjects()
    {
        foreach (MarkerBinding binding in markerBindings)
        {
            if (binding.fullMapMarker != null)
            {
                Destroy(binding.fullMapMarker.gameObject);
            }

            if (binding.minimapMarker != null)
            {
                Destroy(binding.minimapMarker.gameObject);
            }
        }

        markerBindings.Clear();

        foreach (QuestDestinationNameplate nameplate in nameplates.Values)
        {
            if (nameplate != null)
            {
                Destroy(nameplate.gameObject);
            }
        }

        nameplates.Clear();
    }

    private void DestroyLegend()
    {
        if (legend != null)
        {
            Destroy(legend);
        }

        legend = null;
    }

    private void DestroyMinimapOverlay()
    {
        if (minimapOverlay != null)
        {
            Destroy(minimapOverlay.gameObject);
        }

        minimapOverlay = null;
    }

    private static RectTransform FindSceneRectTransform(string objectName)
    {
        RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);

        foreach (RectTransform rect in rects)
        {
            if (rect.name == objectName)
            {
                return rect;
            }
        }

        return null;
    }

    private static RectTransform FindDescendant(RectTransform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == objectName)
            {
                return rect;
            }
        }

        return null;
    }

    private static RawImage FindRenderTextureImage(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
        {
            if (image.texture is RenderTexture)
            {
                return image;
            }
        }

        return null;
    }

    private static Camera FindCameraForTexture(Texture texture)
    {
        if (texture == null)
        {
            return null;
        }

        foreach (Camera cameraComponent in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (cameraComponent.targetTexture == texture)
            {
                return cameraComponent;
            }
        }

        return null;
    }

    private void UpdateNameplateVisibilityForMap()
    {
        bool fullMapOpen = fullMapRoot != null && fullMapRoot.gameObject.activeInHierarchy;

        foreach (QuestDestinationNameplate nameplate in nameplates.Values)
        {
            if (nameplate != null)
            {
                nameplate.gameObject.SetActive(!fullMapOpen);
            }
        }
    }
}
