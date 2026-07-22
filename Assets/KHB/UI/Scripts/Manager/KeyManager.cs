using TMPro;
using UnityEngine;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance;

    [Header("[ 현재 설정된 키 ]")]
    public KeyCode mapKey = KeyCode.M;
    public KeyCode questKey = KeyCode.Q;
    public KeyCode menuKey = KeyCode.Escape;

    [Header("[ UI 텍스트 연결 (버튼 안에 있는 글자) ]")]
    public TextMeshProUGUI mapKeyText;
    public TextMeshProUGUI questKeyText;
    public TextMeshProUGUI menuKeyText;

    // 키 변경 대기 상태를 위한 변수들
    private bool isWaitingForKey = false;
    private string actionToRebind = "";

    public bool IsWaitingForKey // 프로퍼티 활용
    {
        get { return isWaitingForKey; }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadKeys(); // 게임 시작 시 저장된 키 불러오기
    }

    private void Start()
    {
        UpdateUI(); // UI에 현재 키 텍스트 표시
    }

    // 1. 저장된 키를 불러오는 함수 (저장된 게 없으면 기본값 세팅)
    private void LoadKeys()
    {
        mapKey = (KeyCode)PlayerPrefs.GetInt("MapKey", (int)mapKey);
        questKey = (KeyCode)PlayerPrefs.GetInt("QuestKey", (int)questKey);
        menuKey = (KeyCode)PlayerPrefs.GetInt("MenuKey", (int)menuKey);
    }

    // 2. 현재 설정된 키를 UI 텍스트에 업데이트
    private void UpdateUI()
    {
        if (mapKeyText != null) mapKeyText.text = mapKey.ToString();
        if (questKeyText != null) questKeyText.text = questKey.ToString();
        if (menuKeyText != null) menuKeyText.text = menuKey.ToString();
    }

    // 3. UI 버튼을 클릭했을 때 호출할 함수 (Inspector에서 버튼 OnClick에 연결)
    public void StartRebind(string actionName)
    {
        if (isWaitingForKey) return; // 이미 다른 키를 기다리는 중이면 무시

        actionToRebind = actionName;
        isWaitingForKey = true;

        // 유저가 알 수 있게 텍스트 변경
        switch (actionName)
        {
            case "Map": mapKeyText.text = "Press Any Key..."; break;
            case "Quest": questKeyText.text = "Press Any Key..."; break;
            case "Menu": menuKeyText.text = "Press Any Key..."; break;
        }
    }

    // 4. 유니티의 GUI 이벤트 시스템을 통해 키보드 입력 감지 (핵심 로직)
    private void OnGUI()
    {
        if (isWaitingForKey)
        {
            Event e = Event.current;

            // 키보드가 눌렸고, 그 키가 None이 아니라면
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                
                if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.A || e.keyCode == KeyCode.S || e.keyCode == KeyCode.D
                    || e.keyCode == KeyCode.Space)
                {
                    isWaitingForKey = false;
                    UpdateUI();
                    return;
                }

                AssignNewKey(e.keyCode);
            }
        }
    }

    // 5. 감지된 새 키를 저장하고 UI 갱신
    private void AssignNewKey(KeyCode newKey)
    {
        switch (actionToRebind)
        {
            case "Map":
                mapKey = newKey;
                PlayerPrefs.SetInt("MapKey", (int)mapKey);
                break;
            case "Quest":
                questKey = newKey;
                PlayerPrefs.SetInt("QuestKey", (int)questKey);
                break;
            case "Menu":
                menuKey = newKey;
                PlayerPrefs.SetInt("MenuKey", (int)menuKey);
                break;
        }

        PlayerPrefs.Save(); // 디스크에 확실히 저장
        isWaitingForKey = false;
        UpdateUI();
    }
}
