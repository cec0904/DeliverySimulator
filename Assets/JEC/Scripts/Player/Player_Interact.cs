using System.Collections.Generic;
using UnityEngine;

public class Player_Interact : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private float castRadius = 0.5f;

    private readonly List<Interactable> triggerInteractables = new();

    private NpcQuestUIController npcQuestUI;
    private Interactable focusedInteractable;

    private void Awake()
    {
        npcQuestUI = NpcQuestUIController.CreateIfMissing();
    }

    private void Update()
    {
        focusedInteractable = FindFocusedInteractable();
        UpdateInteractionPrompt();

        if (!Input.GetKeyDown(KeyCode.F) || focusedInteractable == null)
        {
            return;
        }

        focusedInteractable.Interact(gameObject);
    }

    public void RegisterTriggerInteractable(Interactable interactable)
    {
        if (interactable == null || triggerInteractables.Contains(interactable))
        {
            return;
        }

        triggerInteractables.Add(interactable);
    }

    public void UnregisterTriggerInteractable(Interactable interactable)
    {
        if (interactable == null)
        {
            return;
        }

        triggerInteractables.Remove(interactable);
    }

    private Interactable FindFocusedInteractable()
    {
        for (int i = triggerInteractables.Count - 1; i >= 0; i--)
        {
            Interactable triggerInteractable = triggerInteractables[i];

            if (triggerInteractable == null)
            {
                triggerInteractables.RemoveAt(i);
                continue;
            }

            return triggerInteractable;
        }

        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(
                origin,
                castRadius,
                direction,
                out RaycastHit hitInfo,
                interactionDistance,
                interactableLayer,
                QueryTriggerInteraction.Collide))
        {
            Interactable interactable =
                hitInfo.collider.GetComponentInParent<Interactable>();

            if (interactable != null && !interactable.RequiresTrigger)
            {
                return interactable;
            }
        }

        return null;
    }

    private void UpdateInteractionPrompt()
    {
        if (npcQuestUI == null)
        {
            npcQuestUI = NpcQuestUIController.CreateIfMissing();
        }

        if (npcQuestUI == null || focusedInteractable == null)
        {
            npcQuestUI?.HideInteractionPrompt();
            return;
        }

        string message = focusedInteractable.GetPromptMessage(gameObject);

        if (string.IsNullOrWhiteSpace(message))
        {
            npcQuestUI.HideInteractionPrompt();
            return;
        }

        npcQuestUI.ShowInteractionPrompt(message);
    }

    private void OnDisable()
    {
        npcQuestUI?.HideInteractionPrompt();
    }
}