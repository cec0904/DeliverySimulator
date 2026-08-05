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

    public IReadOnlyList<QuestRuntimeData> SelectedQuests => selectedQuests;

    public bool TryAddQuest(QuestRuntimeData quest)
    {
        if (quest == null || selectedQuests.Contains(quest))
        {
            return false;
        }

        quest.state = QuestState.Accepted;
        selectedQuests.Add(quest);

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
    }

    public void CompleteQuest(string runtimeQuestId)
    {
        QuestRuntimeData quest = FindQuest(runtimeQuestId);

        if (quest == null || quest.state != QuestState.PickedUp)
        {
            return;
        }

        quest.state = QuestState.Completed;
        selectedQuests.Remove(quest);
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
    }

    public QuestRuntimeData FindQuest(string runtimeQuestId)
    {
        return selectedQuests.Find(quest => quest.runtimeQuestId == runtimeQuestId);
    }
}