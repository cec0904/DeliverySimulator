using UnityEngine;
using UnityEngine.EventSystems;

public class MapController : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
{
    [Header("References")]
    [SerializeField] private RectTransform mapWindow;
    [SerializeField] private RectTransform mapContent;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float zoomSpeed = 0.2f;

    private float currentZoom = 1f;
    private Vector2 lastPointerPosition;

    public float CurrentZoom => currentZoom;

    private void Start()
    {
        currentZoom = 1f;
        mapContent.localScale = Vector3.one;
        ClampPosition();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapWindow, eventData.position, eventData.pressEventCamera, out lastPointerPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapWindow, eventData.position, eventData.pressEventCamera, out Vector2 currentPointerPosition);

        Vector2 delta = currentPointerPosition - lastPointerPosition;
        mapContent.anchoredPosition += delta;
        lastPointerPosition = currentPointerPosition;

        ClampPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;

        float oldZoom = currentZoom;
        currentZoom += eventData.scrollDelta.y > 0f ? zoomSpeed : -zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        if (Mathf.Approximately(oldZoom, currentZoom)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapWindow, eventData.position, eventData.pressEventCamera, out Vector2 mousePosition);

        Vector2 oldContentPosition = mapContent.anchoredPosition;
        float zoomRatio = currentZoom / oldZoom;

        mapContent.localScale = Vector3.one * currentZoom;
        mapContent.anchoredPosition = mousePosition - (mousePosition - oldContentPosition) * zoomRatio;

        ClampPosition();
    }

    private void ClampPosition()
    {
        float windowWidth = mapWindow.rect.width;
        float windowHeight = mapWindow.rect.height;

        float contentWidth = mapContent.rect.width * currentZoom;
        float contentHeight = mapContent.rect.height * currentZoom;

        float maxX = Mathf.Max(0f, (contentWidth - windowWidth) * 0.5f);
        float maxY = Mathf.Max(0f, (contentHeight - windowHeight) * 0.5f);

        Vector2 pos = mapContent.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);

        mapContent.anchoredPosition = pos;
    }
}