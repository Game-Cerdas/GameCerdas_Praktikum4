using UnityEngine;
using UnityEngine.AI;

// Integrasi Praktikum 1-4 (bagian 51 modul):
//   Perception (PlayerVisionSensor)  -> Decision (chase / tidak)
//   -> Pathfinding (NavMeshAgent)    -> Movement (steering saat tidak mengejar)
//
// NavMeshAgent dan SteeringAgent sama-sama menggerakkan Transform,
// jadi hanya satu yang aktif pada satu waktu.
[RequireComponent(typeof(NavMeshAgent))]
public class HybridNavAgent : MonoBehaviour
{
    [Header("Referensi")]
    public Transform player;
    public PlayerVisionSensor visionSensor;
    public SteeringAgent steeringAgent;

    [Header("Chase")]
    public float chaseSpeed = 4.5f;
    public float stoppingDistance = 1.5f;

    [Header("Memory (Praktikum 2)")]
    [Tooltip("Berapa lama NPC masih mengejar posisi terakhir setelah Player hilang dari pandangan.")]
    public float memoryDuration = 3f;

    [Header("Repathing")]
    public float repathInterval = 0.25f;

    private NavMeshAgent agent;

    private bool isChasing;
    private float memoryTimer;
    private float nextRepathTime;
    private Vector3 lastKnownPosition;

    public bool IsChasing => isChasing;
    public Vector3 LastKnownPosition => lastKnownPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;
    }

    private void Start()
    {
        SetChasing(false);
    }

    private void Update()
    {
        bool canSee = visionSensor != null && visionSensor.CanSeePlayer;

        if (canSee)
        {
            lastKnownPosition = player != null
                ? player.position
                : visionSensor.PlayerPosition;

            memoryTimer = memoryDuration;

            if (!isChasing)
            {
                SetChasing(true);
            }
        }
        else if (isChasing)
        {
            memoryTimer -= Time.deltaTime;

            if (memoryTimer <= 0f)
            {
                SetChasing(false);
            }
        }

        if (!isChasing)
        {
            return;
        }

        if (Time.time < nextRepathTime)
        {
            return;
        }

        nextRepathTime = Time.time + repathInterval;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(lastKnownPosition);
        }
    }

    private void SetChasing(bool value)
    {
        isChasing = value;

        if (value)
        {
            if (steeringAgent != null)
            {
                steeringAgent.enabled = false;
            }

            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
            }
            else
            {
                Debug.LogWarning("NPC tidak berada di atas NavMesh.", this);
            }

            nextRepathTime = Time.time + repathInterval;
        }
        else
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.enabled = false;

            if (steeringAgent != null)
            {
                steeringAgent.enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !isChasing)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, lastKnownPosition);
        Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);
    }
}
