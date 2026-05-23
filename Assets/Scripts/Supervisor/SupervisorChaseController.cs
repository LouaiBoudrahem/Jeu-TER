using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SupervisorChaseController : MonoBehaviour
{
    private Transform targetPlayer;
    private float chaseDuration;
    private float elapsedTime = 0f;
    private NavMeshAgent navMeshAgent;
    private bool isChasing = true;

    public void Initialize(Transform player, float duration)
    {
        targetPlayer = player;
        chaseDuration = duration;

        // Setup NavMesh Agent
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }

        StartCoroutine(ChaseRoutine());
    }

    private IEnumerator ChaseRoutine()
    {
        while (isChasing && elapsedTime < chaseDuration)
        {
            elapsedTime += Time.deltaTime;

            if (targetPlayer != null && navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.SetDestination(targetPlayer.position);
            }

            // Check if chase duration is over
            if (elapsedTime >= chaseDuration)
            {
                isChasing = false;
                if (navMeshAgent != null)
                {
                    navMeshAgent.enabled = false;
                }
                
                // Destroy after a brief delay
                yield return new WaitForSeconds(0.5f);
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
