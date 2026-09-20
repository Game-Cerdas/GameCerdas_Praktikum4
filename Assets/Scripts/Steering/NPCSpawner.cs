using System.Collections.Generic;
using UnityEngine;

// Pendukung Level 3 - menggandakan NPC supaya Separation terlihat.
public class NPCSpawner : MonoBehaviour
{
    [Header("Sumber NPC")]

    [SerializeField]
    private GameObject npcSource;

    [SerializeField]
    private bool includeSourceInCount = true;

    [Header("Jumlah & Area")]

    [SerializeField]
    private int npcCount = 6;

    [SerializeField]
    private Transform spawnCenter;

    [SerializeField]
    private float spawnRadius = 6f;

    [SerializeField]
    private float minSpacing = 1.5f;

    [SerializeField]
    private int maxPlacementAttempts = 30;

    [Header("Cek Tabrakan Saat Spawn")]

    [SerializeField]
    private LayerMask blockedMask;

    [SerializeField]
    private float clearanceRadius = 0.6f;

    [Header("Variasi")]

    [SerializeField]
    private bool randomizeRotation = true;

    private readonly List<Vector3> usedPositions =
        new List<Vector3>();

    private void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        if (npcSource == null)
        {
            Debug.LogWarning(
                "NPCSpawner: npcSource belum di-assign.",
                this
            );

            return;
        }

        usedPositions.Clear();

        int cloneCount = npcCount;

        if (includeSourceInCount)
        {
            usedPositions.Add(npcSource.transform.position);
            cloneCount = npcCount - 1;
        }

        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 position;

            if (!TryFindSpawnPosition(out position))
            {
                Debug.LogWarning(
                    "NPCSpawner: gagal cari posisi kosong untuk NPC ke-" +
                    (i + 1) + ". Perbesar spawnRadius atau kecilkan minSpacing.",
                    this
                );

                continue;
            }

            Quaternion rotation =
                randomizeRotation
                    ? Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f
                    )
                    : npcSource.transform.rotation;

            GameObject clone =
                Instantiate(
                    npcSource,
                    position,
                    rotation,
                    transform
                );

            clone.name =
                npcSource.name + "_" + (i + 1).ToString("00");

            clone.SetActive(true);

            usedPositions.Add(position);
        }
    }

    private bool TryFindSpawnPosition(out Vector3 position)
    {
        Vector3 center = GetCenter();
        float height = npcSource.transform.position.y;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector2 offset =
                Random.insideUnitCircle * spawnRadius;

            Vector3 candidate =
                new Vector3(
                    center.x + offset.x,
                    height,
                    center.z + offset.y
                );

            if (!IsFarEnough(candidate))
            {
                continue;
            }

            if (IsBlocked(candidate))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        position = center;
        return false;
    }

    private bool IsFarEnough(Vector3 candidate)
    {
        for (int i = 0; i < usedPositions.Count; i++)
        {
            Vector3 offset = candidate - usedPositions[i];
            offset.y = 0f;

            if (offset.magnitude < minSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlocked(Vector3 candidate)
    {
        if (blockedMask.value == 0)
        {
            return false;
        }

        return Physics.CheckSphere(
            candidate,
            clearanceRadius,
            blockedMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private Vector3 GetCenter()
    {
        return spawnCenter != null
            ? spawnCenter.position
            : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetCenter();

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);

        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
