using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragController : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 dragOffset;

    private bool isDraggingAllowed = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // UI가 올라가 있는 최상위 Canvas를 찾습니다.
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GameObject clickedObject = eventData.pointerEnter;

        if (clickedObject != null)
        {
            Button button = clickedObject.GetComponentInParent<Button>();

            if (button != null)
            {
                isDraggingAllowed = false;
                Debug.Log($"버튼 '{button.name}' 영역을 클릭하여 드래그를 차단합니다.");
                return;
            }
        }

        // 2. 버튼이 아니라면 드래그 허용 및 오프셋 계산
        isDraggingAllowed = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset
        );
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        if (!isDraggingAllowed) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition))
        {

            rectTransform.anchoredPosition = localPointerPosition - dragOffset;
        }
    }
}
