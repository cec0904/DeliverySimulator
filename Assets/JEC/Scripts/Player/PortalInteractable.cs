using System.Collections;
using Invector.vCharacterController;
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

        Rigidbody playerRigidbody =
            interactor.GetComponent<Rigidbody>();

        vThirdPersonInput playerInput =
            interactor.GetComponent<vThirdPersonInput>();

        vThirdPersonController playerController =
            interactor.GetComponent<vThirdPersonController>();

        PlayerParkourController parkourController =
            interactor.GetComponent<PlayerParkourController>();

        bool previousInputLock =
            playerInput != null && playerInput.lockCharacterInput;

        if (playerInput != null)
        {
            playerInput.lockCharacterInput = true;
        }

        // 진행 중인 파쿠르와 파쿠르용 물리 상태를 먼저 정상 복구합니다.
        if (parkourController != null)
        {
            parkourController.EndParkour();
        }

        if (playerController != null)
        {
            playerController.EndParkour();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // 파쿠르 종료로 복구된 Collider/Rigidbody 상태를 물리 프레임에 반영합니다.
        yield return new WaitForFixedUpdate();

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

        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
            playerRigidbody.rotation = targetRotation;
        }
        else
        {
            interactor.transform.SetPositionAndRotation(
                targetPosition,
                targetRotation
            );
        }

        Physics.SyncTransforms();

        ResetControllerState(playerController, targetPosition.y);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.WakeUp();
        }

        // 도착지 바닥과 Capsule Collider의 충돌을 한 번 계산시킵니다.
        yield return new WaitForFixedUpdate();

        ResetControllerState(playerController, targetPosition.y);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (playerInput != null)
        {
            playerInput.lockCharacterInput = previousInputLock;
        }

        isTeleporting = false;
    }

    private static void ResetControllerState(
        vThirdPersonController controller,
        float destinationHeight)
    {
        if (controller == null)
        {
            return;
        }

        controller.input = Vector3.zero;
        controller.inputSmooth = Vector3.zero;
        controller.moveDirection = Vector3.zero;

        controller.isJumping = false;
        controller.isSprintJumping = false;
        controller.isSprinting = false;

        controller.isGrounded = true;
        controller.groundDistance = 0f;
        controller.verticalVelocity = 0f;
        controller.heightReached = destinationHeight;
    }
}