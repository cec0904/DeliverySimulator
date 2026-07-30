using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "QuestSystem/Quest Database")]
public class QuestDatabaseSO : ScriptableObject
{
    [Header("[ 등록된 전체 퀘스트 목록 ]")]
    public List<QuestDataSO> questList = new List<QuestDataSO>();

    public QuestDataSO GetRandomQuest()
    {
        if (questList == null || questList.Count == 0)
        {
            Debug.LogWarning("등록된 퀘스트가 없습니다!");
            return null;
        }

        int randomIndex = Random.Range(0, questList.Count);
        return questList[randomIndex];
    }
}
