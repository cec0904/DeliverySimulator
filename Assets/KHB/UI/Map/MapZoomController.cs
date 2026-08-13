using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapZoomController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("[ 필수 연결 ]")]
    [SerializeField] private RectTransform mapContentRect; // 자기 자신 (Map_Content)
    [SerializeField] private ScrollRect scrollRect;        // 부모 (Map_Window의 ScrollRect)

    [Header("[ Zoom 설정 ]")]
    [SerializeField] private float zoomSpeed = 0.3f;       // 줌 감도
    [SerializeField] private float minZoom = 1.0f;        // 최소 줌 (1.0 = 창 크기 딱 맞춤)
    [SerializeField] private float maxZoom = 3.0f;        // 최대 줌 (3배 확대)

    private Vector2 defaultSize;
    private float currentZoom = 1.0f;
    private bool isHovered = false;

    private void Awake()
    {
        if (mapContentRect == null)
            mapContentRect = GetComponent<RectTransform>();

        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();
    }

    private void Start()
    {
        if (scrollRect != null)
        {
            // ScrollRect 자체의 휠 스크롤 감도를 0으로 만들어 줌인과 이동 꼬임 방지
            scrollRect.scrollSensitivity = 0f;
        }

        // Anchor와 Pivot을 Center(0.5, 0.5)로 고정
        mapContentRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapContentRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRect.pivot = new Vector2(0.5f, 0.5f);
        mapContentRect.anchoredPosition = Vector2.zero;

        // 초기 16:9 기본 크기 저장 (1280x720)
        defaultSize = mapContentRect.sizeDelta;

        ApplyZoom();
    }

    private void Update()
    {
        if (!isHovered) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            Zoom(scrollInput);
        }
    }

    private void Zoom(float increment)
    {
        currentZoom += increment * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (mapContentRect == null) return;

        // 줌 비율에 맞춰 Map_Content 크기 확장
        mapContentRect.sizeDelta = defaultSize * currentZoom;

        // ScrollRect에 크기가 변했음을 알려 드래그 가능 범위 재계산
        if (scrollRect != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(scrollRect.GetComponent<RectTransform>());
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}