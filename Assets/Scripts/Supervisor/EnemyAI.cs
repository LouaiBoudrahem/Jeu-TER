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

    [Header("Catch Sequence")]
    [SerializeField] private AudioSource catchAudioSource;
    [SerializeField] private GameObject catchCinemachineCamera;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float cameraLookAtDuration = 0.5f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float respawnWaitDuration = 1f;
    [SerializeField] private float catchTeleportDistance = 1f;

    [Header("Chase Audio")]
    [SerializeField] private AudioSource chaseAudioSource;

    [Header("Patrol Proximity Audio")]
    [SerializeField] private AudioSource patrolProximityAudioSource;
    [SerializeField] private float patrolProximityDistance = 3f;

    [Header("References")]
    [SerializeField] private LayerMask obstacleMask;  
    [SerializeField] private PlayerCharacter playerCharacter;
    private Player playerScript;

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
    private bool catchSequenceStarted = false;
    private bool catchRecoveryActive = false;
    private float catchRecoveryUntil = 0f;
    private bool chaseAudioPlaying = false;
    private bool patrolProximityAudioPlaying = false;
    private int patrolWaitLookStateHash;
    private string patrolWaitLookStateName = string.Empty;
    private bool useLookStateAOnNextWait = true;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        CacheAnimationHashes();

        playerCharacter = playerCharacter != null ? playerCharacter : FindObjectOfType<PlayerCharacter>();
        playerScript = playerCharacter != null ? playerCharacter.GetComponent<Player>() : null;
        if (playerScript == null)
            playerScript = FindObjectOfType<Player>();
        player = playerCharacter != null ? playerCharacter.transform : null;

        if (agent != null)
            agent.isStopped = false;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (agent == null || player == null)
            return;

        if (catchRecoveryActive && Time.time >= catchRecoveryUntil)
            catchRecoveryActive = false;

        switch (state)
        {
            case EnemyState.Patrol:     UpdatePatrol();     break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Chase:      UpdateChase();      break;
            case EnemyState.Catch:      UpdateCatch();      break;
        }

        if (state != EnemyState.Catch && !catchRecoveryActive)
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
        PlayChaseAudio();
        agent.SetDestination(player.position);

        if (!CanSeePlayer())
        {
            agent.SetDestination(lastKnownPosition);
            SetState(EnemyState.Investigate);
            return;
        }

        float sqrDist = (transform.position - player.position).sqrMagnitude;
        float catchSqr = catchRange * catchRange;
        bool closeByPosition = sqrDist <= catchSqr + 0.01f;
        bool closeByPath = (agent != null && !agent.pathPending && agent.remainingDistance <= catchRange + 0.01f);

        if (closeByPosition || closeByPath)
            SetState(EnemyState.Catch);
    }

    private void UpdateCatch()
    {
        agent.isStopped = true;
        StopChaseAudio();

        if (!catchSequenceStarted)
        {
            catchSequenceStarted = true;
            StartCoroutine(DoCatchSequence());
        }
    }

    private void SetState(EnemyState newState)
    {
        if (state == newState)
            return;

        state = newState;

        if (state == EnemyState.Catch)
        {
            agent.isStopped = true;
            StopChaseAudio();
            PlayAnimation(catchStateHash, catchStateName);
            Debug.Log("Player caught!");
            return;
        }

        agent.isStopped = false;

        if (state != EnemyState.Chase)
            StopChaseAudio();

        if (state != EnemyState.Patrol)
            StopPatrolProximityAudio();

        if (state == EnemyState.Patrol)
        {
            investigateTimer = 0f;
            isWaiting = false;
            waitTimer = 0f;
            currentLookAnimation = -1;
            patrolWaitLookStateHash = 0;
            patrolWaitLookStateName = string.Empty;
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

        // Patrol proximity audio: play only while patrolling and player is within distance
        if (player != null && patrolProximityAudioSource != null)
        {
            float pdist = Vector3.Distance(transform.position, player.position);
            if (pdist <= patrolProximityDistance && !patrolProximityAudioPlaying && !catchRecoveryActive)
            {
                PlayPatrolProximityAudio();
            }
            else if (pdist > patrolProximityDistance && patrolProximityAudioPlaying)
            {
                StopPatrolProximityAudio();
            }
        }

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitAtPointDuration)
                GoToNextPatrolPoint();

            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            isWaiting = true;
            waitTimer = 0f;
            StartPatrolWaitLookAnimation();
            return;
        }

        PlayAnimation(walkStateHash, walkStateName);
    }

    private void PlayPatrolProximityAudio()
    {
        if (patrolProximityAudioSource == null)
            return;

        patrolProximityAudioSource.loop = true;

        if (patrolProximityAudioPlaying)
            return;

        patrolProximityAudioSource.Play();
        patrolProximityAudioPlaying = true;
    }

    private void StopPatrolProximityAudio()
    {
        if (patrolProximityAudioSource == null)
            return;

        if (patrolProximityAudioSource.isPlaying)
            patrolProximityAudioSource.Stop();

        patrolProximityAudioPlaying = false;
    }

    private void StartPatrolWaitLookAnimation()
    {
        if (string.IsNullOrWhiteSpace(lookStateNameA) && string.IsNullOrWhiteSpace(lookStateNameB))
            return;

        if (!string.IsNullOrWhiteSpace(lookStateNameA) && !string.IsNullOrWhiteSpace(lookStateNameB))
        {
            if (currentLookAnimation == -1)
                currentLookAnimation = useLookStateAOnNextWait ? 0 : 1;
            else
                currentLookAnimation = 1 - currentLookAnimation;

            patrolWaitLookStateHash = currentLookAnimation == 0 ? lookStateHashA : lookStateHashB;
            patrolWaitLookStateName = currentLookAnimation == 0 ? lookStateNameA : lookStateNameB;
            useLookStateAOnNextWait = !useLookStateAOnNextWait;
            PlayAnimation(patrolWaitLookStateHash, patrolWaitLookStateName);
            return;
        }

        bool useA = !string.IsNullOrWhiteSpace(lookStateNameA);
        patrolWaitLookStateHash = useA ? lookStateHashA : lookStateHashB;
        patrolWaitLookStateName = useA ? lookStateNameA : lookStateNameB;
        PlayAnimation(patrolWaitLookStateHash, patrolWaitLookStateName);
    }

    private void PlayAnimation(int stateHash, string stateName)
    {
        if (animator == null || stateHash == 0 || currentAnimationState == stateName)
            return;

        animator.CrossFadeInFixedTime(stateHash, crossFadeDuration);
        currentAnimationState = stateName;
    }

    private void PlayChaseAudio()
    {
        if (chaseAudioSource == null)
            return;

        chaseAudioSource.loop = true;

        if (chaseAudioPlaying)
            return;

        chaseAudioSource.Play();
        chaseAudioPlaying = true;
    }

    private void StopChaseAudio()
    {
        if (chaseAudioSource == null)
            return;

        if (chaseAudioSource.isPlaying)
            chaseAudioSource.Stop();

        chaseAudioPlaying = false;
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

    private System.Collections.IEnumerator DoCatchSequence()
    {
        if (playerCharacter == null || player == null || playerScript == null)
            yield break;

        catchRecoveryActive = true;
        catchRecoveryUntil = Time.time + cameraLookAtDuration + fadeDuration + respawnWaitDuration + fadeDuration + 0.25f;

        playerScript.SetInputLocked(true);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = true;
        }

        if (catchAudioSource != null)
        {
            catchAudioSource.loop = false;
            catchAudioSource.Stop();
            catchAudioSource.Play();
        }

        Vector3 dir = transform.position - player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = playerCharacter != null ? playerCharacter.transform.forward : Vector3.forward;
        }
        dir.Normalize();
        Vector3 targetPos = player.position + dir * catchTeleportDistance;
        targetPos.y = player.position.y;

        if (agent != null)
        {
            agent.Warp(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }

        if (playerScript != null)
        {
            playerScript.BeginCinemachineCameraTransition(catchCinemachineCamera);
        }

        yield return new WaitForSeconds(cameraLookAtDuration);

        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        PlayAnimation(catchStateHash, catchStateName);
        SetState(EnemyState.Patrol);
        agent.isStopped = false;

        yield return new WaitForSeconds(respawnWaitDuration);

        if (playerScript != null)
        {
            if (playerSpawnPoint != null)
            {
                playerScript.RespawnAt(playerSpawnPoint);
            }
            else
            {
                Debug.LogWarning("EnemyAI: Player spawn point not assigned!");
            }

            playerScript.EndCinemachineCameraTransition();
        }

        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        playerScript.SetInputLocked(false);
        GoToNextPatrolPoint();
        catchSequenceStarted = false;
        StopChaseAudio();

        if (Time.time >= catchRecoveryUntil)
            catchRecoveryActive = false;
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
