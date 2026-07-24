using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MovingQuestListUI : MonoBehaviour
{
    [Header("UI Rect Transform & Pointers")]
    [SerializeField] private RectTransform questWindowRect; // P_QuestWindow RectTransform
    [SerializeField] private CanvasGroup detailCanvasGroup; // P_DetailContent의 CanvasGroup

    [Header("Transform Configs")]
    [SerializeField] private Vector2 smallPosition = new Vector2(-200f, 0f); // 오른쪽 작을 때 Pos X
    [SerializeField] private Vector3 smallScale = new Vector3(0.8f, 0.8f, 1f); // 오른쪽 작을 때 크기

    [Header("Center Expanded Configs")]
    [SerializeField] private Vector2 centerPosition = Vector2.zero; // 중앙 Pos (0,0)
    [SerializeField] private Vector3 centerScale = Vector3.one; // 중앙 크기 (1,1,1)

    [SerializeField] private float animDuration = 0.3f; // 애니메이션 속도(초)

    [Header("List & Text Settings")]
    [SerializeField] private Transform listContentParent;
    [SerializeField] private GameObject questTitleItemPrefab;
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;

    private List<GameObject> spawnedTitleButtons = new List<GameObject>();
    private Coroutine activeAnimCoroutine;
    private bool isExpanded = false;

    private void Awake()
    {
        if (questWindowRect == null) questWindowRect = GetComponent<RectTransform>();
        ResetToSmallState();
    }

    private void OnEnable()
    {
        RefreshQuestList();
        ResetToSmallState();
    }

    /// <summary>
    /// 초기 오른쪽 작은 상태로 리셋
    /// </summary>
    public void ResetToSmallState()
    {
        isExpanded = false;
        questWindowRect.anchoredPosition = smallPosition;
        questWindowRect.localScale = smallScale;

        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.alpha = 0f;
            detailCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 퀘스트 목록 갱신
    /// </summary>
    public void RefreshQuestList()
    {
        foreach (var btn in spawnedTitleButtons) Destroy(btn);
        spawnedTitleButtons.Clear();

        if (QuestManager.Instance == null) return;

        foreach (var quest in QuestManager.Instance.GetActiveQuests())
        {
            if (quest == null) continue;

            GameObject itemObj = Instantiate(questTitleItemPrefab, listContentParent);
            QuestTitleItem titleItem = itemObj.GetComponent<QuestTitleItem>();

            if (titleItem != null)
            {
                // 제목 클릭 시 확대 연출 함수 호출!
                titleItem.Setup(quest, OnSelectQuestTitle);
            }
            spawnedTitleButtons.Add(itemObj);
        }
    }

    /// <summary>
    /// 특정 퀘스트 제목 클릭 시 호출
    /// </summary>
    private void OnSelectQuestTitle(QuestDataSO questData)
    {
        // 1. 텍스트 정보 업데이트
        if (detailTitleText != null) detailTitleText.text = questData.questTitle;
        if (detailDescriptionText != null) detailDescriptionText.text = questData.questDescription;

        // 2. 이미 확대 상태가 아니라면 중앙으로 확대 연출 실행
        if (!isExpanded)
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(Co_AnimateWindow(true));
        }
    }

    /// <summary>
    /// 축소 버튼(X)을 누르거나 다시 작은 모드로 돌릴 때
    /// </summary>
    public void OnClickShrinkButton()
    {
        if (isExpanded)
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(Co_AnimateWindow(false));
        }
    }

    /// <summary>
    /// 중앙 확대 / 오른쪽 축소 코루틴 애니메이션
    /// </summary>
    private System.Collections.IEnumerator Co_AnimateWindow(bool expand)
    {
        isExpanded = expand;

        Vector2 startPos = questWindowRect.anchoredPosition;
        Vector2 targetPos = expand ? centerPosition : smallPosition;

        Vector3 startScale = questWindowRect.localScale;
        Vector3 targetScale = expand ? centerScale : smallScale;

        float startAlpha = detailCanvasGroup != null ? detailCanvasGroup.alpha : 0f;
        float targetAlpha = expand ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale이 0이어도 동작하도록 unscaledDeltaTime 사용
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration); // 부드러운 가속/감속(SmoothStep)

            // Pos & Scale 보간
            questWindowRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            questWindowRect.localScale = Vector3.Lerp(startScale, targetScale, t);

            // 상세 내용 Alpha 보간
            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            yield return null;
        }

        // 최종값 고정
        questWindowRect.anchoredPosition = targetPos;
        questWindowRect.localScale = targetScale;
        if (detailCanvasGroup != null)
        {
            detailCanvasGroup.alpha = targetAlpha;
            detailCanvasGroup.blocksRaycasts = expand; // 확대되었을 때만 클릭 가능
        }
    }

    /// <summary>
    /// 전체 UI 닫기 (UIManager 연동)
    /// </summary>
    public void CloseQuestUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
