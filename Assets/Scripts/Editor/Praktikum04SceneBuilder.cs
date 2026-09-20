using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Membangun otomatis dua scene Praktikum 4:
//   Assets/Scenes/P04_AStarGrid.unity  (Bagian A — A* manual)
//   Assets/Scenes/P04_NavMesh.unity    (Bagian B — Unity NavMesh, sudah di-Bake)
// Dijalankan otomatis saat project pertama kali dibuka (jika scene belum ada),
// atau manual lewat menu: Praktikum 04 > Build Semua Scene.
[InitializeOnLoad]
public static class Praktikum04SceneBuilder
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MaterialsFolder = "Assets/Materials";
    private const string AStarScenePath = ScenesFolder + "/P04_AStarGrid.unity";
    private const string NavMeshScenePath = ScenesFolder + "/P04_NavMesh.unity";
    private const string NavMeshDataPath = ScenesFolder + "/P04_NavMesh_NavMeshData.asset";
    private const string AutoKey = "P04_AutoSetupTried";

    static Praktikum04SceneBuilder()
    {
        if (File.Exists(AStarScenePath) || File.Exists(NavMeshScenePath))
        {
            return;
        }

        if (SessionState.GetBool(AutoKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoKey, true);
        EditorApplication.delayCall += TryAutoSetup;
    }

    private static void TryAutoSetup()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryAutoSetup;
            return;
        }

        BuildAllScenes();
        Debug.Log("[Praktikum04] Scene P04_AStarGrid & P04_NavMesh berhasil dibuat otomatis.");
    }

    [MenuItem("Praktikum 04/Build Semua Scene")]
    public static void BuildAllScenesMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        BuildAllScenes();
    }

    [MenuItem("Praktikum 04/Bake Ulang NavMesh (scene aktif)")]
    public static void RebakeActiveScene()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogWarning("[Praktikum04] Tidak ada NavMeshSurface di scene aktif.");
            return;
        }

        BakeSurface(surface);
        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        EditorSceneManager.SaveScene(surface.gameObject.scene);
    }

    public static void BuildAllScenes()
    {
        EnsureFolder(ScenesFolder);
        EnsureFolder(MaterialsFolder);

        int obstacleLayer = EnsureLayer("Obstacle");
        int groundLayer = EnsureLayer("Ground");

        BuildNavMeshScene(obstacleLayer, groundLayer);
        BuildAStarScene(obstacleLayer, groundLayer);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(AStarScenePath, true),
            new EditorBuildSettingsScene(NavMeshScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------
    // BAGIAN A — A* GRID
    // ------------------------------------------------------------------
    private static void BuildAStarScene(int obstacleLayer, int groundLayer)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCamera(new Vector3(4.5f, 13f, -3f), new Vector3(62f, 0f, 0f));
        CreateLight();

        // Ground (untuk ClickGoalSetter / raycast)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(4.5f, -0.06f, 4.5f);
        ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        ground.layer = groundLayer;
        SetMat(ground, "P04_Ground", new Color(0.25f, 0.32f, 0.28f));

        // Obstacle sesuai modul (bagian 9)
        GameObject obstacles = new GameObject("Obstacles");
        Vector3[] obstaclePositions =
        {
            new Vector3(3f, 0.5f, 2f),
            new Vector3(3f, 0.5f, 3f),
            new Vector3(3f, 0.5f, 4f),
            new Vector3(6f, 0.5f, 5f),
            new Vector3(6f, 0.5f, 6f),
            new Vector3(7f, 0.5f, 6f),
        };

        for (int i = 0; i < obstaclePositions.Length; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Obstacle_{i + 1:00}";
            cube.transform.SetParent(obstacles.transform);
            cube.transform.position = obstaclePositions[i];
            cube.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            cube.layer = obstacleLayer;
            SetMat(cube, "P04_Obstacle", new Color(0.15f, 0.15f, 0.18f));
        }

        // AStarSystem
        GameObject system = new GameObject("AStarSystem");
        system.transform.position = Vector3.zero;

        GridManager grid = system.AddComponent<GridManager>();
        grid.width = 10;
        grid.height = 10;
        grid.cellSize = 1f;
        grid.obstacleMask = 1 << obstacleLayer;

        AStarPathfinder pathfinder = system.AddComponent<AStarPathfinder>();

        // Start & Goal
        GameObject start = CreateMarker("StartMarker", new Vector3(0f, 0.2f, 0f), "P04_Start", Color.green);
        GameObject goal = CreateMarker("GoalMarker", new Vector3(9f, 0.2f, 9f), "P04_Goal", Color.red);

        pathfinder.gridManager = grid;
        pathfinder.startMarker = start.transform;
        pathfinder.goalMarker = goal.transform;

        // Agent
        GameObject agent = new GameObject("Agent");
        agent.transform.position = Vector3.zero;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(agent.transform);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        SetMat(body, "P04_Agent", new Color(0.2f, 0.45f, 1f));

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "FacingIndicator";
        nose.transform.SetParent(agent.transform);
        nose.transform.localPosition = new Vector3(0f, 0.7f, 0.25f);
        nose.transform.localScale = new Vector3(0.12f, 0.12f, 0.2f);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        SetMat(nose, "P04_Obstacle", new Color(0.15f, 0.15f, 0.18f));

        AgentPathFollower follower = agent.AddComponent<AgentPathFollower>();
        follower.pathfinder = pathfinder;

        // Opsional: klik ground untuk memindah goal
        ClickGoalSetter click = system.AddComponent<ClickGoalSetter>();
        click.mainCamera = cam;
        click.goalMarker = goal.transform;
        click.pathfinder = pathfinder;
        click.agent = follower;
        click.groundMask = 1 << groundLayer;

        EditorSceneManager.SaveScene(scene, AStarScenePath);
    }

    // ------------------------------------------------------------------
    // BAGIAN B — NAVMESH
    // ------------------------------------------------------------------
    private static void BuildNavMeshScene(int obstacleLayer, int groundLayer)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Vector3(0f, 30f, -24f), new Vector3(55f, 0f, 0f));
        CreateLight();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        ground.layer = groundLayer;
        SetMat(ground, "P04_Ground", new Color(0.25f, 0.32f, 0.28f));

        CreateWall("Wall_01", new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 8f), obstacleLayer);
        CreateWall("Wall_02", new Vector3(-5f, 1f, 3f), new Vector3(5f, 2f, 1f), obstacleLayer);
        CreateWall("Wall_03", new Vector3(5f, 1f, -3f), new Vector3(5f, 2f, 1f), obstacleLayer);

        // NPC
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "NPC";
        npc.transform.position = new Vector3(-8f, 1f, -8f);
        SetMat(npc, "P04_NPC", new Color(0.9f, 0.25f, 0.2f));
        IgnoreFromBake(npc);

        NavMeshAgent navAgent = npc.AddComponent<NavMeshAgent>();
        navAgent.speed = 3.5f;
        navAgent.angularSpeed = 180f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = 1.5f;
        navAgent.radius = 0.5f;
        navAgent.height = 2f;
        navAgent.autoBraking = true;

        // PlayerTarget
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = "PlayerTarget";
        target.transform.position = new Vector3(8f, 0.5f, 8f);
        SetMat(target, "P04_Target", new Color(0.2f, 0.45f, 1f));
        IgnoreFromBake(target);
        target.AddComponent<PlayerTargetMovement>();

        NavMeshChaser chaser = npc.AddComponent<NavMeshChaser>();
        chaser.target = target.transform;
        npc.AddComponent<NavMeshPathDebugger>();

        // DynamicObstacle (bagian 44–45 + Challenge 4)
        GameObject dyn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dyn.name = "DynamicObstacle";
        dyn.transform.position = new Vector3(-3f, 1f, -6f);
        dyn.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
        SetMat(dyn, "P04_Dynamic", new Color(1f, 0.6f, 0.1f));
        IgnoreFromBake(dyn);

        NavMeshObstacle navObstacle = dyn.AddComponent<NavMeshObstacle>();
        navObstacle.shape = NavMeshObstacleShape.Box;
        navObstacle.center = Vector3.zero;
        navObstacle.size = Vector3.one;
        navObstacle.carving = true;
        navObstacle.carveOnlyStationary = false;

        DynamicObstacleMover mover = dyn.AddComponent<DynamicObstacleMover>();
        mover.moveOffset = new Vector3(6f, 0f, 0f);
        mover.speed = 1.5f;

        // Navigation (NavMeshSurface)
        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 0; // Humanoid
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        EditorSceneManager.SaveScene(scene, NavMeshScenePath);

        BakeSurface(surface);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, NavMeshScenePath);
    }

    private static void BakeSurface(NavMeshSurface surface)
    {
        surface.RemoveData();
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum04] Bake NavMesh gagal.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath) != null)
        {
            AssetDatabase.DeleteAsset(NavMeshDataPath);
        }

        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();

        Debug.Log("[Praktikum04] NavMesh berhasil di-Bake: " + NavMeshDataPath);
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------
    private static Camera CreateCamera(Vector3 position, Vector3 euler)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        Camera cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        return cam;
    }

    private static void CreateLight()
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale, int layer)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = layer;
        SetMat(wall, "P04_Wall", new Color(0.55f, 0.55f, 0.6f));
    }

    private static GameObject CreateMarker(string name, Vector3 position, string matName, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.35f;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        SetMat(marker, matName, color);
        return marker;
    }

    private static void IgnoreFromBake(GameObject go)
    {
        NavMeshModifier modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
    }

    private static void SetMat(GameObject go, string matName, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = GetOrCreateMaterial(matName, color);
        }
    }

    private static Material GetOrCreateMaterial(string matName, Color color)
    {
        string path = $"{MaterialsFolder}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        Shader shader = null;

        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            shader = GraphicsSettings.defaultRenderPipeline.defaultShader;
        }

        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1)
        {
            return existing;
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning($"[Praktikum04] Tidak ada slot layer kosong untuk '{layerName}'.");
        return 0;
    }
}
