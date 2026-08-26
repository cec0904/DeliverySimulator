using System.Collections;
using TMPro;
using UnityEngine;

public class NpcQuestUIController : MonoBehaviour
{
    private const string ResourcePath = "NpcQuestUI";

    [Header("상호작용 안내")]
    [SerializeField] private CanvasGroup interactionGroup;
    [SerializeField] private TMP_Text interactionText;

    [Header("전달 완료")]
    [SerializeField] private CanvasGroup completionGroup;
    [SerializeField] private TMP_Text completionTitleText;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text completionBodyText;
    [SerializeField] private float completionVisibleDuration = 2f;

    private static NpcQuestUIController instance;
    private PlayerQuestList playerQuestList;
    private Coroutine completionRoutine;
    private Coroutine timedPromptRoutine;
    private bool timedPromptActive;

    public static NpcQuestUIController CreateIfMissing()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindAnyObjectByType<NpcQuestUIController>();

        if (instance != null)
        {
            return instance;
        }

        NpcQuestUIController prefab = Resources.Load<NpcQuestUIController>(ResourcePath);

        if (prefab == null)
        {
            return null;
        }

        instance = Instantiate(prefab);
        instance.name = prefab.name;

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SetCanvasGroupVisible(interactionGroup, false);
        SetCanvasGroupVisible(completionGroup, false);
        BindPlayerQuestList();
    }

    private void Start()
    {
        BindPlayerQuestList();
    }

    private void OnDestroy()
    {
        UnbindPlayerQuestList();

        if (instance == this)
        {
            instance = null;
        }
    }

    public void ShowInteractionPrompt(string message)
    {
        if (timedPromptActive)
        {
            return;
        }

        if (interactionText == null || string.IsNullOrWhiteSpace(message))
        {
            HideInteractionPrompt();
            return;
        }

        interactionText.text = message;
        SetCanvasGroupVisible(interactionGroup, true);
    }

    public void HideInteractionPrompt()
    {
        if (timedPromptActive)
        {
            return;
        }

        SetCanvasGroupVisible(interactionGroup, false);
    }

    public void ShowTimedInteractionPrompt(string message, float duration)
    {
        if (interactionText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (timedPromptRoutine != null)
        {
            StopCoroutine(timedPromptRoutine);
        }

        timedPromptActive = true;
        interactionText.text = message;
        SetCanvasGroupVisible(interactionGroup, true);
        timedPromptRoutine = StartCoroutine(HideTimedPromptRoutine(duration));
    }

    public void CancelTimedInteractionPrompt()
    {
        if (timedPromptRoutine != null)
        {
            StopCoroutine(timedPromptRoutine);
            timedPromptRoutine = null;
        }

        timedPromptActive = false;
        SetCanvasGroupVisible(interactionGroup, false);
    }

    private void BindPlayerQuestList()
    {
        PlayerQuestList foundQuestList = FindAnyObjectByType<PlayerQuestList>();

        if (playerQuestList == foundQuestList)
        {
            return;
        }

        UnbindPlayerQuestList();
        playerQuestList = foundQuestList;

        if (playerQuestList != null)
        {
            playerQuestList.QuestCompleted += ShowQuestCompleted;
        }
    }

    private void UnbindPlayerQuestList()
    {
        if (playerQuestList != null)
        {
            playerQuestList.QuestCompleted -= ShowQuestCompleted;
            playerQuestList = null;
        }
    }

    private void ShowQuestCompleted(QuestRuntimeData quest)
    {
        if (quest == null)
        {
            return;
        }

        CancelTimedInteractionPrompt();

        if (completionTitleText != null)
        {
            completionTitleText.text = "전달 완료!";
        }

        if (npcNameText != null)
        {
            npcNameText.text = quest.destination != null
                ? quest.destination.DisplayName
                : "배달 NPC";
        }

        if (completionBodyText != null)
        {
            string itemName = quest.questData != null &&
                              !string.IsNullOrWhiteSpace(quest.questData.displayName)
                ? quest.questData.displayName
                : "물건";

            completionBodyText.text = $"{itemName} 전달을 완료했습니다";
        }

        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
        }

        completionRoutine = StartCoroutine(ShowCompletionRoutine());
    }

    private IEnumerator HideTimedPromptRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));

        timedPromptActive = false;
        timedPromptRoutine = null;
        SetCanvasGroupVisible(interactionGroup, false);
    }

    private IEnumerator ShowCompletionRoutine()
    {
        yield return FadeCanvasGroup(completionGroup, 0f, 1f, 0.15f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, completionVisibleDuration));
        yield return FadeCanvasGroup(completionGroup, 1f, 0f, 0.2f);

        completionRoutine = null;
    }

    private static IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float from,
        float to,
        float duration
    )
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
        bool visible = to > 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(visible);
    }

    private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(visible);
    }
}
