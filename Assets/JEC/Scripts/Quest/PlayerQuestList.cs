using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestState
{
    Offered,
    Accepted,
    PickedUp,
    Completed,
    Failed
}

[Serializable]
public class QuestRuntimeData
{
    public string runtimeQuestId;

    public DeliveryQuestData questData;
    public QuestPickUpPoint pickupPoint;
    public QuestDestination destination;

    public QuestState state;

    public float timeLimit;
    public float remainingTime;
    public int reward;
}

public class PlayerQuestList : MonoBehaviour
{
    // 플레이어가 선택해서 진행 중인 퀘스트
    [SerializeField] private List<QuestRuntimeData> selectedQuests = new();
    [SerializeField] private int maxSelectedQuestCount = 5;
    public IReadOnlyList<QuestRuntimeData> SelectedQuests => selectedQuests;

    public event Action QuestsChanged;
    public event Action<QuestRuntimeData> QuestCompleted;

    public bool TryAddQuest(QuestRuntimeData quest)
    {
        if (quest == null || selectedQuests.Contains(quest) || selectedQuests.Count >= maxSelectedQuestCount)
        {
            return false;
        }

        quest.state = QuestState.Accepted;
        selectedQuests.Add(quest);

        QuestsChanged?.Invoke();

        return true;
    }
    public bool TryCancelQuest(string runtimeQuestId)
    {
        QuestRuntimeData quest = FindQuest(runtimeQuestId);

        if (quest == null)
        {
            return false;
        }

        selectedQuests.Remove(quest);
        QuestsChanged?.Invoke();

        return true;
    }

    public void SetPickedUp(string runtimeQuestId)
    {
        QuestRuntimeData quest = FindQuest(runtimeQuestId);

        if (quest == null || quest.state != QuestState.Accepted)
        {
            return;
        }

        quest.state = QuestState.PickedUp;
        QuestsChanged?.Invoke();
    }

    public void CompleteQuest(string runtimeQuestId)
    {
        QuestRuntimeData quest = FindQuest(runtimeQuestId);

        if (!TryCompleteQuest(quest))
        {
            return;
        }

        QuestCompleted?.Invoke(quest);
        QuestsChanged?.Invoke();
    }

    public int TryDeliverQuestsAt(QuestDestination destination)
    {
        if (destination == null)
        {
            return 0;
        }

        List<QuestRuntimeData> completedQuests = new();

        for (int i = selectedQuests.Count - 1; i >= 0; i--)
        {
            QuestRuntimeData quest = selectedQuests[i];

            if (quest == null || quest.state != QuestState.PickedUp)
            {
                continue;
            }

            if (quest.destination != destination)
            {
                continue;
            }

            if (TryCompleteQuest(quest))
            {
                completedQuests.Add(quest);
            }
        }

        if (completedQuests.Count == 0)
        {
            return 0;
        }

        foreach (QuestRuntimeData completedQuest in completedQuests)
        {
            QuestCompleted?.Invoke(completedQuest);
        }

        QuestsChanged?.Invoke();

        return completedQuests.Count;
    }

    public void FailQuest(string runtimeQuestId)
    {
        QuestRuntimeData quest = FindQuest(runtimeQuestId);

        if (quest == null)
        {
            return;
        }

        quest.state = QuestState.Failed;
        selectedQuests.Remove(quest);
        QuestsChanged?.Invoke();
    }

    public QuestRuntimeData FindQuest(string runtimeQuestId)
    {
        return selectedQuests.Find(quest => quest.runtimeQuestId == runtimeQuestId);
    }

    public bool HasQuestReadyForPickupAt(QuestPickUpPoint pickupPoint)
    {
        if (pickupPoint == null)
        {
            return false;
        }

        return selectedQuests.Exists(quest =>
            quest != null &&
            quest.state == QuestState.Accepted &&
            quest.pickupPoint == pickupPoint
        );
    }

    public bool HasQuestReadyForDeliveryAt(QuestDestination destination)
    {
        if (destination == null)
        {
            return false;
        }

        return selectedQuests.Exists(quest =>
            quest != null &&
            quest.state == QuestState.PickedUp &&
            quest.destination == destination
        );
    }

    public int TryPickUpQuestsAt(QuestPickUpPoint pickupPoint)
    {
        if (pickupPoint == null)
        {
            return 0;
        }

        int pickedUpCount = 0;

        foreach (QuestRuntimeData quest in selectedQuests)
        {
            if (quest == null)
            {
                continue;
            }

            if (quest.state != QuestState.Accepted)
            {
                continue;
            }

            if (quest.pickupPoint != pickupPoint)
            {
                continue;
            }

            quest.state = QuestState.PickedUp;
            pickedUpCount++;
        }

        if (pickedUpCount > 0)
        {
            QuestsChanged?.Invoke();
        }

        return pickedUpCount;
    }

    private bool TryCompleteQuest(QuestRuntimeData quest)
    {
        if (quest == null || quest.state != QuestState.PickedUp)
        {
            return false;
        }

        quest.state = QuestState.Completed;
        selectedQuests.Remove(quest);

        return true;
    }
}
