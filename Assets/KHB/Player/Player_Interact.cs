using UnityEngine;

public class Player_Interact : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private float interactionDistance = 1.5f; 
    [SerializeField] private LayerMask interactableLayer;

    private Interactable currentInteractable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    void Update()
    {
        // 캐릭터의 발밑 대신 허리/가슴 높이쯤에서 발사하도록 시작점 조정
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;
        float thickness = 0.5f; // 두께 (반지름)

        RaycastHit hitInfo;

        // Raycast 대신 SphereCast 사용
        if (Physics.SphereCast(origin, thickness, direction, out hitInfo, interactionDistance, interactableLayer))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null && Input.GetKeyDown(KeyCode.F))
            {
                interactable.Interact();
            }
        }
    }
}
