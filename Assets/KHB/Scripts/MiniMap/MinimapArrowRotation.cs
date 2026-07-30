using UnityEngine;

public class MinimapArrowRotation : MonoBehaviour
{
    [Header("[ 추적 대상 ]")]
    [SerializeField] private Transform playerTransform; // 실제 3D 플레이어 Transform

    [Header("[ 회전축 설정 ]")]
    [SerializeField] private bool ignoreXRotation = true;
    [SerializeField] private bool ignoreZRotation = true;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || rectTransform == null) return;

        float playerYaw = playerTransform.eulerAngles.y;

        float uiRotationZ = -playerYaw;

        rectTransform.localRotation = Quaternion.Euler(0f, 0f, uiRotationZ);
    }
}
