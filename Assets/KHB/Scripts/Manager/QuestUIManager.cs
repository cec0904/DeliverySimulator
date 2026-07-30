using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUIManager : MonoBehaviour
{
    [Header("[ 패널 참조 ]")]
    [SerializeField] private GameObject questListPanel;   // 1. quest title 목록 패널
    [SerializeField] private GameObject questDetailPanel; // 2. quest contents 상세 패널

    [Header("[ 상세 화면 Component ]")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text deliveryInfoText;
    [SerializeField] private Button backButton;

    private void Start()
    {
        // 뒤로가기 버튼 이벤트 연결
        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowList);
        }

        // 시작 시에는 목록 화면만 켜둔다.
        ShowList();
    }

    // 1번(Title) 항목을 클릭했을 때 호출되는 함수
    public void ShowDetail(QuestDataSO questData)
    {
        // 1. 선택한 퀘스트 데이터로 UI 텍스트 채우기
        titleText.text = questData.questTitle;
        descriptionText.text = questData.questDescription;
        deliveryInfoText.text = $"전달 대상: {questData.targetNpcID} / 위치: {questData.deliveryLocation}";

        // 2. 패널 전환
        questListPanel.SetActive(false);  // 1번 숨기기
        questDetailPanel.SetActive(true); // 2번 띄우기
    }

    // 뒤로가기 누르거나 목록으로 복귀 시 호출
    public void ShowList()
    {
        questDetailPanel.SetActive(false); // 2번 숨기기
        questListPanel.SetActive(true);   // 1번 띄우기
    }
}