using UnityEngine;

public class UIWindowCloser : MonoBehaviour
{
    public GameObject targetWindow;
    void Awake()
    {
        // 만약 대상 창을 지정하지 않았다면, 스크립트가 붙은 버튼의 부모 창을 자동으로 찾습니다.
        if (targetWindow == null)
        {
            targetWindow = transform.parent != null ? transform.parent.gameObject : gameObject;
        }
    }

    // 버튼 클릭 시 호출될 함수
    public void CloseWindow()
    {
        if (targetWindow != null)
        {
            targetWindow.SetActive(false);
        }
    }
}
