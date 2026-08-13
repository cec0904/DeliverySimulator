using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneQuestUIController : MonoBehaviour
{
    private enum QuestTab
    {
        NewQuests,
        AcceptedQuests
    }

    [Header("핸드폰 상태")]
    [SerializeField] private PhoneUIController phoneUIController;

    [Header("핸드폰 화면")]
    [SerializeField] private GameObject clockScreen;
    [SerializeField] private GameObject newQuestScreen;
    [SerializeField] private GameObject acceptedQuestScreen;

    [Header("렌더링 순서")]
    [SerializeField] private RectTransform phoneDisplayRoot;
    [SerializeField] private RectTransform phoneFrame;

    [Header("탭 버튼")]
    [SerializeField] private Button newQuestButton;
    [SerializeField] private Button acceptedQuestButton;

    [Header("퀘스트 목록")]
    [SerializeField] private Transform newQuestContent;
    [SerializeField] private Transform acceptedQuestContent;
    [SerializeField] private QuestItemUI questItemPrefab;

    [Header("퀘스트 데이터")]
    [SerializeField] private questManager questManager;
    [SerializeField] private PlayerQuestList playerQuestList;

    private QuestTab currentTab = QuestTab.NewQuests;

    private void Awake()
    {
        if (questManager == null)
        {
            questManager = FindAnyObjectByType<questManager>();
        }

        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }

        if (newQuestButton != null)
        {
            newQuestButton.onClick.AddListener(ShowNewQuests);
        }

        if (acceptedQuestButton != null)
        {
            acceptedQuestButton.onClick.AddListener(ShowAcceptedQuests);
        }
    }

    private void OnEnable()
    {
        if (phoneUIController != null)
        {
            phoneUIController.StateChanged += HandlePhoneStateChanged;
        }

        if (questManager != null)
        {
            questManager.OffersChanged += HandleQuestDataChanged;
        }

        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged += HandleQuestDataChanged;
        }
    }

    private void Start()
    {
        // 게임 시작 시 닫힌 핸드폰 화면 상태
        CloseQuestScreen();
    }

    private void OnDisable()
    {
        if (phoneUIController != null)
        {
            phoneUIController.StateChanged -= HandlePhoneStateChanged;
        }

        if (questManager != null)
        {
            questManager.OffersChanged -= HandleQuestDataChanged;
        }

        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged -= HandleQuestDataChanged;
        }
    }

    private void OnDestroy()
    {
        if (newQuestButton != null)
        {
            newQuestButton.onClick.RemoveListener(ShowNewQuests);
        }

        if (acceptedQuestButton != null)
        {
            acceptedQuestButton.onClick.RemoveListener(ShowAcceptedQuests);
        }
    }

    private void HandlePhoneStateChanged()
    {
        if (phoneUIController.IsOpen)
        {
            OpenQuestScreen();
        }
        else
        {
            CloseQuestScreen();
        }
    }

    private void OpenQuestScreen()
    {
        if (clockScreen != null)
        {
            clockScreen.SetActive(false);
        }

        // 핸드폰이 완전히 열렸을 때만 DisplayRoot를 앞으로
        if (phoneDisplayRoot != null)
        {
            phoneDisplayRoot.SetAsLastSibling();
        }

        // 열 때는 항상 새로운 퀘스트부터
        ShowNewQuests();
    }

    private void CloseQuestScreen()
    {
        if (newQuestScreen != null)
        {
            newQuestScreen.SetActive(false);
        }

        if (acceptedQuestScreen != null)
        {
            acceptedQuestScreen.SetActive(false);
        }

        if (clockScreen != null)
        {
            clockScreen.SetActive(true);
        }

        if (phoneFrame != null)
        {
            phoneFrame.SetAsLastSibling();
        }
    }

    public void ShowNewQuests()
    {
        currentTab = QuestTab.NewQuests;

        if (newQuestScreen != null)
        {
            newQuestScreen.SetActive(true);
        }

        if (acceptedQuestScreen != null)
        {
            acceptedQuestScreen.SetActive(false);
        }

        RefreshNewQuestList();
    }

    public void ShowAcceptedQuests()
    {
        currentTab = QuestTab.AcceptedQuests;

        if (newQuestScreen != null)
        {
            newQuestScreen.SetActive(false);
        }

        if (acceptedQuestScreen != null)
        {
            acceptedQuestScreen.SetActive(true);
        }

        RefreshAcceptedQuestList();
    }

    private void HandleQuestDataChanged()
    {
        if (currentTab == QuestTab.NewQuests)
        {
            RefreshNewQuestList();
        }
        else
        {
            RefreshAcceptedQuestList();
        }
    }

    private void RefreshNewQuestList()
    {
        if (newQuestContent == null || questItemPrefab == null || questManager == null)
        {
            return;
        }

        ClearContent(newQuestContent);

        IReadOnlyList<QuestRuntimeData> offers =
            questManager.QuestOffers;

        foreach (QuestRuntimeData quest in offers)
        {
            QuestItemUI item =
                Instantiate(questItemPrefab, newQuestContent);

            string questId = quest.runtimeQuestId;

            item.Bind(quest, () => AcceptQuest(questId), () => CancelNewQuest(questId));
        }
    }

    private void RefreshAcceptedQuestList()
    {
        if (acceptedQuestContent == null || questItemPrefab == null || playerQuestList == null)
        {
            return;
        }

        ClearContent(acceptedQuestContent);

        IReadOnlyList<QuestRuntimeData> acceptedQuests =
            playerQuestList.SelectedQuests;

        foreach (QuestRuntimeData quest in acceptedQuests)
        {
            QuestItemUI item =
                Instantiate(questItemPrefab, acceptedQuestContent);

            string questId = quest.runtimeQuestId;

            item.Bind(quest, null, () => CancelAcceptedQuest(questId));
        }
    }

    private void AcceptQuest(string runtimeQuestId)
    {
        if (questManager == null)
        {
            return;
        }

        questManager.TryAcceptQuest(runtimeQuestId);
    }
    private void CancelQuest(string runtimeQuestId)
    {
        if (questManager == null)
        {
            return;
        }

        questManager.TryAcceptQuest(runtimeQuestId);
    }

    private void ClearContent(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    private void CancelNewQuest(string runtimeQuestId)
    {
        if (questManager == null)
        {
            return;
        }

        questManager.TryCancelQuestOffer(runtimeQuestId);
    }

    private void CancelAcceptedQuest(string runtimeQuestId)
    {
        if (playerQuestList == null)
        {
            return;
        }

        playerQuestList.TryCancelQuest(runtimeQuestId);
    }
}