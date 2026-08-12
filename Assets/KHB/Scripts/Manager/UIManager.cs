using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("[ UI 패널들 ]")]
    //[SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject inventoryPanel;

    [SerializeField] private MainMenuManager mainMenuManager;

    [Header("[ 미니맵 ]")]
    [SerializeField] private GameObject minimapPanel;
    [SerializeField] private Camera minimapCamera;

    // 폰 애니메이션 연출 전용 컨트롤러
    [SerializeField] private PhoneUIController phoneUIController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (phoneUIController != null)
        {
            phoneUIController.StateChanged += UpdateGameState;
        }
    }

    private void Start()
    {
        // 게임 시작 시 UI 초기 상태 세팅
        InitUI();
    }

    private void InitUI()
    {
        //if (inGameHUD != null) inGameHUD.SetActive(true);

        if (mainMenuManager != null) mainMenuManager.SetMenuVisible(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        //if (questPanel != null) questPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);


        UpdateGameState();
    }

    private void Update()
    {
        if (KeyManager.Instance == null || KeyManager.Instance.IsWaitingForKey) return;
        if (Input.GetKeyDown(KeyManager.Instance.menuKey))
        {
            HandleEscapeKey();
            return;
        }
        if (mainMenuManager != null && mainMenuManager.IsOpen)
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyManager.Instance.mapKey))
        {
            TogglePanel(mapPanel);
        }

        else if (Input.GetKeyDown(KeyManager.Instance.questKey))
        {
            if (phoneUIController != null)
            {
                phoneUIController.TogglePhone();
                UpdateGameState();
            }
        }
        //else if (Input.GetKeyDown(KeyManager.Instance.menuKey))
        //{
        //    TogglePanel(inventoryPanel);
        //}

    }

    public void TogglePanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isActive = targetPanel.activeSelf;

        // 다른 팝업 UI들을 한 번에 다 닫아줌
        CloseAllPanels();

        // 내가 누른 창만 상태 반전
        targetPanel.SetActive(!isActive);

        // 플레이어 조작/커서 상태 업데이트
        UpdateGameState();
    }

    private void HandleEscapeKey()
    {
        // 휴대폰 애니메이션 중에는 ESC 입력 무시
        // 메인 메뉴가 열리는 것도 방지
        if (phoneUIController != null &&
            phoneUIController.IsAnimating)
        {
            return;
        }

        // 휴대폰이 열려 있으면 휴대폰만 닫고 종료
        if (phoneUIController != null &&
            phoneUIController.IsOpen)
        {
            phoneUIController.ClosePhone();

            // 현재는 Closing 상태이므로 일시정지를 유지
            // 닫기 완료 이벤트에서 다시 복구
            UpdateGameState();

            return;
        }

        // 열려있는 패널이 있다면 그것부터 닫기
        if (IsAnyPopupOpen())
        {
            CloseAllPanels();
        }
        else if (mainMenuManager != null)
        {
            if (mainMenuManager.IsSubPanelOpen)
            {
                mainMenuManager.ResetToMainButtons();
            }
            else
            {
                // 서브 패널이 열린 게 아니라면 메인 메뉴 전체를 Toggle
                bool nextState = !mainMenuManager.IsOpen;
                mainMenuManager.SetMenuVisible(nextState);
            }
        }

        UpdateGameState();
    }

    private bool IsAnyPopupOpen()
    {
        bool mapOpen = mapPanel != null && mapPanel.activeSelf;
        //bool questOpen = questPanel != null && questPanel.activeSelf;
        bool questOpen = phoneUIController != null && (phoneUIController.IsOpen || phoneUIController.IsAnimating);
        bool invOpen = inventoryPanel != null && inventoryPanel.activeSelf;

        return mapOpen || questOpen || invOpen;
    }

    private void CloseAllPanels()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        //if (questPanel != null) questPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (mainMenuManager != null) mainMenuManager.SetMenuVisible(false);
    }

    public void ClosePanel(GameObject targetPanel)
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }

        // 핵심: 창이 닫혔으니 시간, 커서, 미니맵 상태를 다시 계산해라!
        UpdateGameState();
    }

    public void CloseMainMenu()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.SetMenuVisible(false); // 내부에서 ResetToMainButtons()도 함께 수행됨
        }

        // 시간, 마우스 커서, 미니맵 등 재계산
        UpdateGameState();
    }

    private void UpdateGameState()
    {
        bool isMainMenuOpen = mainMenuManager != null && mainMenuManager.IsOpen;
        bool isAnyUIOpen = IsAnyPopupOpen() || isMainMenuOpen;
        //Debug.Log($"[UpdateGameState] isMainMenuOpen: {isMainMenuOpen} | IsAnyPopupOpen(): {IsAnyPopupOpen()} => isAnyUIOpen: {isAnyUIOpen}");
        // UI 열려 있으면 마우스 가능, 시간 제어
        Cursor.visible = isAnyUIOpen;
        Cursor.lockState = isAnyUIOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = isAnyUIOpen ? 0f : 1f;

        if (minimapPanel != null) minimapPanel.SetActive(!isAnyUIOpen);
        if (minimapCamera != null) minimapCamera.enabled = !isAnyUIOpen;
    }

    private void OnDisable()
    {
        // 스크립트 비활성화 시 timeScale 원복 안전장치
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (phoneUIController != null)
        {
            phoneUIController.StateChanged -= UpdateGameState;
        }

        Time.timeScale = 1f;
    }

    public void OpenQuestPanel()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (mainMenuManager != null)
            mainMenuManager.SetMenuVisible(false);

        if (phoneUIController != null)
            phoneUIController.OpenPhone();

        UpdateGameState();
    }
}
