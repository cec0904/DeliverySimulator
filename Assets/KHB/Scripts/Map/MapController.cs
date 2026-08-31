using UnityEngine;
using UnityEngine.EventSystems;

public class MapController : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
{
    [Header("References")]
    [SerializeField] private RectTransform mapWindow;
    [SerializeField] private RectTransform mapContent;

    [Header("Map Regions")]
    [SerializeField] private RectTransform exteriorMap;
    [SerializeField] private RectTransform locktStoreMap;
    [SerializeField] private RectTransform shinjuMap;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 3f;
    [SerializeField] private float zoomSpeed = 0.2f;

    private readonly Vector3[] worldCorners = new Vector3[4];
    private float currentZoom = 1f;
    private Vector2 lastPointerPosition;
    private Vector3 baseScale;
    private Vector2 basePosition;
    private RectTransform activeMap;
    private bool initialized;

    public float CurrentZoom => currentZoom;

    private void Start()
    {
        EnsureInitialized();
        ResetView();
    }

    private void OnDisable()
    {
        if (initialized)
            ResetView();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!EnsureInitialized())
            return;

        if (activeMap == null)
            activeMap = FindMapAtPointer(eventData.position, eventData.pressEventCamera);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapWindow,
            eventData.position,
            eventData.pressEventCamera,
            out lastPointerPosition
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!EnsureInitialized() || activeMap == null || currentZoom <= minZoom)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapWindow,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentPointerPosition
        );

        mapContent.anchoredPosition += currentPointerPosition - lastPointerPosition;
        lastPointerPosition = currentPointerPosition;
        ClampPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!EnsureInitialized() || Mathf.Approximately(eventData.scrollDelta.y, 0f))
            return;

        bool zoomingIn = eventData.scrollDelta.y > 0f;
        if (zoomingIn && activeMap == null)
            activeMap = FindMapAtPointer(eventData.position, eventData.pressEventCamera);

        if (activeMap == null)
            return;

        float newZoom = currentZoom + (zoomingIn ? zoomSpeed : -zoomSpeed);
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

        if (Mathf.Approximately(currentZoom, newZoom))
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapContent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 contentPoint
        );

        Vector3 pointerWorldPosition = mapContent.TransformPoint(contentPoint);

        currentZoom = newZoom;
        mapContent.localScale = new Vector3(
            baseScale.x * currentZoom,
            baseScale.y * currentZoom,
            baseScale.z
        );

        Vector3 scaledPointerWorldPosition = mapContent.TransformPoint(contentPoint);
        Vector3 pointerBefore = mapWindow.InverseTransformPoint(pointerWorldPosition);
        Vector3 pointerAfter = mapWindow.InverseTransformPoint(scaledPointerWorldPosition);
        mapContent.anchoredPosition += new Vector2(
            pointerBefore.x - pointerAfter.x,
            pointerBefore.y - pointerAfter.y
        );

        if (Mathf.Approximately(currentZoom, minZoom))
        {
            ResetView();
            return;
        }

        ClampPosition();
    }

    private bool EnsureInitialized()
    {
        if (initialized)
            return mapWindow != null && mapContent != null;

        mapContent ??= transform as RectTransform;
        mapWindow ??= mapContent != null ? mapContent.parent as RectTransform : null;

        if (mapWindow == null || mapContent == null)
            return false;

        exteriorMap ??= FindDescendant(mapContent, "Map_Image");
        locktStoreMap ??= FindDescendant(mapContent, "IndoorOffice");
        shinjuMap ??= FindDescendant(mapContent, "IndoorOffice1");

        basePosition = mapContent.anchoredPosition;
        baseScale = mapContent.localScale;
        currentZoom = minZoom;
        initialized = true;
        return true;
    }

    private RectTransform FindMapAtPointer(Vector2 screenPoint, Camera eventCamera)
    {
        // The office images overlap the exterior map's rectangle, so test them first.
        if (ContainsPointer(locktStoreMap, screenPoint, eventCamera))
            return locktStoreMap;

        if (ContainsPointer(shinjuMap, screenPoint, eventCamera))
            return shinjuMap;

        return ContainsPointer(exteriorMap, screenPoint, eventCamera) ? exteriorMap : null;
    }

    private static bool ContainsPointer(
        RectTransform rect,
        Vector2 screenPoint,
        Camera eventCamera
    )
    {
        return rect != null &&
               rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, eventCamera);
    }

    private void ResetView()
    {
        if (mapContent == null)
            return;

        currentZoom = minZoom;
        mapContent.localScale = baseScale;
        mapContent.anchoredPosition = basePosition;
        activeMap = null;
    }

    private void ClampPosition()
    {
        if (mapWindow == null || mapContent == null)
            return;

        Rect windowRect = mapWindow.rect;
        Rect contentBounds = GetBoundsInWindow(mapContent);
        Rect activeBounds = activeMap != null
            ? GetBoundsInWindow(activeMap)
            : contentBounds;

        Rect horizontalBounds = activeBounds.width >= windowRect.width
            ? activeBounds
            : contentBounds;
        Rect verticalBounds = activeBounds.height >= windowRect.height
            ? activeBounds
            : contentBounds;

        Vector2 correction = new(
            GetAxisCorrection(
                horizontalBounds.xMin,
                horizontalBounds.xMax,
                windowRect.xMin,
                windowRect.xMax
            ),
            GetAxisCorrection(
                verticalBounds.yMin,
                verticalBounds.yMax,
                windowRect.yMin,
                windowRect.yMax
            )
        );

        mapContent.anchoredPosition += correction;
    }

    private Rect GetBoundsInWindow(RectTransform target)
    {
        target.GetWorldCorners(worldCorners);

        Vector3 firstCorner = mapWindow.InverseTransformPoint(worldCorners[0]);
        float minX = firstCorner.x;
        float maxX = firstCorner.x;
        float minY = firstCorner.y;
        float maxY = firstCorner.y;

        for (int index = 1; index < worldCorners.Length; index++)
        {
            Vector3 corner = mapWindow.InverseTransformPoint(worldCorners[index]);
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static float GetAxisCorrection(
        float contentMin,
        float contentMax,
        float windowMin,
        float windowMax
    )
    {
        if (contentMin > windowMin)
            return windowMin - contentMin;

        if (contentMax < windowMax)
            return windowMax - contentMax;

        return 0f;
    }

    private static RectTransform FindDescendant(RectTransform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == objectName)
                return rect;
        }

        return null;
    }
}
