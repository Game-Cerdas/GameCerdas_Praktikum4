using UnityEngine;

// Eksperimen A* (bagian 24 modul) — dijalankan saat Play dengan tombol angka:
//   0 / R = kondisi awal
//   1     = Eksperimen 1: Goal dipindah ke (9, 0.2, 2)
//   2     = Eksperimen 2: dinding tambahan melintang
//   3     = Eksperimen 3: Goal dikelilingi obstacle -> "Path tidak ditemukan"
public class AStarExperimentSwitcher : MonoBehaviour
{
    [Header("Referensi")]
    public GridManager gridManager;
    public AStarPathfinder pathfinder;
    public Transform agent;
    public Transform startMarker;
    public Transform goalMarker;

    [Header("Grup Obstacle Eksperimen")]
    public GameObject experiment2Wall;
    public GameObject experiment3Cage;

    [Header("Posisi Goal")]
    public Vector3 defaultGoalPosition = new Vector3(9f, 0.2f, 9f);
    public Vector3 experiment1GoalPosition = new Vector3(9f, 0.2f, 2f);

    private int activeExperiment = -1;

    private void Start()
    {
        // Grid baru saja dibuat di Awake dengan kondisi awal, jadi tidak perlu rebuild.
        Apply(0, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.R))
        {
            Apply(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Apply(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Apply(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Apply(3);
        }
    }

    [ContextMenu("Reset ke kondisi awal")]
    public void ResetToDefault()
    {
        Apply(0);
    }

    public void Apply(int experiment)
    {
        Apply(experiment, true);
    }

    public void Apply(int experiment, bool rebuildGrid)
    {
        if (gridManager == null || pathfinder == null)
        {
            return;
        }

        activeExperiment = experiment;

        if (experiment2Wall != null)
        {
            experiment2Wall.SetActive(experiment == 2);
        }

        if (experiment3Cage != null)
        {
            experiment3Cage.SetActive(experiment == 3);
        }

        if (goalMarker != null)
        {
            goalMarker.position = experiment == 1
                ? experiment1GoalPosition
                : defaultGoalPosition;
        }

        if (agent != null && startMarker != null)
        {
            Vector3 startPosition = startMarker.position;
            startPosition.y = agent.position.y;
            agent.position = startPosition;
        }

        if (rebuildGrid && Application.isPlaying)
        {
            gridManager.RebuildGrid();
        }

        pathfinder.FindPath();

        Debug.Log(GetDescription(experiment));
    }

    private string GetDescription(int experiment)
    {
        switch (experiment)
        {
            case 1:
                return "[Eksperimen 1] Goal dipindah ke (9, 0.2, 2). Path berubah karena heuristic mengarah ke goal baru.";
            case 2:
                return "[Eksperimen 2] Dinding melintang aktif. A* memutari dinding lewat celah yang tersisa.";
            case 3:
                return "[Eksperimen 3] Goal dikelilingi obstacle. Semua neighbor goal tidak walkable -> path tidak ditemukan.";
            default:
                return "[Kondisi awal] Goal (9, 0.2, 9), hanya obstacle bawaan. Tekan 1 / 2 / 3 untuk eksperimen, 0 atau R untuk reset.";
        }
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(10f, 10f, 640f, 22f),
            "Eksperimen A*  |  0/R = awal   1 = pindah goal   2 = dinding   3 = goal terisolasi   " +
            "(aktif: " + (activeExperiment <= 0 ? "awal" : activeExperiment.ToString()) + ")"
        );
    }
}
