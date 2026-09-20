using UnityEngine;

// Challenge 4 — Dynamic Obstacle:
// menggerakkan NavMeshObstacle bolak-balik agar repathing / carving terlihat.
public class DynamicObstacleMover : MonoBehaviour
{
    public Vector3 moveOffset = new Vector3(6f, 0f, 0f);
    public float speed = 1.5f;
    public bool moving = true;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!moving)
        {
            return;
        }

        float t = Mathf.PingPong(Time.time * speed / Mathf.Max(0.01f, moveOffset.magnitude), 1f);
        transform.position = Vector3.Lerp(startPosition, startPosition + moveOffset, t);
    }
}
