using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestListUI : MonoBehaviour
{
    [Header("Title List UI")]
    [SerializeField] private Transform listContentParent; // 제목 버튼들이 생성될 Content
    [SerializeField] private GameObject questTitleItemPrefab; // 제목 버튼 프리팹

    [Header("Detail Popup UI")]
    [SerializeField] private GameObject detailPopupPanel; // 상세 내용 팝업 패널
    [SerializeField] private TextMeshProUGUI detailTitleText; // 상세 팝업 - 제목
    [SerializeField] private TextMeshProUGUI detailDescriptionText; // 상세 팝업 - 내용
    [SerializeField] private Button closeDetailButton; // 팝업 닫기 버튼

    private List<GameObject> spawnedTitleButtons = new List<GameObject>();
    public static QuestListUI Instance { get; private set; }
    private void Awake()
    {
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.AddListener(CloseDetailPopup);
        }

        // 상세 팝업은 처음에 켜지지 않도록 설정
        if (detailPopupPanel != null)
        {
            detailPopupPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // UI 창이 활성화될 때마다 퀘스트 목록 갱신
        RefreshQuestList();
    }

    /// <summary>
    /// QuestManager의 ActiveQuestList를 가져와 제목 버튼 목록 생성
    /// </summary>
    public void RefreshQuestList()
    {
        // 1. 기존 버튼 청소
        foreach (var btn in spawnedTitleButtons)
        {
            Destroy(btn);
        }
        spawnedTitleButtons.Clear();

        if (QuestManager.Instance == null) return;

        // 2. 진행 중인 퀘스트 목록 가져오기
        List<QuestDataSO> activeQuests = QuestManager.Instance.GetActiveQuests();

        // 3. 퀘스트별 제목 버튼 생성
        foreach (var quest in activeQuests)
        {
            if (quest == null) continue;

            GameObject itemObj = Instantiate(questTitleItemPrefab, listContentParent);
            QuestTitleItem titleItem = itemObj.GetComponent<QuestTitleItem>();

            if (titleItem != null)
            {
                // 버튼 생성 및 클릭 시 ShowDetailPopup 호출 연결
                titleItem.Setup(quest, ShowDetailPopup);
            }

            spawnedTitleButtons.Add(itemObj);
        }
    }

    /// <summary>
    /// 제목 버튼 클릭 시 팝업을 열고 제목/내용 출력
    /// </summary>
    private void ShowDetailPopup(QuestDataSO questData)
    {
        if (questData == null) return;

        if (detailTitleText != null)
            detailTitleText.text = questData.questTitle;

        if (detailDescriptionText != null)
            detailDescriptionText.text = questData.questDescription; // SO의 내용 변수명에 맞게 수정

        if (detailPopupPanel != null)
            detailPopupPanel.SetActive(true);
    }

    public void CloseQuestWindow()
    {
        if (UIManager.Instance != null)
        {
            // 💡 UIManager를 통해 창을 끄고 시간/마우스 상태를 자동으로 원복
            UIManager.Instance.ClosePanel(gameObject);
        }
        else
        {
            // 안전장치
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 상세 팝업 닫기
    /// </summary>
    public void CloseDetailPopup()
    {
        if (detailPopupPanel != null)
            detailPopupPanel.SetActive(false);
    }
}