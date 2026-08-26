using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NpcQuestUIPrefabBuilder
{
    private const string PrefabPath = "Assets/JEC/UI/NPC/Resources/NpcQuestUI.prefab";
    private const string PanelSpritePath = "Assets/JEC/UI/Source/panel_popup.png";
    private const string CompactUiSpritePath = "Assets/JEC/UI/Source/button_normal.png";
    private const string FontPath = "Assets/KHB/UI/Font/NanumBarunGothic SDF.asset";
    private const string MaleNpcMarkerPath = "Assets/JEC/UI/Map/Resources/Img_NpcMale.png";
    private const string FemaleNpcMarkerPath = "Assets/JEC/UI/Map/Resources/Img_NpcFemale.png";
    private const string DefaultStoreMarkerPath = "Assets/JEC/UI/Icon/Img_BurgerComboMeal.png";
    private const int PrefabBuildVersion = 3;
    private const string PrefabBuildVersionKey = "DeliverySimulator.NpcQuestUIPrefabBuilder.Version";

    [InitializeOnLoadMethod]
    private static void ScheduleInitialBuild()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    [MenuItem("Tools/Delivery Simulator/NPC Quest UI/Create Or Update Prefab")]
    public static void CreateOrUpdatePrefab()
    {
        BuildPrefab();
        EditorPrefs.SetInt(PrefabBuildVersionKey, PrefabBuildVersion);
    }

    private static void BuildIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildIfMissing;
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null ||
            EditorPrefs.GetInt(PrefabBuildVersionKey, -1) < PrefabBuildVersion)
        {
            BuildPrefab();
            EditorPrefs.SetInt(PrefabBuildVersionKey, PrefabBuildVersion);
        }
    }

    private static void BuildPrefab()
    {
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        Sprite compactUiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CompactUiSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Texture2D maleNpcMarker = AssetDatabase.LoadAssetAtPath<Texture2D>(MaleNpcMarkerPath);
        Texture2D femaleNpcMarker = AssetDatabase.LoadAssetAtPath<Texture2D>(FemaleNpcMarkerPath);
        Texture2D defaultStoreMarker = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultStoreMarkerPath);

        if (panelSprite == null || compactUiSprite == null || font == null ||
            maleNpcMarker == null || femaleNpcMarker == null || defaultStoreMarker == null)
        {
            Debug.LogError("[NpcQuestUIPrefabBuilder] UI 배경, 폰트 또는 마커 이미지를 찾지 못했습니다.");
            return;
        }

        GameObject root = new(
            "NpcQuestUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(NpcQuestUIController),
            typeof(QuestMapMarkerController)
        );

        root.layer = LayerMask.NameToLayer("UI");

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject promptPanel = CreatePanel(
            "InteractionPrompt",
            rootRect,
            compactUiSprite,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -76f),
            new Vector2(680f, 122f)
        );
        CanvasGroup promptGroup = promptPanel.AddComponent<CanvasGroup>();
        TMP_Text promptText = CreateText(
            "PromptText",
            promptPanel.GetComponent<RectTransform>(),
            font,
            "<color=#FFD36A>F키</color>를 누르면 물건을 받을 수 있습니다",
            29f,
            FontStyles.Normal,
            Color.white
        );
        SetStretch(promptText.rectTransform, 38f, 20f);

        GameObject completionPanel = CreatePanel(
            "QuestCompletion",
            rootRect,
            panelSprite,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -190f),
            new Vector2(600f, 300f)
        );
        CanvasGroup completionGroup = completionPanel.AddComponent<CanvasGroup>();
        RectTransform completionRect = completionPanel.GetComponent<RectTransform>();

        TMP_Text titleText = CreateText(
            "CompletionTitle",
            completionRect,
            font,
            "전달 완료!",
            46f,
            FontStyles.Bold,
            new Color(1f, 0.83f, 0.42f, 1f)
        );
        SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 82f), new Vector2(500f, 58f));

        TMP_Text npcNameText = CreateText(
            "NpcName",
            completionRect,
            font,
            "NPC 이름",
            36f,
            FontStyles.Bold,
            Color.white
        );
        SetAnchoredRect(npcNameText.rectTransform, new Vector2(0f, 18f), new Vector2(500f, 52f));

        TMP_Text bodyText = CreateText(
            "CompletionBody",
            completionRect,
            font,
            "물건 전달을 완료했습니다",
            27f,
            FontStyles.Normal,
            new Color(0.86f, 0.86f, 0.82f, 1f)
        );
        SetAnchoredRect(bodyText.rectTransform, new Vector2(0f, -48f), new Vector2(500f, 46f));

        NpcQuestUIController controller = root.GetComponent<NpcQuestUIController>();
        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("interactionGroup").objectReferenceValue = promptGroup;
        serializedController.FindProperty("interactionText").objectReferenceValue = promptText;
        serializedController.FindProperty("completionGroup").objectReferenceValue = completionGroup;
        serializedController.FindProperty("completionTitleText").objectReferenceValue = titleText;
        serializedController.FindProperty("npcNameText").objectReferenceValue = npcNameText;
        serializedController.FindProperty("completionBodyText").objectReferenceValue = bodyText;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        QuestMapMarkerController markerController = root.GetComponent<QuestMapMarkerController>();
        SerializedObject serializedMarkerController = new(markerController);
        serializedMarkerController.FindProperty("maleNpcMarkerTexture").objectReferenceValue = maleNpcMarker;
        serializedMarkerController.FindProperty("femaleNpcMarkerTexture").objectReferenceValue = femaleNpcMarker;
        serializedMarkerController.FindProperty("defaultStoreMarkerTexture").objectReferenceValue = defaultStoreMarker;
        serializedMarkerController.FindProperty("compactUiSprite").objectReferenceValue = compactUiSprite;
        serializedMarkerController.FindProperty("panelSprite").objectReferenceValue = panelSprite;
        serializedMarkerController.FindProperty("font").objectReferenceValue = font;
        serializedMarkerController.ApplyModifiedPropertiesWithoutUndo();

        promptGroup.alpha = 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
        completionGroup.alpha = 0f;
        completionGroup.interactable = false;
        completionGroup.blocksRaycasts = false;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[NpcQuestUIPrefabBuilder] 생성 완료: {PrefabPath}");
    }

    private static GameObject CreatePanel(
        string name,
        RectTransform parent,
        Sprite sprite,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = parent.gameObject.layer;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        return panel;
    }

    private static TMP_Text CreateText(
        string name,
        RectTransform parent,
        TMP_FontAsset font,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color
    )
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.richText = true;

        return text;
    }

    private static void SetStretch(RectTransform rect, float horizontalPadding, float verticalPadding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
