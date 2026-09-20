using UnityEngine;

public class PlayerVisionSensor : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform player;

    [SerializeField]
    private GunController gunController;

    [Header("Vision Settings")]

    [SerializeField]
    private float viewRadius = 8f;

    [Range(0f, 360f)]
    [SerializeField]
    private float viewAngle = 90f;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Eye Settings")]

    [SerializeField]
    private float eyeHeight = 1.2f;

    public bool CanSeePlayer { get; private set; }

    public bool CanSeeGunDrawn { get; private set; }

    public Vector3 PlayerPosition =>
        player != null ? player.position : transform.position;

    private void Update()
    {
        DetectPlayer();
    }

    // cek apakah player terlihat dan pegang gun
    private void DetectPlayer()
    {
        CanSeePlayer = false;
        CanSeeGunDrawn = false;

        if (player == null)
        {
            return;
        }

        Vector3 directionToPlayer =
            player.position - transform.position;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewRadius)
        {
            return;
        }

        Vector3 normalizedDirection = directionToPlayer.normalized;

        float angleToPlayer =
            Vector3.Angle(transform.forward, normalizedDirection);

        if (angleToPlayer > viewAngle / 2f)
        {
            return;
        }

        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        Vector3 targetPosition =
            player.position + Vector3.up * 0.5f;

        Vector3 rayDirection = targetPosition - eyePosition;
        float rayDistance = rayDirection.magnitude;

        if (Physics.Raycast(
            eyePosition,
            rayDirection.normalized,
            rayDistance,
            obstacleMask))
        {
            return;
        }

        CanSeePlayer = true;

        if (gunController != null)
        {
            CanSeeGunDrawn = gunController.IsGunDrawn;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        if (player == null)
        {
            return;
        }

        if (CanSeeGunDrawn)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
        else if (CanSeePlayer)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
