using UnityEngine;

public class Player_Interact : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private float castRadius = 0.5f;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, castRadius, direction, out RaycastHit hitInfo,
                interactionDistance, interactableLayer, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = hitInfo.collider.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
            }
        }
    }
}
