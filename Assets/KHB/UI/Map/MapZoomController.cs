using UnityEngine;
using UnityEngine.EventSystems;

public class MapZoomController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("[ Zoom 대상 ]")]
    [SerializeField] private RectTransform targetMapRect;

    [Header("[ Zoom 설정 ]")]
    [SerializeField] private float zoomSpeed = 0.5f;     // 휠 속도
    [SerializeField] private float minScale = 1.0f;     // 최소 축소 비율 (기본 크기)
    [SerializeField] private float maxScale = 3.0f;     // 최대 확대 비율 (3배)

    private bool isHovered = false; // 마우스가 지도 창 위에 있는지 여부

    private void Update()
    {
        if (!isHovered || targetMapRect == null) return;

        // 마우스 휠 입력 받기 (위: +, 아래: -)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            ZoomMap(scrollInput);
        }
    }

    private void ZoomMap(float increment)
    {
        // 현재 Scale 가져오기
        Vector3 currentScale = targetMapRect.localScale;

        // 새로운 Scale 계산
        float newScaleX = currentScale.x + (increment * zoomSpeed);
        float newScaleY = currentScale.y + (increment * zoomSpeed);

        // Min/Max 범위를 넘지 않도록 제한 (Clamping)
        newScaleX = Mathf.Clamp(newScaleX, minScale, maxScale);
        newScaleY = Mathf.Clamp(newScaleY, minScale, maxScale);

        // Scale 적용
        targetMapRect.localScale = new Vector3(newScaleX, newScaleY, 1f);
    }

    // 마우스가 지도 창 내부로 들어왔을 때만 휠 작동
    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}
