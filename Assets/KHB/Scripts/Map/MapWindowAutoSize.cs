using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MapWindowAutoSize : MonoBehaviour
{
    [Header("Screen Ratio")]
    [Range(0.1f, 1f)]
    [SerializeField] private float screenRatio = 1f;

    private RectTransform windowRect;
    private RectTransform canvasRect;

    private void Awake()
    {
        windowRect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateSize();
    }

    private void UpdateSize()
    {
        if (canvasRect == null)
            return;

        // Canvas의 기준 UI 크기
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // 72%
        float width = canvasWidth * screenRatio;
        float height = canvasHeight * screenRatio;

        // 16:9 유지
        float aspect = 16f / 9f;

        height = width / aspect;

        // 세로가 더 크면 세로 기준
        if (height > canvasHeight * screenRatio)
        {
            height = canvasHeight * screenRatio;
            width = height * aspect;
        }

        windowRect.sizeDelta = new Vector2(width, height);
        windowRect.anchoredPosition = Vector2.zero;
    }
}