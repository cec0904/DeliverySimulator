using UnityEngine;
using UnityEngine.UI;

public class InventoryHUDController : MonoBehaviour
{
    [Header("퀘스트 데이터")]
    [SerializeField] private PlayerQuestList playerQuestList;

    [Header("물건 아이콘")]
    [SerializeField] private RawImage[] itemIcons;

    [Header("표시 조건")]
    [SerializeField] private bool showAcceptedQuestItems;

    private void Awake()
    {
        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }

        ClearHUD();
    }

    private void OnEnable()
    {
        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged += RefreshHUD;
        }

        RefreshHUD();
    }

    private void OnDisable()
    {
        if (playerQuestList != null)
        {
            playerQuestList.QuestsChanged -= RefreshHUD;
        }
    }

    private void RefreshHUD()
    {
        ClearHUD();

        if (playerQuestList == null || itemIcons == null)
        {
            return;
        }

        int iconIndex = 0;

        for (int i = 0; i < playerQuestList.SelectedQuests.Count && iconIndex < itemIcons.Length; i++)
        {
            QuestRuntimeData quest = playerQuestList.SelectedQuests[i];

            if (quest == null || quest.questData == null || quest.questData.icon == null)
            {
                continue;
            }

            bool shouldShow = quest.state == QuestState.PickedUp ||
                              (showAcceptedQuestItems && quest.state == QuestState.Accepted);

            if (!shouldShow)
            {
                continue;
            }

            RawImage itemIcon = itemIcons[iconIndex];
            iconIndex++;

            if (itemIcon == null)
            {
                continue;
            }

            itemIcon.texture = quest.questData.icon;
            itemIcon.enabled = true;
        }
    }

    private void ClearHUD()
    {
        if (itemIcons == null)
        {
            return;
        }

        foreach (RawImage itemIcon in itemIcons)
        {
            if (itemIcon == null)
            {
                continue;
            }

            itemIcon.texture = null;
            itemIcon.enabled = false;
        }
    }
}
