using UnityEngine;

public class GroundBoundary : MonoBehaviour
{
    [Header("Ground Reference")]
    [SerializeField]
    private Renderer groundRenderer;

    [Header("Padding")]
    [SerializeField]
    private float edgePadding = 0.5f;

    private Bounds groundBounds;

    // ambil batas ground dari renderer
    private void Awake()
    {
        if (groundRenderer != null)
        {
            groundBounds = groundRenderer.bounds;
        }
    }

    // kembalikan posisi yang sudah dibatasi
    public Vector3 ClampPosition(Vector3 position)
    {
        if (groundRenderer == null)
        {
            return position;
        }

        float minX = groundBounds.min.x + edgePadding;
        float maxX = groundBounds.max.x - edgePadding;
        float minZ = groundBounds.min.z + edgePadding;
        float maxZ = groundBounds.max.z - edgePadding;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);

        return position;
    }

    [Header("Escape Settings")]
    [SerializeField]
    private float escapeMargin = 3f;

    // hitung arah menjauh dari tepi terdekat
    public Vector3 GetEscapeDirection(Vector3 position)
    {
        if (groundRenderer == null)
            return Vector3.zero;

        float minX = groundBounds.min.x;
        float maxX = groundBounds.max.x;
        float minZ = groundBounds.min.z;
        float maxZ = groundBounds.max.z;

        Vector3 escape = Vector3.zero;

        float distToMinX = position.x - minX;
        float distToMaxX = maxX - position.x;
        float distToMinZ = position.z - minZ;
        float distToMaxZ = maxZ - position.z;

        if (distToMinX < escapeMargin)
            escape.x += (escapeMargin - distToMinX);

        if (distToMaxX < escapeMargin)
            escape.x -= (escapeMargin - distToMaxX);

        if (distToMinZ < escapeMargin)
            escape.z += (escapeMargin - distToMinZ);

        if (distToMaxZ < escapeMargin)
            escape.z -= (escapeMargin - distToMaxZ);

        return escape;
    }

    // cek posisi dekat tepi manapun
    public bool IsNearEdge(Vector3 position)
    {
        return GetEscapeDirection(position).sqrMagnitude > 0.0001f;
    }
}
