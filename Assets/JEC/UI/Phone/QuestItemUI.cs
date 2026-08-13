using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [Header("표시 UI")]
    [SerializeField] private RawImage itemIcon;
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text routeText;
    [SerializeField] private TMP_Text rewardText;

    [Header("버튼")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;

    public void Bind(
    QuestRuntimeData quest,
    Action onAccept,
    Action onCancel)
    {
        if (quest == null)
        {
            return;
        }

        if (questNameText != null)
        {
            if (quest.questData == null)
            {
                questNameText.text = "이름 없는 퀘스트";
            }
            else if (!string.IsNullOrWhiteSpace(quest.questData.displayName))
            {
                questNameText.text = quest.questData.displayName;
            }
            else
            {
                questNameText.text = quest.questData.name;
            }
        }

        if (routeText != null)
        {
            string pickupName = quest.pickupPoint != null
                ? quest.pickupPoint.name
                : "출발지 없음";

            string destinationName = quest.destination != null
                ? quest.destination.name
                : "목적지 없음";

            routeText.text = $"{pickupName} → {destinationName}";
        }

        if (rewardText != null)
        {
            rewardText.text = $"{quest.reward:N0}원";
        }

        if (itemIcon != null)
        {
            itemIcon.texture = quest.questData != null
                ? quest.questData.icon
                : null;
        }

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.gameObject.SetActive(onAccept != null);

            if (onAccept != null)
            {
                acceptButton.onClick.AddListener(() => onAccept.Invoke());
            }
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.gameObject.SetActive(onCancel != null);

            if (onCancel != null)
            {
                cancelButton.onClick.AddListener(() => onCancel.Invoke());
            }
        }
    }
}