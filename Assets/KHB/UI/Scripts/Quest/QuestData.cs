using UnityEngine;

public class QuestData : MonoBehaviour
{
    public string questTitle;
    [TextArea(5, 10)]
    public string questDescription; // TextArea 창 넓어짐

    public int goldReward;
    public bool isCompleted;
}
