using UnityEngine;

// Pengembangan opsional (bagian 25): klik kiri pada ground untuk memindah Goal.
public class ClickGoalSetter : MonoBehaviour
{
    public Camera mainCamera;
    public Transform goalMarker;
    public AStarPathfinder pathfinder;
    public AgentPathFollower agent;

    public LayerMask groundMask;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f,
            groundMask))
        {
            goalMarker.position =
                hit.point + Vector3.up * 0.2f;

            // Path baru dihitung dari posisi agent saat ini.
            if (agent != null && pathfinder.startMarker != null)
            {
                Vector3 p = agent.transform.position;
                pathfinder.startMarker.position =
                    new Vector3(p.x, pathfinder.startMarker.position.y, p.z);
            }

            pathfinder.FindPath();
        }
    }
}
