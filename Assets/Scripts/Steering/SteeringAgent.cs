using System.Collections.Generic;
using UnityEngine;

public enum SteeringState
{
    Arrive,
    Wander,
    Avoiding,
    Flee,
    Pursue
}

public enum ChaseMode
{
    Arrive,
    Pursue
}

public class SteeringAgent : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform target;

    [SerializeField]
    private bool useTarget = true;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Arrive")]

    [SerializeField]
    private float slowRadius = 4f;

    [SerializeField]
    private float stopRadius = 1.5f;

    [Header("Level 4 - Pursue")]

    [SerializeField]
    private ChaseMode chaseMode = ChaseMode.Arrive;

    [SerializeField]
    private float maxPredictionTime = 1.5f;

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

    [Header("Level 2 - Flee")]

    [SerializeField]
    private bool enableFlee = true;

    [SerializeField]
    private float fleeRadius = 2.5f;

    [SerializeField]
    private float fleeExitBuffer = 1.5f;

    [SerializeField]
    private float fleeSpeedMultiplier = 1.3f;

    [Header("Level 3 - Separation")]

    [SerializeField]
    private bool enableSeparation = true;

    [SerializeField]
    private float separationRadius = 2.5f;

    [SerializeField]
    private float separationWeight = 1.5f;

    [Header("Boundary")]

    [SerializeField]
    private GroundBoundary groundBoundary;

    [SerializeField]
    private float boundaryEscapeWeight = 3f;

    [Header("Vision - Gun Flee")]

    [SerializeField]
    private PlayerVisionSensor visionSensor;

    [SerializeField]
    private float fleeSpeed = 6f;

    [Header("Help Call")]

    [SerializeField]
    private float alertWaitDuration = 2f;

    [Header("Gun Reaction")]

    [SerializeField]
    private float fleeProximityThreshold = 4f;

    [SerializeField]
    private float panicFleeSpeed = 7f;

    [SerializeField]
    private float panicFleeDuration = 6f;

    private enum GunReaction
    {
        None,
        Panicking
    }

    private static readonly List<SteeringAgent> activeAgents =
        new List<SteeringAgent>();

    private Vector3 velocity;
    private Vector3 wanderDirection;
    private float wanderTimer;

    private Vector3 targetVelocity;
    private Vector3 lastTargetPosition;
    private bool hasTargetSample;

    private Vector3 predictedTargetPosition;
    private bool isFleeing;
    private bool isFleeingFromGun;

    private bool isAlerted;
    private bool hasArrivedAtCaller;
    private float alertWaitTimer;

    private GunReaction gunReaction = GunReaction.None;
    private float panicTimer;

    private SteeringState currentState = SteeringState.Wander;

    public Vector3 Velocity => velocity;

    public SteeringState CurrentState => currentState;

    public Vector3 PredictedTargetPosition => predictedTargetPosition;

    public static IReadOnlyList<SteeringAgent> ActiveAgents => activeAgents;

    private void OnEnable()
    {
        if (!activeAgents.Contains(this))
        {
            activeAgents.Add(this);
        }
    }

    private void OnDisable()
    {
        activeAgents.Remove(this);
    }

    private void Start()
    {
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;

        if (target != null)
        {
            lastTargetPosition = target.position;
            hasTargetSample = true;
        }
    }

    private void Update()
    {
        TrackTargetVelocity();
        UpdateGunReaction();
        UpdateAlertState();

        Vector3 desiredVelocity =
            CalculatePrimaryBehaviour();

        desiredVelocity =
            ApplySeparation(desiredVelocity);

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        desiredVelocity =
            ApplyBoundaryEscape(desiredVelocity);

        float speedLimit = GetCurrentSpeedLimit();

        desiredVelocity =
            Vector3.ClampMagnitude(
                desiredVelocity,
                speedLimit
            );

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        velocity =
            Vector3.ClampMagnitude(
                velocity,
                speedLimit
            );

        ApplyMovement();
        UpdateRotation();
    }

    // ------------------------------------------------------------
    // Target velocity tracking (dipakai Pursue - Level 4)
    // ------------------------------------------------------------

    private void TrackTargetVelocity()
    {
        if (target == null)
        {
            targetVelocity = Vector3.zero;
            hasTargetSample = false;
            return;
        }

        Vector3 currentPosition = target.position;

        if (!hasTargetSample || Time.deltaTime <= 0f)
        {
            lastTargetPosition = currentPosition;
            hasTargetSample = true;
            return;
        }

        Vector3 frameVelocity =
            (currentPosition - lastTargetPosition) /
            Time.deltaTime;

        frameVelocity.y = 0f;

        targetVelocity =
            Vector3.Lerp(
                targetVelocity,
                frameVelocity,
                0.35f
            );

        lastTargetPosition = currentPosition;
    }

    // ------------------------------------------------------------
    // Help Call - dipanggil dari PlayerAlert
    // ------------------------------------------------------------

    // dipanggil saat dengar teriakan minta tolong
    public void OnHelpCallHeard(Transform caller)
    {
        if (target == null)
        {
            target = caller;
        }

        useTarget = true;
        isAlerted = true;
        hasArrivedAtCaller = false;
    }

    // kelola status bubar setelah sampai lokasi panggilan
    private void UpdateAlertState()
    {
        if (!isAlerted || target == null)
        {
            return;
        }

        float distance =
            Vector3.Distance(transform.position, target.position);

        if (!hasArrivedAtCaller)
        {
            if (distance <= stopRadius)
            {
                hasArrivedAtCaller = true;
                alertWaitTimer = alertWaitDuration;
            }

            return;
        }

        alertWaitTimer -= Time.deltaTime;

        if (alertWaitTimer <= 0f)
        {
            isAlerted = false;
            useTarget = false;
            hasArrivedAtCaller = false;
        }
    }

    // ------------------------------------------------------------
    // Pemilihan behaviour utama + penentuan state
    // ------------------------------------------------------------

    private Vector3 CalculatePrimaryBehaviour()
    {
        if (gunReaction == GunReaction.Panicking)
        {
            isFleeingFromGun = false;
            isFleeing = false;
            currentState = SteeringState.Flee;

            return CalculatePanicFlee();
        }

        if (visionSensor != null && visionSensor.CanSeeGunDrawn)
        {
            isFleeingFromGun = true;
            isFleeing = false;
            currentState = SteeringState.Flee;

            return CalculateFleeFromPlayer();
        }

        isFleeingFromGun = false;

        if (!useTarget || target == null)
        {
            isFleeing = false;
            currentState = SteeringState.Wander;
            return CalculateWander();
        }

        if (ShouldFlee())
        {
            currentState = SteeringState.Flee;
            return CalculateFlee();
        }

        if (chaseMode == ChaseMode.Pursue)
        {
            currentState = SteeringState.Pursue;
            return CalculatePursue();
        }

        currentState = SteeringState.Arrive;
        predictedTargetPosition = target.position;

        return CalculateArrive(target.position);
    }

    private bool ShouldFlee()
    {
        if (!enableFlee)
        {
            isFleeing = false;
            return false;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        // Histeresis: masuk flee di fleeRadius,
        // baru keluar setelah lewat fleeRadius + buffer.
        float exitRadius =
            fleeRadius + Mathf.Max(fleeExitBuffer, 0f);

        if (isFleeing)
        {
            isFleeing = distance < exitRadius;
        }
        else
        {
            isFleeing = distance < fleeRadius;
        }

        return isFleeing;
    }

    // ------------------------------------------------------------
    // Level 0 - Arrive
    // ------------------------------------------------------------

    private Vector3 CalculateArrive(Vector3 destination)
    {
        Vector3 toTarget = destination - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return Vector3.zero;
        }

        float desiredSpeed = maxSpeed;

        if (distance < slowRadius)
        {
            float range =
                Mathf.Max(
                    slowRadius - stopRadius,
                    0.001f
                );

            float normalizedDistance =
                (distance - stopRadius) / range;

            desiredSpeed =
                maxSpeed *
                Mathf.Clamp01(normalizedDistance);
        }

        return toTarget.normalized * desiredSpeed;
    }

    // ------------------------------------------------------------
    // Level 0 - Wander
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // Level 2 - Flee
    // ------------------------------------------------------------

    private Vector3 CalculateFlee()
    {
        Vector3 awayFromTarget =
            transform.position - target.position;

        awayFromTarget.y = 0f;

        if (awayFromTarget.sqrMagnitude < 0.001f)
        {
            // Posisi hampir menumpuk: kabur ke arah acak
            // supaya tidak terjadi pembagian nol.
            awayFromTarget =
                new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                );
        }

        predictedTargetPosition = target.position;

        return awayFromTarget.normalized *
               maxSpeed *
               Mathf.Max(fleeSpeedMultiplier, 0.01f);
    }

    // ------------------------------------------------------------
    // Vision - Flee dari player yang pegang gun
    // ------------------------------------------------------------

    // hitung arah menjauh dari player bersenjata
    private Vector3 CalculateFleeFromPlayer()
    {
        Vector3 playerPosition = visionSensor.PlayerPosition;

        Vector3 awayFromPlayer =
            transform.position - playerPosition;

        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            return transform.forward * fleeSpeed;
        }

        return awayFromPlayer.normalized * fleeSpeed;
    }

    // tentukan level reaksi terhadap gun berdasarkan jarak
    private void UpdateGunReaction()
    {
        if (visionSensor == null)
        {
            return;
        }

        if (gunReaction == GunReaction.None && visionSensor.CanSeeGunDrawn)
        {
            float distanceToPlayer =
                Vector3.Distance(
                    transform.position,
                    visionSensor.PlayerPosition
                );

            if (distanceToPlayer >= fleeProximityThreshold)
            {
                gunReaction = GunReaction.Panicking;
                panicTimer = panicFleeDuration;
            }

            return;
        }

        if (gunReaction == GunReaction.Panicking)
        {
            panicTimer -= Time.deltaTime;

            if (panicTimer <= 0f)
            {
                gunReaction = GunReaction.None;
            }
        }
    }

    // hitung arah lari panik dengan kecepatan lebih tinggi
    private Vector3 CalculatePanicFlee()
    {
        Vector3 playerPosition = visionSensor.PlayerPosition;

        Vector3 awayFromPlayer =
            transform.position - playerPosition;

        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            return transform.forward * panicFleeSpeed;
        }

        return awayFromPlayer.normalized * panicFleeSpeed;
    }

    // ------------------------------------------------------------
    // Level 4 - Pursue (Arrive ke posisi prediksi)
    // ------------------------------------------------------------

    private Vector3 CalculatePursue()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        float currentSpeed = velocity.magnitude;

        float predictionTime;

        if (currentSpeed <= distance / Mathf.Max(maxPredictionTime, 0.001f))
        {
            // NPC terlalu lambat / target terlalu jauh:
            // pakai prediksi maksimum.
            predictionTime = maxPredictionTime;
        }
        else
        {
            predictionTime = distance / Mathf.Max(currentSpeed, 0.001f);

            predictionTime =
                Mathf.Min(predictionTime, maxPredictionTime);
        }

        predictedTargetPosition =
            target.position + targetVelocity * predictionTime;

        return CalculateArrive(predictedTargetPosition);
    }

    // ------------------------------------------------------------
    // Level 3 - Separation antar-NPC
    // ------------------------------------------------------------

    private Vector3 ApplySeparation(Vector3 desiredVelocity)
    {
        if (!enableSeparation || separationRadius <= 0f)
        {
            return desiredVelocity;
        }

        Vector3 separation = Vector3.zero;
        int neighbourCount = 0;

        for (int i = 0; i < activeAgents.Count; i++)
        {
            SteeringAgent other = activeAgents[i];

            if (other == null || other == this)
            {
                continue;
            }

            Vector3 offset =
                transform.position - other.transform.position;

            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance > separationRadius)
            {
                continue;
            }

            if (distance < 0.0001f)
            {
                offset =
                    new Vector3(
                        Random.Range(-1f, 1f),
                        0f,
                        Random.Range(-1f, 1f)
                    );

                distance = 0.0001f;
            }

            // Semakin dekat, semakin kuat dorongannya.
            float strength =
                1f - Mathf.Clamp01(distance / separationRadius);

            separation += offset.normalized * strength;
            neighbourCount++;
        }

        if (neighbourCount == 0)
        {
            return desiredVelocity;
        }

        separation /= neighbourCount;

        return desiredVelocity +
               separation * maxSpeed * separationWeight;
    }

    // ------------------------------------------------------------
    // Level 1 pendukung - Obstacle Avoidance
    // ------------------------------------------------------------

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
            currentState = SteeringState.Avoiding;

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

    // ------------------------------------------------------------
    // Gerak & rotasi
    // ------------------------------------------------------------

    private float GetCurrentSpeedLimit()
    {
        // Panic paling didahulukan, pakai panicFleeSpeed sendiri.
        if (gunReaction == GunReaction.Panicking)
        {
            return panicFleeSpeed;
        }

        // Flee dari gun didahulukan, pakai fleeSpeed sendiri.
        if (isFleeingFromGun)
        {
            return fleeSpeed;
        }

        // Pakai flag isFleeing, bukan currentState, karena state
        // bisa berubah jadi Avoiding sambil NPC tetap kabur.
        if (isFleeing)
        {
            return maxSpeed *
                   Mathf.Max(fleeSpeedMultiplier, 0.01f);
        }

        return maxSpeed;
    }

    // terapkan movement dan batasi ke ground
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

    // ------------------------------------------------------------
    // Gizmos
    // ------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        // Arrive: stop radius (merah) & slow radius (oranye)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, stopRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(position, slowRadius);

        // Flee radius (magenta) & exit radius (magenta pudar)
        if (enableFlee)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(position, fleeRadius);

            Gizmos.color = new Color(1f, 0f, 1f, 0.35f);

            Gizmos.DrawWireSphere(
                position,
                fleeRadius + Mathf.Max(fleeExitBuffer, 0f)
            );
        }

        // Separation radius (cyan)
        if (enableSeparation)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, separationRadius);
        }

        if (target == null)
        {
            return;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(position, target.position);

        // Pursue: titik prediksi (hijau)
        if (Application.isPlaying &&
            chaseMode == ChaseMode.Pursue)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawLine(
                target.position,
                predictedTargetPosition
            );

            Gizmos.DrawWireSphere(
                predictedTargetPosition,
                0.35f
            );

            Gizmos.DrawLine(
                position,
                predictedTargetPosition
            );
        }
    }
}
