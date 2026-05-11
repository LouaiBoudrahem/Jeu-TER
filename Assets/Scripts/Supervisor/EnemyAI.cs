using UnityEngine;

public enum EnemyState { Patrol, Investigate, Chase, Catch }

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float sightRange = 10f;
    [SerializeField] private float sightAngle = 90f;  
    [SerializeField] private float hearingRange = 5f;
    [SerializeField] private float catchRange = 1f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitAtPointDuration = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeDuration = 0.12f;
    [SerializeField] private string walkStateName = "Walking";
    [SerializeField] private string runStateName = "Running";
    [SerializeField] private string catchStateName = "Catching";
    [SerializeField] private string lookStateNameA = "Looking";
    [SerializeField] private string lookStateNameB = "Looking2";

    [Header("References")]
    [SerializeField] private LayerMask obstacleMask;  
    [SerializeField] private PlayerCharacter playerCharacter;

    private UnityEngine.AI.NavMeshAgent agent;
    private EnemyState state = EnemyState.Patrol;
    private Transform player;

    private int patrolIndex = 0;
    private int patrolDirection = 1;
    private Vector3 lastKnownPosition;
    private float investigateTimer;
    private float waitTimer = 0f;
    private bool isWaiting = false;    
    private int currentLookAnimation = -1;
    private string currentAnimationState = string.Empty;
    private int walkStateHash;
    private int runStateHash;
    private int catchStateHash;
    private int lookStateHashA;
    private int lookStateHashB;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        CacheAnimationHashes();

        playerCharacter = playerCharacter != null ? playerCharacter : FindObjectOfType<PlayerCharacter>();
        player = playerCharacter != null ? playerCharacter.transform : null;

        if (agent != null)
            agent.isStopped = false;

        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (agent == null || player == null)
            return;

        switch (state)
        {
            case EnemyState.Patrol:     UpdatePatrol();     break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Chase:      UpdateChase();      break;
            case EnemyState.Catch:      UpdateCatch();      break;
        }

        if (state != EnemyState.Catch)
            CheckPerception();
    }

    private void CheckPerception()
    {
        if (CanSeePlayer())
        {
            lastKnownPosition = player.position;
            SetState(EnemyState.Chase);
            return;
        }

        float noise = playerCharacter != null ? playerCharacter.GetNoiseLevel() : 0f;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < hearingRange * noise && state == EnemyState.Patrol)
        {
            lastKnownPosition = player.position;
            SetState(EnemyState.Investigate);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > sightAngle) return false;

        if (Physics.Raycast(transform.position + Vector3.up, 
            toPlayer.normalized, dist, obstacleMask))
            return false;

        return true;
    }

    private void UpdateInvestigate()
    {
        agent.speed = patrolSpeed;
        PlayAnimation(walkStateHash, walkStateName);
        agent.SetDestination(lastKnownPosition);

        investigateTimer += Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (investigateTimer > 3f)
            {
                investigateTimer = 0f;
                SetState(EnemyState.Patrol);
                GoToNextPatrolPoint();
            }
        }
    }

    private void UpdateChase()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        PlayAnimation(runStateHash, runStateName);
        agent.SetDestination(player.position);

        if (!CanSeePlayer())
        {
            agent.SetDestination(lastKnownPosition);
            SetState(EnemyState.Investigate);
            return;
        }

        if (Vector3.Distance(transform.position, player.position) < catchRange)
            SetState(EnemyState.Catch);
    }

    private void UpdateCatch()
    {
        agent.isStopped = true;
    }

    private void SetState(EnemyState newState)
    {
        if (state == newState)
            return;

        state = newState;

        if (state == EnemyState.Catch)
        {
            agent.isStopped = true;
            PlayAnimation(catchStateHash, catchStateName);
            Debug.Log("Player caught!");
            return;
        }

        agent.isStopped = false;

        if (state == EnemyState.Patrol)
        {
            investigateTimer = 0f;
            isWaiting = false;
            waitTimer = 0f;
            currentLookAnimation = -1;
        }

        if (state == EnemyState.Investigate)
            investigateTimer = 0f;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (patrolIndex < 0 || patrolIndex >= patrolPoints.Length)
            patrolIndex = 0;

        if (patrolPoints[patrolIndex] == null)
            return;

        isWaiting = false;
        waitTimer = 0f;
        PlayAnimation(walkStateHash, walkStateName);
        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (patrolIndex + patrolDirection >= patrolPoints.Length || 
            patrolIndex + patrolDirection < 0)
            patrolDirection *= -1;

        patrolIndex += patrolDirection;
    }

    private void UpdatePatrol()
    {
        agent.speed = patrolSpeed;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitAtPointDuration)
                GoToNextPatrolPoint();
            else
                PlayLookAroundAnimation();

            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            isWaiting = true;
            waitTimer = 0f;
            PlayLookAroundAnimation();
            return;
        }

        PlayAnimation(walkStateHash, walkStateName);
    }

    private void PlayLookAroundAnimation()
    {
        if (string.IsNullOrWhiteSpace(lookStateNameA) && string.IsNullOrWhiteSpace(lookStateNameB))
            return;

        if (!string.IsNullOrWhiteSpace(lookStateNameA) && !string.IsNullOrWhiteSpace(lookStateNameB))
        {
            if (currentLookAnimation == -1)
                currentLookAnimation = Random.Range(0, 2);
            else
                currentLookAnimation = 1 - currentLookAnimation;

            PlayAnimation(currentLookAnimation == 0 ? lookStateHashA : lookStateHashB, currentLookAnimation == 0 ? lookStateNameA : lookStateNameB);
            return;
        }

        bool useA = !string.IsNullOrWhiteSpace(lookStateNameA);
        PlayAnimation(useA ? lookStateHashA : lookStateHashB, useA ? lookStateNameA : lookStateNameB);
    }

    private void PlayAnimation(int stateHash, string stateName)
    {
        if (animator == null || stateHash == 0 || currentAnimationState == stateName)
            return;

        animator.CrossFadeInFixedTime(stateHash, crossFadeDuration);
        currentAnimationState = stateName;
    }

    private void CacheAnimationHashes()
    {
        walkStateHash = string.IsNullOrWhiteSpace(walkStateName) ? 0 : Animator.StringToHash(walkStateName);
        runStateHash = string.IsNullOrWhiteSpace(runStateName) ? 0 : Animator.StringToHash(runStateName);
        catchStateHash = string.IsNullOrWhiteSpace(catchStateName) ? 0 : Animator.StringToHash(catchStateName);
        lookStateHashA = string.IsNullOrWhiteSpace(lookStateNameA) ? 0 : Animator.StringToHash(lookStateNameA);
        lookStateHashB = string.IsNullOrWhiteSpace(lookStateNameB) ? 0 : Animator.StringToHash(lookStateNameB);
    }

    private void OnValidate()
    {
        CacheAnimationHashes();
    }

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);

            int next = (i + 1) % patrolPoints.Length;
            if (patrolPoints[next] != null)
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
