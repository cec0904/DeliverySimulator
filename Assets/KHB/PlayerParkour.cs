using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    private Animator animator;

    [Header("Sensor Settings")]
    public float detectionRange = 1.0f; // 벽 감지 거리
    public LayerMask obstacleLayer;     // 장애물 레이어 (예: Environment, Default 등)

    void Start()
    {
        // 캐릭터의 애니메이터 컴포넌트 가져오기
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        bool isHit = Physics.Raycast(rayStart, transform.forward, out hit, detectionRange, obstacleLayer);

        if (isHit)
        {
            //Debug.DrawRay(rayStart, transform.forward * detectionRange, Color.green);

            if (Input.GetKeyDown(KeyCode.Space))
            { 
                StartParkour();
            }
        }
    }

    void StartParkour()
    {
        Debug.Log($"[Parkour]");
        // 애니메이터의 isParkour 파라미터를 true로 설정
        animator.SetBool("isParkour", true);

        // 캐릭터의 기본 이동 컨트롤러가 튀지 않도록 잠시 비활성화하는 처리가 필요할 수 있습니다.
        // 예: GetComponent<vThirdPersonController>().enabled = false;

        // 3초 뒤(혹은 애니메이션이 끝날 때) 다시 원상태로 돌리는 코루틴 실행
        StartCoroutine(ResetParkourState(1.5f)); // 1.5초는 애니메이션 길이에 맞게 조절
    }

    System.Collections.IEnumerator ResetParkourState(float delay)
    {
        yield return new WaitForSeconds(delay);

        animator.SetBool("isParkour", false);

        // 비활성화했던 컨트롤러 다시 켜기
        // GetComponent<vThirdPersonController>().enabled = true;
    }
}
