using System.Collections.Generic;
using UnityEngine;

public class AgentPathFollower : MonoBehaviour
{
    public AStarPathfinder pathfinder;

    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;
    public float waypointThreshold = 0.1f;

    private List<GridNode> path;
    private int currentIndex;

    private void Start()
    {
        if (pathfinder == null)
        {
            return;
        }

        path = pathfinder.currentPath;
        currentIndex = 0;
    }

    private void Update()
    {
        // Tambahan: jika pathfinder menghasilkan path baru
        // (misalnya goal dipindah lewat ClickGoalSetter),
        // agent otomatis mengikuti path baru tersebut.
        if (pathfinder != null &&
            pathfinder.currentPath != path)
        {
            path = pathfinder.currentPath;
            currentIndex = 0;
        }

        if (path == null ||
            path.Count == 0 ||
            currentIndex >= path.Count)
        {
            return;
        }

        Vector3 target =
            path[currentIndex].worldPosition;

        target.y = transform.position.y;

        Vector3 direction =
            target - transform.position;

        if (direction.magnitude <=
            waypointThreshold)
        {
            currentIndex++;
            return;
        }

        Vector3 moveDirection =
            direction.normalized;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

        if (moveDirection.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }
}
