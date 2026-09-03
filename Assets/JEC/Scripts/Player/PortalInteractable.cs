using System.Collections;
using UnityEngine;

public class PortalInteractable : Interactable
{
    public override bool RequiresTrigger => true;

    [SerializeField] private Transform destination;
    [SerializeField] private bool matchDestinationRotation = true;

    [Tooltip("도착 시 플레이어가 바닥에 겹치지 않도록 조금 위로 올립니다.")]
    [SerializeField, Min(0f)] private float verticalOffset = 0.15f;

    private bool isTeleporting;

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || isTeleporting)
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogError(
                $"[{name}] 포탈 목적지가 지정되지 않았습니다.",
                this
            );
            return;
        }

        StartCoroutine(TeleportRoutine(interactor));
    }

    private IEnumerator TeleportRoutine(GameObject interactor)
    {
        isTeleporting = true;

        Vector3 targetPosition =
            destination.position + Vector3.up * verticalOffset;

        Quaternion targetRotation = matchDestinationRotation
            ? destination.rotation
            : interactor.transform.rotation;

        Debug.Log(
            $"[{name}] {interactor.name}: " +
            $"{interactor.transform.position} -> " +
            $"{destination.name} {targetPosition}",
            this
        );

        yield return PlayerTeleportUtility.Teleport(
            interactor.transform,
            targetPosition,
            targetRotation
        );

        isTeleporting = false;
    }
}
