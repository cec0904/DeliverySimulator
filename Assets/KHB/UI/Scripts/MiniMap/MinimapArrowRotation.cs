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
        // 내 오브젝트의 RectTransform 컴포넌트를 가져옴
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 만약 플레이어가 지정되지 않았다면 "Player" 태그로 자동 검색
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

        // 스프라이트의 처음 기본 방향이 북쪽(위쪽)을 향하고 있다고 가정합니다.
        float uiRotationZ = -playerYaw;

        rectTransform.localRotation = Quaternion.Euler(0f, 0f, uiRotationZ);
    }
}
