using UnityEngine;
using Invector.vCharacterController;

public class CameraZoomin : MonoBehaviour
{
    private vThirdPersonCamera tpCamera;

    [Header("줌 속도 및 범위 설정")]
    public float zoomSpeed = 2.0f;       // 마우스 휠 감도
    public float minDistance = 1.0f;    // 가장 가까운 거리 (줌인 한계)
    public float maxDistance = 8.0f;    // 가장 먼 거리 (줌아웃 한계)

    void Start()
    {
        // 동일한 오브젝트에 붙은 인벡터 카메라 스크립트를 가져옵니다.
        tpCamera = GetComponent<vThirdPersonCamera>();

        if (tpCamera == null)
        {
            Debug.LogError("vThirdPersonCamera를 찾을 수 없습니다! 이 스크립트는 인벡터 카메라 오브젝트에 붙여야 합니다.");
        }
    }

    void Update()
    {
        if (tpCamera == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            float newDistance = tpCamera.defaultDistance - (scrollInput * zoomSpeed);

            tpCamera.defaultDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        }
    }
}
