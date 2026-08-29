using UnityEngine;

public class PlayerMapMarker : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Map")]
    [SerializeField] private RectTransform mapContent;

    [Header("Captured Map")]
    [SerializeField] private float mapWidth = 1280f;
    [SerializeField] private float mapHeight = 720f;

    [Header("Capture Camera World Center")]
    [SerializeField]
    private Vector2 mapWorldCenter =
        new Vector2(-371f, 10f);

    [Header("Capture Camera")]
    [SerializeField] private float orthographicSize = 300f;

    [Header("락트소어 사무실")]
    [SerializeField] private string locktStoreMapObjectName = "IndoorOffice";
    [SerializeField] private Vector2 locktStoreWorldCenter = new(-200f, 2779.27f);
    [SerializeField] private Vector2 locktStoreWorldSize = new(37.7f, 32.3f);

    [Header("신쥬 사무실")]
    [SerializeField] private string shinjuMapObjectName = "IndoorOffice1";
    [SerializeField] private Vector2 shinjuWorldCenter = new(-200f, 2758.95f);
    [SerializeField] private Vector2 shinjuWorldSize = new(34.5f, 25.9f);

    private RectTransform markerRect;
    private RectTransform locktStoreMapRect;
    private RectTransform shinjuMapRect;

    private void Awake()
    {
        markerRect = GetComponent<RectTransform>();
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (player == null || mapContent == null)
            return;

        UpdateMarkerPosition();
    }

    private void UpdateMarkerPosition()
    {
        if (!TryWorldToMapPosition(player.position, out Vector2 mapPosition))
            return;

        markerRect.anchoredPosition = mapPosition;
        markerRect.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }

    public bool TryWorldToMapPosition(Vector3 worldPosition, out Vector2 mapPosition)
    {
        ResolveReferences();

        if (mapContent == null)
        {
            mapPosition = default;
            return false;
        }

        bool insideLocktStore =
            IsInsideWorldRegion(worldPosition, locktStoreWorldCenter, locktStoreWorldSize);
        bool insideShinju =
            IsInsideWorldRegion(worldPosition, shinjuWorldCenter, shinjuWorldSize);

        if (insideLocktStore && insideShinju)
        {
            float locktStoreDistance = GetNormalizedRegionDistance(
                worldPosition,
                locktStoreWorldCenter,
                locktStoreWorldSize
            );
            float shinjuDistance = GetNormalizedRegionDistance(
                worldPosition,
                shinjuWorldCenter,
                shinjuWorldSize
            );

            insideLocktStore = locktStoreDistance <= shinjuDistance;
            insideShinju = !insideLocktStore;
        }

        if (insideLocktStore &&
            TryMapToRect(worldPosition, locktStoreWorldCenter, locktStoreWorldSize, locktStoreMapRect, out mapPosition))
        {
            return true;
        }

        if (insideShinju &&
            TryMapToRect(worldPosition, shinjuWorldCenter, shinjuWorldSize, shinjuMapRect, out mapPosition))
        {
            return true;
        }

        float aspect = mapWidth / mapHeight;
        float worldHalfHeight = orthographicSize;
        float worldHalfWidth = orthographicSize * aspect;

        float normalizedX = Mathf.InverseLerp(
            mapWorldCenter.x - worldHalfWidth,
            mapWorldCenter.x + worldHalfWidth,
            worldPosition.x
        );
        float normalizedY = Mathf.InverseLerp(
            mapWorldCenter.y - worldHalfHeight,
            mapWorldCenter.y + worldHalfHeight,
            worldPosition.z
        );

        mapPosition = new Vector2(
            (Mathf.Clamp01(normalizedX) - 0.5f) * mapContent.rect.width,
            (Mathf.Clamp01(normalizedY) - 0.5f) * mapContent.rect.height
        );
        return true;
    }

    private void ResolveReferences()
    {
        if (mapContent == null)
            mapContent = transform.parent as RectTransform;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (mapContent == null)
            return;

        locktStoreMapRect ??= FindDescendant(mapContent, locktStoreMapObjectName);
        shinjuMapRect ??= FindDescendant(mapContent, shinjuMapObjectName);
    }

    private bool TryMapToRect(
        Vector3 worldPosition,
        Vector2 worldCenter,
        Vector2 worldSize,
        RectTransform targetRect,
        out Vector2 mapPosition
    )
    {
        if (targetRect == null || worldSize.x <= 0f || worldSize.y <= 0f)
        {
            mapPosition = default;
            return false;
        }

        float normalizedX = Mathf.InverseLerp(
            worldCenter.x - worldSize.x * 0.5f,
            worldCenter.x + worldSize.x * 0.5f,
            worldPosition.x
        );
        float normalizedY = Mathf.InverseLerp(
            worldCenter.y - worldSize.y * 0.5f,
            worldCenter.y + worldSize.y * 0.5f,
            worldPosition.z
        );

        Vector3 targetLocalPosition = new(
            (Mathf.Clamp01(normalizedX) - 0.5f) * targetRect.rect.width,
            (Mathf.Clamp01(normalizedY) - 0.5f) * targetRect.rect.height,
            0f
        );
        Vector3 worldUiPosition = targetRect.TransformPoint(targetLocalPosition);
        mapPosition = mapContent.InverseTransformPoint(worldUiPosition);
        return true;
    }

    private static bool IsInsideWorldRegion(
        Vector3 worldPosition,
        Vector2 worldCenter,
        Vector2 worldSize
    )
    {
        return Mathf.Abs(worldPosition.x - worldCenter.x) <= worldSize.x * 0.5f &&
               Mathf.Abs(worldPosition.z - worldCenter.y) <= worldSize.y * 0.5f;
    }

    private static float GetNormalizedRegionDistance(
        Vector3 worldPosition,
        Vector2 worldCenter,
        Vector2 worldSize
    )
    {
        float normalizedX = (worldPosition.x - worldCenter.x) / Mathf.Max(worldSize.x, 0.001f);
        float normalizedY = (worldPosition.z - worldCenter.y) / Mathf.Max(worldSize.y, 0.001f);
        return normalizedX * normalizedX + normalizedY * normalizedY;
    }

    private static RectTransform FindDescendant(RectTransform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == objectName)
                return rect;
        }

        return null;
    }
}
