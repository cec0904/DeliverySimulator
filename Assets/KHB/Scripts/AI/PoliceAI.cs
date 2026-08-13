using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using rayzngames;
using UnityEngine.SceneManagement;

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
    private bool isTargetOnNavMesh = true;
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

        // 1. 10초 추격 제한 타이머
        currentChaseTimer += Time.deltaTime;
        if (currentChaseTimer >= maxChaseTime)
        {
            GiveUpChase();
            return;
        }

        if ((transform.position - targetToChase.position).sqrMagnitude <= arrestDistance * arrestDistance)
        {
            ArrestPlayer();
            return;
        }

        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval)
        {
            pathUpdateTimer = 0f;

            isTargetOnNavMesh = NavMesh.SamplePosition(targetToChase.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas);

            if (isTargetOnNavMesh)
            {
                if (!agent.enabled) agent.enabled = true;
                agent.SetDestination(targetToChase.position);
            }
            else
            {
                if (agent.enabled) agent.enabled = false;
            }
        }

        // 3. 오프로드 직접 이동 처리 (이동 자체는 부드럽게 매 프레임 처리되어야 함)
        if (!isTargetOnNavMesh)
        {
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
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
        }
        SceneManager.LoadScene(
        SceneManager.GetActiveScene().buildIndex);
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