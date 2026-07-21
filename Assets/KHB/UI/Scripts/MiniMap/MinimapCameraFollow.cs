using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("[ 추적 대상 ]")]
    [SerializeField] private Transform target; // 플레이어 Transform

    [Header("[ 시점 기준 카메라 ]")]
    [SerializeField] private Transform mainCameraTransform; // 메인 카메라 Transform

    [Header("[ 카메라 높이 ]")]
    [SerializeField] private float cameraHeight = 150.0f;

    private void LateUpdate()
    {
        if (target == null) return;
        // 플레이어의 X, Z 위치만 따라가고 Y(높이)는 고정
        Vector3 newPosition = new Vector3(target.position.x, target.position.y + cameraHeight, target.position.z);
        transform.position = newPosition;
        if (mainCameraTransform != null)
        {
            // 미니맵 카메라는 아래를 내려다보므로(X=90), Y축 회전값에 메인 카메라의 Y 회전값을 대입합니다.
            float currentCameraYaw = mainCameraTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(90f, currentCameraYaw, 0f);
        }
    }
}
