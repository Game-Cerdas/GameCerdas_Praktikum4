using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float turnSpeed = 10f;

    [Header("Boundary")]
    [SerializeField]
    private GroundBoundary groundBoundary;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            new Vector3(horizontal, 0f, vertical);

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );

        ClampToGround();

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
        }
    }

    // batasi posisi setelah bergerak
    private void ClampToGround()
    {
        if (groundBoundary == null)
        {
            return;
        }

        Vector3 clampedPosition =
            groundBoundary.ClampPosition(transform.position);

        Vector3 correction =
            clampedPosition - transform.position;

        if (correction.sqrMagnitude > 0.0001f)
        {
            controller.Move(correction);
        }
    }
}