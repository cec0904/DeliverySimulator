using UnityEngine;
using UnityEngine.EventSystems;

public class MapController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IScrollHandler
{
    [Header("References")]
    [SerializeField] private RectTransform mapWindow;
    [SerializeField] private RectTransform mapContent;

    [Header("Captured Map Size")]
    [SerializeField] private float mapWidth = 1280f;
    [SerializeField] private float mapHeight = 720f;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float zoomSpeed = 0.2f;

    private float currentZoom = 1f;

    public float CurrentZoom => currentZoom;

    private Vector2 lastMousePosition;

    private float baseWidth;
    private float baseHeight;

    private void Start()
    {
        SetupBaseSize();
        UpdateMapSize();
    }

    /// <summary>
    /// Zoom 1일 때 실제 Window 크기를 기준으로
    /// Map_Content의 기본 크기를 결정
    /// </summary>
    private void SetupBaseSize()
    {
        float windowWidth = mapWindow.rect.width;
        float windowHeight = mapWindow.rect.height;

        float mapAspect = mapWidth / mapHeight;

        // Window가 16:9라고 가정하되,
        // 실제 Window 안에 지도 전체가 들어가도록 Fit
        float width = windowWidth;
        float height = width / mapAspect;

        if (height > windowHeight)
        {
            height = windowHeight;
            width = height * mapAspect;
        }

        baseWidth = width;
        baseHeight = height;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        lastMousePosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastMousePosition;

        mapContent.anchoredPosition += delta;

        lastMousePosition = eventData.position;

        ClampPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Approximately(eventData.scrollDelta.y, 0f))
            return;

        Vector2 mouseLocalPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapWindow,
            eventData.position,
            eventData.pressEventCamera,
            out mouseLocalPosition
        );

        Vector2 contentLocalPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapContent,
            eventData.position,
            eventData.pressEventCamera,
            out contentLocalPosition
        );

        Vector2 oldSize = mapContent.rect.size;

        // 마우스가 현재 지도에서 어느 위치를 가리키고 있는지
        Vector2 normalizedPosition = new Vector2(
            (contentLocalPosition.x / oldSize.x) + 0.5f,
            (contentLocalPosition.y / oldSize.y) + 0.5f
        );

        // Zoom
        if (eventData.scrollDelta.y > 0f)
        {
            currentZoom += zoomSpeed;
        }
        else
        {
            currentZoom -= zoomSpeed;
        }

        currentZoom = Mathf.Clamp(
            currentZoom,
            minZoom,
            maxZoom
        );

        UpdateMapSize();

        Vector2 newSize = mapContent.rect.size;

        Vector2 newContentLocalPosition = new Vector2(
            (normalizedPosition.x - 0.5f) * newSize.x,
            (normalizedPosition.y - 0.5f) * newSize.y
        );

        // 마우스가 가리키던 지도 위치를
        // 같은 화면 위치에 유지
        mapContent.anchoredPosition =
            mouseLocalPosition - newContentLocalPosition;

        ClampPosition();
    }

    private void UpdateMapSize()
    {
        float width = baseWidth * currentZoom;
        float height = baseHeight * currentZoom;

        mapContent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );

        mapContent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );

        ClampPosition();
    }

    private void ClampPosition()
    {
        float windowWidth = mapWindow.rect.width;
        float windowHeight = mapWindow.rect.height;

        float contentWidth = mapContent.rect.width;
        float contentHeight = mapContent.rect.height;

        float maxX = Mathf.Max(
            0f,
            (contentWidth - windowWidth) / 2f
        );

        float maxY = Mathf.Max(
            0f,
            (contentHeight - windowHeight) / 2f
        );

        Vector2 pos = mapContent.anchoredPosition;

        pos.x = Mathf.Clamp(
            pos.x,
            -maxX,
            maxX
        );

        pos.y = Mathf.Clamp(
            pos.y,
            -maxY,
            maxY
        );

        mapContent.anchoredPosition = pos;
    }
}