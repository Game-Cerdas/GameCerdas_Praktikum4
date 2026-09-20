using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshPathDebugger : MonoBehaviour
{
    private NavMeshAgent agent;

    public Color lineColor = Color.cyan;

    [Header("Status Path (bagian 43)")]
    public bool logStatus = false;
    public float logInterval = 1f;

    private float nextLogTime;

    private void Awake()
    {
        agent =
            GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!logStatus || Time.time < nextLogTime)
        {
            return;
        }

        nextLogTime = Time.time + logInterval;

        Debug.Log(
            $"Pending: {agent.pathPending}, " +
            $"Has Path: {agent.hasPath}, " +
            $"Remaining: {agent.remainingDistance:F2}, " +
            $"Status: {agent.pathStatus}, " +
            $"Velocity: {agent.velocity.magnitude:F2}"
        );
    }

    private void OnDrawGizmos()
    {
        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }

        if (agent == null ||
            !agent.hasPath)
        {
            return;
        }

        Vector3[] corners =
            agent.path.corners;

        Gizmos.color = lineColor;

        for (int i = 0;
             i < corners.Length - 1;
             i++)
        {
            Gizmos.DrawLine(
                corners[i],
                corners[i + 1]
            );

            Gizmos.DrawSphere(
                corners[i],
                0.12f
            );
        }

        if (corners.Length > 0)
        {
            Gizmos.DrawSphere(
                corners[corners.Length - 1],
                0.12f
            );
        }
    }
}
