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

    private RectTransform markerRect;

    private void Awake()
    {
        markerRect = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (player == null || mapContent == null)
            return;

        UpdateMarkerPosition();
    }

    private void UpdateMarkerPosition()
    {
        Vector3 playerWorldPos = player.position;

 
        float aspect = mapWidth / mapHeight;

        float worldHalfHeight = orthographicSize;
        float worldHalfWidth = orthographicSize * aspect;

        float worldMinX =
            mapWorldCenter.x - worldHalfWidth;

        float worldMaxX =
            mapWorldCenter.x + worldHalfWidth;

        float worldMinZ =
            mapWorldCenter.y - worldHalfHeight;

        float worldMaxZ =
            mapWorldCenter.y + worldHalfHeight;

       
        float normalizedX = Mathf.InverseLerp(
            worldMinX,
            worldMaxX,
            playerWorldPos.x
        );

        float normalizedY = Mathf.InverseLerp(
            worldMinZ,
            worldMaxZ,
            playerWorldPos.z
        );


        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        float contentWidth = mapContent.rect.width;
        float contentHeight = mapContent.rect.height;

        float uiX =
            (normalizedX - 0.5f) * contentWidth;

        float uiY =
            (normalizedY - 0.5f) * contentHeight;

        markerRect.anchoredPosition =
            new Vector2(uiX, uiY);
    }
}