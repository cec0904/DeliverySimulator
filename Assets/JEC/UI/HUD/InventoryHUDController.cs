using UnityEngine;
using UnityEngine.UI;

public class InventoryHUDController : MonoBehaviour
{
    [Header("퀘스트 데이터")]
    [SerializeField] private PlayerQuestList playerQuestList;

    [Header("물건 아이콘")]
    [SerializeField] private RawImage[] itemIcons;

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

        int count = Mathf.Min(playerQuestList.SelectedQuests.Count, itemIcons.Length);

        for (int i = 0; i < count; i++)
        {
            QuestRuntimeData quest = playerQuestList.SelectedQuests[i];

            // 아직 픽업하지 않은 퀘스트는 빈 슬롯으로 유지
            if (quest == null || quest.state != QuestState.PickedUp || quest.questData == null || quest.questData.icon == null)
            {
                continue;
            }

            RawImage itemIcon = itemIcons[i];

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