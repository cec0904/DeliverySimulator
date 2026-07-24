using UnityEngine;
using UnityEngine.EventSystems;

public class MapUIController : MonoBehaviour, IPointerClickHandler
{
    [Header("스크립트 참조")]
    public WorldPathVisualizer pathVisualizer;

    [Header("맵 영역 정의 (World Coordinates)")]
    public Vector2 worldMinBounds = new Vector2(-100f, -100f);
    public Vector2 worldMaxBounds = new Vector2(100f, 100f);

    private RectTransform mapRectTransform;

    void Awake()
    {
        mapRectTransform = GetComponent<RectTransform>();
        if (mapRectTransform == null)
        {
            Debug.LogError("🚨 RectTransform을 찾을 수 없습니다!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("🎯 1. Map_Image 클릭 이벤트 들어옴!");

        if (mapRectTransform == null) mapRectTransform = GetComponent<RectTransform>();

        Camera uiCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;

        Vector2 localPoint;
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRectTransform,
            eventData.position,
            uiCamera,
            out localPoint
        );

        if (isInside)
        {
            Rect rect = mapRectTransform.rect;

            // 💡 맵 영역을 벗어나는 좌표를 Image Rect 범위 내로 가둡니다.
            float clampedX = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
            float clampedY = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);

            // 💡 InverseLerp 후 Clamp01로 0~1 사이의 정확한 비율로 만듭니다.
            float normalizedX = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, clampedX));
            float normalizedY = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, clampedY));

            float worldX = Mathf.Lerp(worldMinBounds.x, worldMaxBounds.x, normalizedX);
            float worldZ = Mathf.Lerp(worldMinBounds.y, worldMaxBounds.y, normalizedY);

            Vector3 rawWorldPos = new Vector3(worldX, 0f, worldZ);
            Vector3 targetWorldPos = GetValidWorldPosition(rawWorldPos);

            if (pathVisualizer != null)
            {
                pathVisualizer.SetDestination(targetWorldPos);
            }
        }
    }

    private Vector3 GetValidWorldPosition(Vector3 rawPos)
    {

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(rawPos, out hit, 50.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            Debug.Log($"[NavMesh] 도로 찾기 성공! 변환 좌표: {hit.position}");
            return hit.position;
        }
        Debug.LogWarning($"[NavMesh] 클릭 위치 근처에서 도로를 찾지 못했습니다: {rawPos}");
        return rawPos;
    }
}