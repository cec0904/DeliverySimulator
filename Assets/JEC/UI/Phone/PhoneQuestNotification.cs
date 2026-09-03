using TMPro;
using UnityEngine;

/// <summary>Shows the current offer count only while the phone is fully tucked away.</summary>
[DisallowMultipleComponent]
public class PhoneQuestNotification : MonoBehaviour
{
    [SerializeField] private PhoneUIController phoneUIController;
    [Tooltip("Leave empty on the prefab to use the scene's quest manager.")]
    [SerializeField] private questManager questManager;
    [Tooltip("Keep this separate from the GameObject hosting this component.")]
    [SerializeField] private GameObject badgeRoot;
    [SerializeField] private TMP_Text countText;

    private void OnEnable()
    {
        if (questManager == null)
            questManager = FindAnyObjectByType<questManager>();

        if (questManager != null)
            questManager.OffersChanged += RefreshBadge;

        if (phoneUIController != null)
        {
            phoneUIController.TransitionStarted += RefreshBadge;
            phoneUIController.StateChanged += RefreshBadge;
        }

        RefreshBadge();
    }

    private void Start()
    {
        // All scene OnEnable callbacks (including initial offer generation) have run now.
        RefreshBadge();
    }

    private void OnDisable()
    {
        if (questManager != null)
            questManager.OffersChanged -= RefreshBadge;

        if (phoneUIController != null)
        {
            phoneUIController.TransitionStarted -= RefreshBadge;
            phoneUIController.StateChanged -= RefreshBadge;
        }

        if (badgeRoot != null)
            badgeRoot.SetActive(false);
    }

    private void RefreshBadge()
    {
        int count = questManager != null ? questManager.QuestOffers.Count : 0;
        if (countText != null)
            countText.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);

        bool visible = count > 0 && phoneUIController != null &&
                       !phoneUIController.IsOpen && !phoneUIController.IsAnimating;
        if (badgeRoot != null && badgeRoot.activeSelf != visible)
            badgeRoot.SetActive(visible);
    }
}
