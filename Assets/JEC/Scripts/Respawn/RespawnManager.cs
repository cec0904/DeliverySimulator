using System.Collections;
using Invector.vCharacterController;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RespawnManager : MonoBehaviour
{
    [Header("Respawn Points")]
    [SerializeField] private Transform policeRespawnPoint;
    [SerializeField] private Transform hospitalRespawnPoint;
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float playerVerticalOffset = 0.15f;

    [Header("Fade (total default: 5 seconds)")]
    [SerializeField] private RespawnFadeUI fadeUI;
    [SerializeField, Min(0f)] private float fadeInDuration = 1f;
    [SerializeField, Min(0f)] private float blackDuration = 3f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;

    [Header("Notification")]
    [SerializeField, Min(0f)] private float notificationDuration = 3f;

    private static RespawnManager instance;
    private bool isRespawning;
    private vThirdPersonInput lockedInput;
    private bool inputWasEnabled;
    private bool inputWasLocked;
    private Player_Interact lockedInteraction;
    private bool interactionWasEnabled;

    public bool IsRespawning => isRespawning;
    public static bool IsTransitionActive => instance != null && instance.isRespawning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("씬에 RespawnManager가 둘 이상 존재합니다.", this);
            enabled = false;
            return;
        }

        instance = this;

        if (fadeUI != null)
        {
            fadeUI.SetImmediate(0f, false);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnDisable()
    {
        if (!isRespawning) return;
        StopAllCoroutines();
        RestoreInput();
        if (fadeUI != null) fadeUI.SetImmediate(0f, false);
        isRespawning = false;
    }

    public static bool TryRequestRespawn(
        RespawnReason reason,
        Transform playerOverride = null)
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<RespawnManager>();
        }

        if (instance == null)
        {
            Debug.LogError("RespawnManager가 씬에 없어 리스폰을 시작할 수 없습니다.");
            return false;
        }

        return instance.TryStartRespawn(reason, playerOverride);
    }

    private bool TryStartRespawn(RespawnReason reason, Transform playerOverride)
    {
        if (!isActiveAndEnabled || isRespawning)
        {
            return false;
        }

        Transform targetPoint = reason == RespawnReason.PoliceArrest
            ? policeRespawnPoint
            : hospitalRespawnPoint;

        Transform targetPlayer = playerOverride != null ? playerOverride : player;

        if (targetPlayer == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            targetPlayer = taggedPlayer != null ? taggedPlayer.transform : null;
        }

        if (targetPoint == null || targetPlayer == null || fadeUI == null || !fadeUI.IsReady)
        {
            Debug.LogError(
                $"RespawnManager 필수 참조가 비어 있습니다. " +
                $"Player={targetPlayer != null}, Point={targetPoint != null}, FadeUI={fadeUI != null && fadeUI.IsReady}",
                this
            );
            return false;
        }

        player = targetPlayer;
        StartCoroutine(RespawnRoutine(reason, targetPoint, targetPlayer));
        return true;
    }

    private IEnumerator RespawnRoutine(
        RespawnReason reason,
        Transform targetPoint,
        Transform targetPlayer)
    {
        isRespawning = true;
        lockedInteraction = targetPlayer.GetComponent<Player_Interact>();
        if (lockedInteraction != null)
        {
            interactionWasEnabled = lockedInteraction.enabled;
            lockedInteraction.enabled = false;
        }

        MotorbikeMount mountedBike = targetPlayer.GetComponentInParent<MotorbikeMount>();
        try
        {
            if (mountedBike != null && mountedBike.IsMounted)
            {
                mountedBike.PrepareForRespawn();
            }
            else
            {
                LockOnFootInput(targetPlayer);
            }

            yield return fadeUI.FadeToBlack(fadeInDuration);
            float blackStartedAt = Time.unscaledTime;

            if (mountedBike != null && mountedBike.IsMounted)
            {
                mountedBike.TryDismountForRespawn();
                // Capture the restored ON-FOOT state, not the mounted input lock.
                LockOnFootInput(targetPlayer);
            }

            if (targetPlayer == null || targetPoint == null)
            {
                Debug.LogError("리스폰 도중 플레이어 또는 목적지가 제거되었습니다.", this);
                yield break;
            }

            Vector3 destination = targetPoint.position + Vector3.up * playerVerticalOffset;
            yield return PlayerTeleportUtility.Teleport(targetPlayer, destination, targetPoint.rotation);

            float remainingBlackTime = blackDuration - (Time.unscaledTime - blackStartedAt);
            if (remainingBlackTime > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingBlackTime);
            }

            yield return fadeUI.FadeFromBlack(fadeOutDuration);
        }
        finally
        {
            if (fadeUI != null) fadeUI.SetImmediate(0f, false);
            RestoreInput();
            isRespawning = false;
        }
        ShowCompletionMessage(reason);
    }

    private void LockOnFootInput(Transform targetPlayer)
    {
        if (targetPlayer == null) return;
        targetPlayer.GetComponent<PlayerParkourController>()?.EndParkour();
        targetPlayer.GetComponent<vThirdPersonController>()?.EndParkour();

        lockedInput = targetPlayer.GetComponent<vThirdPersonInput>();
        if (lockedInput != null)
        {
            inputWasEnabled = lockedInput.enabled;
            inputWasLocked = lockedInput.lockCharacterInput;
            lockedInput.lockCharacterInput = true;
            lockedInput.enabled = false;
        }

        foreach (vThirdPersonMotor motor in targetPlayer.GetComponents<vThirdPersonMotor>())
        {
            motor.input = Vector3.zero;
            motor.inputSmooth = Vector3.zero;
            motor.moveDirection = Vector3.zero;
            motor.isSprinting = false;
            motor.isJumping = false;
            motor.isSprintJumping = false;
        }

        Rigidbody body = targetPlayer.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreInput()
    {
        if (lockedInput != null)
        {
            lockedInput.lockCharacterInput = inputWasLocked;
            lockedInput.enabled = inputWasEnabled;
            lockedInput = null;
        }
        if (lockedInteraction != null)
        {
            lockedInteraction.enabled = interactionWasEnabled;
            lockedInteraction = null;
        }
    }

    private void ShowCompletionMessage(RespawnReason reason)
    {
        string message;

        switch (reason)
        {
            case RespawnReason.PoliceArrest:
                message = "경찰서에서 풀려났습니다.";
                break;
            case RespawnReason.CitizenCrash:
                message = "합의금을 물어주고 병문안을 다녀왔습니다.";
                break;
            default:
                message = "입원 후 퇴원했습니다.";
                break;
        }

        NpcQuestUIController notification = NpcQuestUIController.CreateIfMissing();
        if (notification != null) notification.ShowTimedInteractionPrompt(message, notificationDuration);
        else Debug.LogError("리스폰 알림용 NpcQuestUI 프리팹을 불러올 수 없습니다.", this);
    }

    private void OnValidate()
    {
        if (fadeUI == null)
        {
            fadeUI = GetComponentInChildren<RespawnFadeUI>(true);
        }
    }
}
