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
    private Vector3 baseScale;
    private Vector2 basePosition;

    private void Start()
    {
        currentZoom = 1f;
        basePosition = mapContent.anchoredPosition;
        baseScale = mapContent.localScale;
        //mapContent.localScale = Vector3.one;
        mapContent.localScale = new Vector3(baseScale.x * currentZoom, baseScale.y * currentZoom, baseScale.z);
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

        float newZoom = currentZoom + (eventData.scrollDelta.y > 0f ? zoomSpeed : -zoomSpeed);
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

        if (Mathf.Approximately(currentZoom, newZoom)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapContent, eventData.position, eventData.pressEventCamera, out Vector2 contentPoint);

        Vector3 beforeWorld = mapContent.TransformPoint(contentPoint);

        currentZoom = newZoom;
        mapContent.localScale = new Vector3(baseScale.x * currentZoom, baseScale.y * currentZoom, baseScale.z);

        Vector3 afterWorld = mapContent.TransformPoint(contentPoint);

        Vector3 before = mapWindow.InverseTransformPoint(beforeWorld);
        Vector3 after = mapWindow.InverseTransformPoint(afterWorld);

        mapContent.anchoredPosition += new Vector2(before.x - after.x, before.y - after.y);

        if (Mathf.Approximately(currentZoom, minZoom))
            mapContent.anchoredPosition = basePosition;

        ClampPosition();
    }

    private void ClampPosition()
    {
        float windowWidth = mapWindow.rect.width;
        float windowHeight = mapWindow.rect.height;

        float contentWidth = mapContent.rect.width * Mathf.Abs(mapContent.localScale.x);
        float contentHeight = mapContent.rect.height * Mathf.Abs(mapContent.localScale.y);

        float maxX = Mathf.Max(0f, (contentWidth - windowWidth) * 0.5f);
        float maxY = Mathf.Max(0f, (contentHeight - windowHeight) * 0.5f);

        Vector2 pos = mapContent.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, basePosition.x - maxX, basePosition.x + maxX);
        pos.y = Mathf.Clamp(pos.y, basePosition.y - maxY, basePosition.y + maxY);
        mapContent.anchoredPosition = pos;
    }
}