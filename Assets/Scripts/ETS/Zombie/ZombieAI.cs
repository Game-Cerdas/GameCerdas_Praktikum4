using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public enum ZombieState
    {
        Wander,
        Chase
    }

    [Header("References")]
    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private ZombiePerception perception;

    [Header("Wander Settings")]
    [SerializeField]
    private float wanderRadius = 10f;

    [SerializeField]
    private float wanderInterval = 4f;

    [SerializeField]
    private float wanderSpeed = 1.5f;

    [Header("Chase Settings")]
    [SerializeField]
    private float chaseSpeed = 3.5f;

    [Header("Debug")]
    [SerializeField]
    private ZombieState currentState;

    private Vector3 spawnPosition;
    private float wanderTimer;

    // ambil reference dan posisi awal
    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (perception == null)
            perception = GetComponent<ZombiePerception>();

        spawnPosition = transform.position;
    }

    private void Start()
    {
        currentState = ZombieState.Wander;
        wanderTimer = 0f;
    }

    private void Update()
    {
        DecideState();
        ExecuteState();
    }

    // tentukan state dari penglihatan
    private void DecideState()
    {
        if (perception.CanSeePlayer)
        {
            currentState = ZombieState.Chase;
        }
        else
        {
            currentState = ZombieState.Wander;
        }
    }

    // jalankan perilaku sesuai state
    private void ExecuteState()
    {
        switch (currentState)
        {
            case ZombieState.Wander:
                Wander();
                break;

            case ZombieState.Chase:
                Chase();
                break;
        }
    }

    // jalan acak di sekitar titik spawn
    private void Wander()
    {
        agent.speed = wanderSpeed;

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            Vector3 randomPoint = GetRandomWanderPoint();
            agent.SetDestination(randomPoint);
            wanderTimer = wanderInterval;
        }
    }

    // cari titik acak yang valid di navmesh
    private Vector3 GetRandomWanderPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += spawnPosition;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return spawnPosition;
    }

    // kejar posisi player
    private void Chase()
    {
        agent.speed = chaseSpeed;

        if (perception.Player == null)
            return;

        agent.SetDestination(perception.Player.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = currentState == ZombieState.Chase ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
