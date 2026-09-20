using UnityEngine;

public class AnimalSteeringAgent : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform target;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Panic")]

    [SerializeField]
    private float panicRadius = 5f;

    [Header("Flee")]

    [SerializeField]
    private float fleeSpeed = 6f;

    [Header("Wander")]

    [SerializeField]
    private float wanderSpeed = 2.5f;

    [SerializeField]
    private float wanderChangeInterval = 1.5f;

    [SerializeField]
    private float wanderAngleChange = 45f;

    [Header("Obstacle Avoidance")]

    [SerializeField]
    private SteeringSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    [Header("Boundary")]

    [SerializeField]
    private GroundBoundary groundBoundary;

    [SerializeField]
    private float boundaryEscapeWeight = 3f;

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    public Vector3 Velocity => velocity;

    private void Start()
    {
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;
    }

    private void Update()
    {
        Vector3 desiredVelocity;

        if (target == null)
        {
            desiredVelocity = CalculateWander();
        }
        else
        {
            Vector3 toTarget =
                target.position - transform.position;

            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance < panicRadius)
            {
                desiredVelocity = CalculateFlee();
            }
            else
            {
                desiredVelocity = CalculateWander();
            }
        }

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        desiredVelocity =
            ApplyBoundaryEscape(desiredVelocity);

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        velocity =
            Vector3.ClampMagnitude(
                velocity,
                maxSpeed
            );

        ApplyMovement();

        UpdateRotation();
    }

    // Hitung arah menjauhi target
    private Vector3 CalculateFlee()
    {
        Vector3 awayFromTarget =
            transform.position - target.position;

        awayFromTarget.y = 0f;

        if (awayFromTarget.sqrMagnitude < 0.001f)
        {
            return transform.forward * fleeSpeed;
        }

        return awayFromTarget.normalized * fleeSpeed;
    }

    // Hitung arah wander acak
    private Vector3 CalculateWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            float randomAngle =
                Random.Range(
                    -wanderAngleChange,
                    wanderAngleChange
                );

            wanderDirection =
                Quaternion.Euler(
                    0f,
                    randomAngle,
                    0f
                ) * transform.forward;

            wanderDirection.y = 0f;
            wanderDirection.Normalize();

            wanderTimer = wanderChangeInterval;
        }

        return wanderDirection * wanderSpeed;
    }

    // Terapkan penghindaran rintangan
    private Vector3 ApplyObstacleAvoidance(
        Vector3 desiredVelocity)
    {
        if (sensor == null)
        {
            return desiredVelocity;
        }

        Vector3 checkDirection =
            desiredVelocity.sqrMagnitude > 0.001f
                ? desiredVelocity.normalized
                : transform.forward;

        Vector3 avoidanceDirection =
            sensor.GetAvoidanceDirection(
                checkDirection
            );

        if (avoidanceDirection.sqrMagnitude > 0.001f)
        {
            Vector3 combinedDirection =
                checkDirection +
                avoidanceDirection *
                avoidanceWeight;

            combinedDirection.y = 0f;

            if (combinedDirection.sqrMagnitude > 0.001f)
            {
                combinedDirection.Normalize();
            }

            float desiredSpeed =
                Mathf.Max(
                    desiredVelocity.magnitude,
                    wanderSpeed
                );

            return combinedDirection * desiredSpeed;
        }

        return desiredVelocity;
    }

    // Gabungkan dorongan menjauh dari tepi map
    private Vector3 ApplyBoundaryEscape(
        Vector3 desiredVelocity)
    {
        if (groundBoundary == null)
        {
            return desiredVelocity;
        }

        Vector3 escapeDirection =
            groundBoundary.GetEscapeDirection(
                transform.position
            );

        if (escapeDirection.sqrMagnitude < 0.0001f)
        {
            return desiredVelocity;
        }

        Vector3 combined =
            desiredVelocity +
            escapeDirection.normalized *
            boundaryEscapeWeight;

        combined.y = 0f;

        float desiredSpeed =
            Mathf.Max(
                desiredVelocity.magnitude,
                maxSpeed * 0.5f
            );

        return combined.normalized * desiredSpeed;
    }

    // Terapkan movement dan batasi ke ground
    private void ApplyMovement()
    {
        transform.position +=
            velocity * Time.deltaTime;

        if (groundBoundary != null)
        {
            transform.position =
                groundBoundary.ClampPosition(transform.position);
        }
    }

    // Perbarui rotasi hadap arah
    private void UpdateRotation()
    {
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                horizontalVelocity.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            panicRadius
        );

        if (target != null)
        {
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }
    }
}
