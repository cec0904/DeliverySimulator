using UnityEngine;

public class MenuController : MonoBehaviour
{
    // 연결할 UI Image 오브젝트
    [SerializeField] private GameObject menuGroup;

    // 현재 메뉴가 켜져 있는지 확인하는 변수
    private bool isMenuOpen = false;

    void Update()
    {
        //if (Application.isEditor)
        //{
        //    if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        //    {
        //        ToggleMenu();
        //    }
        //}
        //else
        //{
        //    if (Input.GetKeyDown(KeyCode.Escape))
        //    {
        //        ToggleMenu();
        //    }
        //}
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        // 상태 반전 (true -> false, false -> true)
        isMenuOpen = !isMenuOpen;

        // 1. UI 활성화/비활성화 제어
        if (menuGroup != null)
        {
            menuGroup.SetActive(isMenuOpen);
        }

        // 2. 게임 일시정지 및 재개 제어 (시간 배율 조절)
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // 게임 일시정지 (물리, 애니메이션, 시간 흐름 멈춤)
            Debug.Log("게임 일시정지");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f; // 원래 속도로 재개
            Debug.Log("게임 재개");
        }
    }

    // 씬이 시작될 때 메뉴가 꺼져 있도록 보장
    void Start()
    {
        if (menuGroup != null)
        {
            menuGroup.SetActive(false);
            isMenuOpen = false;
            Time.timeScale = 1f;
        }
    }

    // 씬이 전환되거나 스크립트가 꺼질 때 시간 배율을 초기화 (버그 방지)
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
