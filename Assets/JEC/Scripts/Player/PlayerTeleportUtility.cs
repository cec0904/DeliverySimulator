using System.Collections;
using Invector.vCharacterController;
using UnityEngine;

public static class PlayerTeleportUtility
{
    public static IEnumerator Teleport(
        Transform player,
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        if (player == null)
        {
            yield break;
        }

        GameObject playerObject = player.gameObject;
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
        vThirdPersonInput playerInput = playerObject.GetComponent<vThirdPersonInput>();
        vThirdPersonController playerController = playerObject.GetComponent<vThirdPersonController>();
        PlayerParkourController parkourController = playerObject.GetComponent<PlayerParkourController>();

        bool previousInputLock = playerInput != null && playerInput.lockCharacterInput;

        if (playerInput != null)
        {
            playerInput.lockCharacterInput = true;
        }

        if (parkourController != null)
        {
            parkourController.EndParkour();
        }

        if (playerController != null)
        {
            playerController.EndParkour();
        }

        StopRigidbody(playerRigidbody);
        yield return WaitForPhysicsOrNextFrame();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
            playerRigidbody.rotation = targetRotation;
        }
        else
        {
            player.SetPositionAndRotation(targetPosition, targetRotation);
        }

        Physics.SyncTransforms();
        ResetControllerState(playerController, targetPosition.y);
        StopRigidbody(playerRigidbody);

        if (playerRigidbody != null)
        {
            playerRigidbody.WakeUp();
        }

        yield return WaitForPhysicsOrNextFrame();

        ResetControllerState(playerController, targetPosition.y);
        StopRigidbody(playerRigidbody);

        if (playerInput != null)
        {
            playerInput.lockCharacterInput = previousInputLock;
        }
    }

    private static object WaitForPhysicsOrNextFrame()
    {
        return Time.timeScale > 0f ? new WaitForFixedUpdate() : null;
    }

    private static void StopRigidbody(Rigidbody rigidbody)
    {
        if (rigidbody == null)
        {
            return;
        }

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
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
