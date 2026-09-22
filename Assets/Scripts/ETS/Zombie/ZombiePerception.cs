using UnityEngine;

public class ZombiePerception : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform player;

    [Header("Vision Settings")]
    [SerializeField]
    private float viewRadius = 10f;

    [Range(0f, 360f)]
    [SerializeField]
    private float viewAngle = 100f;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Eye Settings")]
    [SerializeField]
    private float eyeHeight = 1.5f;

    public bool CanSeePlayer { get; private set; }

    public Transform Player => player;

    // cek penglihatan tiap frame
    private void Update()
    {
        DetectPlayer();
    }

    // deteksi player pakai jarak, fov, raycast
    private void DetectPlayer()
    {
        CanSeePlayer = false;

        if (player == null)
            return;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewRadius)
            return;

        Vector3 normalizedDirection = directionToPlayer.normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, normalizedDirection);

        if (angleToPlayer > viewAngle / 2f)
            return;

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = player.position + Vector3.up * 0.5f;
        Vector3 rayDirection = targetPosition - eyePosition;
        float rayDistance = rayDirection.magnitude;

        if (Physics.Raycast(eyePosition, rayDirection.normalized, rayDistance, obstacleMask))
            return;

        CanSeePlayer = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftBoundary = DirectionFromAngle(-viewAngle / 2f);
        Vector3 rightBoundary = DirectionFromAngle(viewAngle / 2f);

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

        if (player != null && CanSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, player.position + Vector3.up * 0.5f);
        }
    }

    // hitung arah batas fov dari sudut
    private Vector3 DirectionFromAngle(float angle)
    {
        float finalAngle = transform.eulerAngles.y + angle;

        return new Vector3(
            Mathf.Sin(finalAngle * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(finalAngle * Mathf.Deg2Rad)
        );
    }
}
