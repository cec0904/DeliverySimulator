using UnityEngine;
using UnityEngine.AI;

public class NPCMoveTest : MonoBehaviour
{
    // NPC_Test에 붙어 있는 NavMesh Agent 컴포넌트를 저장할 변수
    private NavMeshAgent agent;

    // Hierarchy의 Target 오브젝트 위치를 받을 변수
    [SerializeField]    // private 상태를 유지하면서도 Unity Inspector에서 연결할 수 있게 하려는 것
    private Transform target;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(target.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
