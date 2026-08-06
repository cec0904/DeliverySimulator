using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PoliceAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("추격 대상 설정")]
    private Transform targetToChase;
    private bool isChasing = false;

    [Header("도보 추격 설정")]
    public float runSpeed = 6.0f;
    [SerializeField] private float arrestDistance = 1.5f;

    [Header("추격 포기 설정")]
    [SerializeField] private float maxChaseTime = 10.0f;
    private float currentChaseTimer = 0f;

    [Header("추격 최적화")]
    [SerializeField] private float pathUpdateInterval = 0.5f;
    private float pathUpdateTimer = 0f;

    [Header("경찰차")]
    private GameObject sourceCar;

    public void SetSourceCar(GameObject car)
    {
        sourceCar = car;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.stoppingDistance = arrestDistance;
        agent.updateRotation = true;
    }

    public void StartFootChase()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetToChase = playerObj.transform;

        if (targetToChase == null) return;

        isChasing = true;
        agent.speed = runSpeed;
        agent.isStopped = false;
    }

    private void Update()
    {
        UpdateAnimation();

        if (!isChasing || targetToChase == null) return;


        currentChaseTimer += Time.deltaTime;
        if (currentChaseTimer >= maxChaseTime)
        {
            GiveUpChase();
            return;
        }

        if (Vector3.Distance(transform.position, targetToChase.position) <= arrestDistance)
        {
            ArrestPlayer();
            return;
        }

        // 1. 플레이어가 내비메시(도로) 위에 있는지 확인
        bool isOnNavMesh = NavMesh.SamplePosition(targetToChase.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas);

        if (isOnNavMesh)
        {
            if (!agent.enabled) agent.enabled = true;

            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateInterval)
            {
                pathUpdateTimer = 0f;
                agent.SetDestination(targetToChase.position);
            }
        }
        else
        {
            if (agent.enabled) agent.enabled = false;

            Vector3 direction = (targetToChase.position - transform.position);
            direction.y = 0;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
            transform.position += direction.normalized * runSpeed * Time.deltaTime;
        }
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            float speed = agent.enabled ? agent.velocity.magnitude : runSpeed;
            animator.SetFloat("Speed", speed);
        }
    }

    private void ArrestPlayer()
    {
        isChasing = false;
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        Debug.Log("<color=blue>[경찰관 AI]</color> 플레이어 체포 완료!");
    }

    private void GiveUpChase()
    {
        isChasing = false;

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f); 
            animator.Play("idle_selfcheck_1_300f");
        }

        StartCoroutine(DestroyAfterAnimation("idle_selfcheck_1_300f"));
    }

    private IEnumerator DestroyAfterAnimation(string stateName)
    {
        yield return null;

        float waitTime = 3.0f; 

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(stateName))
            {
                waitTime = stateInfo.length; 
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (sourceCar != null)
        {
            Destroy(sourceCar);
        }

        Destroy(gameObject);
    }
}