using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Membangun tiga scene Praktikum 4:
//   Assets/Scenes/P04_AStarGrid.unity  — Bagian A, A* manual pada grid
//   Assets/Scenes/P04_NavMesh.unity    — Bagian B, Unity NavMesh (sudah di-Bake)
//   Assets/Scenes/P04_Integrasi.unity  — gabungan Praktikum 1-4 (bagian 51 modul)
//
// Aset visual (Player, NPC, NPCAnimal) diambil dari Praktikum 3.
// Jalankan lewat menu: Praktikum 04 > Build Semua Scene.
[InitializeOnLoad]
public static class Praktikum04SceneBuilder
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MaterialsFolder = "Assets/Materials";

    private const string AStarScenePath = ScenesFolder + "/P04_AStarGrid.unity";
    private const string NavMeshScenePath = ScenesFolder + "/P04_NavMesh.unity";
    private const string IntegrationScenePath = ScenesFolder + "/P04_Integrasi.unity";

    private const string NavMeshDataPath = ScenesFolder + "/P04_NavMesh_NavMeshData.asset";
    private const string IntegrationNavMeshDataPath = ScenesFolder + "/P04_Integrasi_NavMeshData.asset";

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string NpcPrefabPath = "Assets/Prefabs/NPC.prefab";
    private const string NpcAnimalPrefabPath = "Assets/Prefabs/NPCAnimal.prefab";

    private const string AutoKey = "P04_AutoSetupTried";

    private static int obstacleLayer;
    private static int groundLayer;

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

        Scene scene = surface.gameObject.scene;
        string dataPath = scene.path == IntegrationScenePath
            ? IntegrationNavMeshDataPath
            : NavMeshDataPath;

        BakeSurface(surface, dataPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void BuildAllScenes()
    {
        EnsureFolder(ScenesFolder);
        EnsureFolder(MaterialsFolder);

        obstacleLayer = EnsureLayer("Obstacle");
        groundLayer = EnsureLayer("Ground");

        BuildNavMeshScene();
        BuildIntegrationScene();
        BuildAStarScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(AStarScenePath, true),
            new EditorBuildSettingsScene(NavMeshScenePath, true),
            new EditorBuildSettingsScene(IntegrationScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Praktikum04] Tiga scene selesai dibuat: P04_AStarGrid, P04_NavMesh, P04_Integrasi.");
    }

    // ==================================================================
    // BAGIAN A — A* GRID
    // ==================================================================
    private static void BuildAStarScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCamera(new Vector3(4.5f, 13f, -3f), new Vector3(62f, 0f, 0f), true);
        CreateLight();

        GameObject ground = CreatePlane("Ground", new Vector3(4.5f, -0.06f, 4.5f), new Vector3(1.2f, 1f, 1.2f));

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

        GameObject system = new GameObject("AStarSystem");
        system.transform.position = Vector3.zero;

        GridManager grid = system.AddComponent<GridManager>();
        grid.width = 10;
        grid.height = 10;
        grid.cellSize = 1f;
        grid.obstacleMask = 1 << obstacleLayer;

        AStarPathfinder pathfinder = system.AddComponent<AStarPathfinder>();

        GameObject start = CreateMarker("StartMarker", new Vector3(0f, 0.2f, 0f), "P04_Start", Color.green);
        GameObject goal = CreateMarker("GoalMarker", new Vector3(9f, 0.2f, 9f), "P04_Goal", Color.red);

        pathfinder.gridManager = grid;
        pathfinder.startMarker = start.transform;
        pathfinder.goalMarker = goal.transform;

        // Agent memakai visual NPCAnimal dari Praktikum 3.
        GameObject agent = InstantiatePrefab(NpcAnimalPrefabPath, "Agent", new Vector3(0f, 0.5f, 0f));

        if (agent != null)
        {
            // Steering dimatikan: di scene ini gerakan murni mengikuti path A*.
            DisableComponent<AnimalSteeringAgent>(agent);
            DisableComponent<SteeringSensor>(agent);

            AgentPathFollower follower = agent.AddComponent<AgentPathFollower>();
            follower.pathfinder = pathfinder;

            ClickGoalSetter click = system.AddComponent<ClickGoalSetter>();
            click.mainCamera = cam;
            click.goalMarker = goal.transform;
            click.pathfinder = pathfinder;
            click.agent = follower;
            click.groundMask = 1 << groundLayer;
        }

        ground.layer = groundLayer;

        EditorSceneManager.SaveScene(scene, AStarScenePath);
    }

    // ==================================================================
    // BAGIAN B — NAVMESH
    // ==================================================================
    private static void BuildNavMeshScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Vector3(0f, 30f, -24f), new Vector3(55f, 0f, 0f), true);
        CreateLight();

        GameObject ground = CreatePlane("Ground", Vector3.zero, new Vector3(5f, 1f, 5f));
        ground.layer = groundLayer;
        GroundBoundary boundary = ground.AddComponent<GroundBoundary>();
        SetRef(boundary, "groundRenderer", ground.GetComponent<Renderer>());

        CreateWall("Wall_01", new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 8f));
        CreateWall("Wall_02", new Vector3(-5f, 1f, 3f), new Vector3(5f, 2f, 1f));
        CreateWall("Wall_03", new Vector3(5f, 1f, -3f), new Vector3(5f, 2f, 1f));

        // PlayerTarget memakai prefab Player dari Praktikum 3 (WASD + CharacterController).
        // PlayerTarget memakai visual prefab Player, tetapi digerakkan
        // PlayerTargetMovement sesuai modul (bagian 40), bukan SimplePlayerController.
        GameObject player = InstantiatePrefab(PlayerPrefabPath, "PlayerTarget", new Vector3(8f, 1f, 8f));
        if (player != null)
        {
            DisableComponent<SimplePlayerController>(player);
            PlayerTargetMovement targetMovement = player.AddComponent<PlayerTargetMovement>();
            targetMovement.moveSpeed = 5f;
        }

        // NPC memakai prefab NPC dari Praktikum 3, tetapi digerakkan NavMeshAgent.
        GameObject npc = InstantiatePrefab(NpcPrefabPath, "NPC", new Vector3(-8f, 1f, -8f));

        if (npc != null)
        {
            DisableComponent<SteeringAgent>(npc);
            DisableComponent<SteeringSensor>(npc);
            DisableComponent<SteeringDebug>(npc);
            DisableComponent<SteeringAgentVisual>(npc);
            DisableComponent<PlayerVisionSensor>(npc);

            NavMeshAgent navAgent = npc.AddComponent<NavMeshAgent>();
            navAgent.speed = 3.5f;
            navAgent.angularSpeed = 180f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 1.5f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;
            navAgent.autoBraking = true;

            NavMeshChaser chaser = npc.AddComponent<NavMeshChaser>();
            chaser.target = player != null ? player.transform : null;

            npc.AddComponent<NavMeshPathDebugger>();
            IgnoreFromBake(npc);
        }

        if (player != null)
        {
            IgnoreFromBake(player);
        }

        CreateDynamicObstacle(new Vector3(-3f, 1f, -6f));

        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = CreateSurface(navigation);

        EditorSceneManager.SaveScene(scene, NavMeshScenePath);
        BakeSurface(surface, NavMeshDataPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, NavMeshScenePath);
    }

    // ==================================================================
    // SCENE INTEGRASI — Praktikum 1 + 2 + 3 + 4
    // ==================================================================
    private static void BuildIntegrationScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Vector3(0f, 34f, -26f), new Vector3(55f, 0f, 0f), true);
        CreateLight();

        GameObject ground = CreatePlane("Ground", Vector3.zero, new Vector3(4f, 1f, 4f));
        ground.layer = groundLayer;
        GroundBoundary boundary = ground.AddComponent<GroundBoundary>();
        SetRef(boundary, "groundRenderer", ground.GetComponent<Renderer>());

        CreateWall("Wall_01", new Vector3(0f, 1f, 2f), new Vector3(2f, 2f, 10f));
        CreateWall("Wall_02", new Vector3(-8f, 1f, -4f), new Vector3(8f, 2f, 1f));
        CreateWall("Wall_03", new Vector3(8f, 1f, -6f), new Vector3(8f, 2f, 1f));
        CreateWall("Wall_04", new Vector3(6f, 1f, 8f), new Vector3(10f, 2f, 1f));

        GameObject player = InstantiatePrefab(PlayerPrefabPath, "Player", new Vector3(12f, 1f, -14f));
        SimplePlayerController playerController = player != null ? player.GetComponent<SimplePlayerController>() : null;
        GunController gunController = player != null ? player.GetComponent<GunController>() : null;

        if (playerController != null)
        {
            SetRef(playerController, "groundBoundary", boundary);
        }

        if (player != null)
        {
            IgnoreFromBake(player);
        }

        GameObject npc = InstantiatePrefab(NpcPrefabPath, "NPC", new Vector3(-12f, 1f, 12f));

        if (npc != null)
        {
            PlayerVisionSensor vision = npc.GetComponent<PlayerVisionSensor>();
            SteeringAgent steering = npc.GetComponent<SteeringAgent>();
            SteeringSensor sensor = npc.GetComponent<SteeringSensor>();

            if (vision != null && player != null)
            {
                SetRef(vision, "player", player.transform);
                SetRef(vision, "gunController", gunController);
                SetMask(vision, "obstacleMask", 1 << obstacleLayer);
            }

            if (steering != null)
            {
                SetRef(steering, "groundBoundary", boundary);
                SetRef(steering, "sensor", sensor);
                SetRef(steering, "visionSensor", vision);
            }

            if (sensor != null)
            {
                SetMask(sensor, "obstacleMask", 1 << obstacleLayer);
            }

            NavMeshAgent navAgent = npc.AddComponent<NavMeshAgent>();
            navAgent.speed = 4.5f;
            navAgent.angularSpeed = 240f;
            navAgent.acceleration = 10f;
            navAgent.stoppingDistance = 1.5f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;
            navAgent.autoBraking = true;

            HybridNavAgent hybrid = npc.AddComponent<HybridNavAgent>();
            hybrid.player = player != null ? player.transform : null;
            hybrid.visionSensor = vision;
            hybrid.steeringAgent = steering;

            npc.AddComponent<NavMeshPathDebugger>();
            IgnoreFromBake(npc);

            // Spawner: menggandakan NPC supaya terlihat banyak agent mengejar (Challenge 5).
            GameObject spawnerObject = new GameObject("NPCSpawner");
            GameObject spawnCenter = new GameObject("SpawnCenter");
            spawnCenter.transform.position = new Vector3(-10f, 0f, 8f);

            NPCSpawner spawner = spawnerObject.AddComponent<NPCSpawner>();
            SetRef(spawner, "npcSource", npc);
            SetRef(spawner, "spawnCenter", spawnCenter.transform);
            SetInt(spawner, "npcCount", 5);
            SetFloat(spawner, "spawnRadius", 7f);
            SetFloat(spawner, "minSpacing", 2f);
            SetMask(spawner, "blockedMask", 1 << obstacleLayer);
        }

        CreateDynamicObstacle(new Vector3(-4f, 1f, -10f));

        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = CreateSurface(navigation);

        EditorSceneManager.SaveScene(scene, IntegrationScenePath);
        BakeSurface(surface, IntegrationNavMeshDataPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, IntegrationScenePath);
    }

    // ==================================================================
    // Helper
    // ==================================================================
    private static NavMeshSurface CreateSurface(GameObject host)
    {
        NavMeshSurface surface = host.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 0; // Humanoid
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        return surface;
    }

    private static void BakeSurface(NavMeshSurface surface, string dataPath)
    {
        surface.RemoveData();
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum04] Bake NavMesh gagal untuk " + dataPath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath) != null)
        {
            AssetDatabase.DeleteAsset(dataPath);
        }

        AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();

        Debug.Log("[Praktikum04] NavMesh di-Bake: " + dataPath);
    }

    private static GameObject CreateDynamicObstacle(Vector3 position)
    {
        GameObject dyn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dyn.name = "DynamicObstacle";
        dyn.transform.position = position;
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

        return dyn;
    }

    private static GameObject InstantiatePrefab(string path, string name, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[Praktikum04] Prefab tidak ditemukan: {path}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.identity;
        return instance;
    }

    private static void DisableComponent<T>(GameObject go) where T : Behaviour
    {
        T component = go.GetComponent<T>();

        if (component != null)
        {
            component.enabled = false;
        }
    }

    private static GameObject CreatePlane(string name, Vector3 position, Vector3 scale)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.position = position;
        plane.transform.localScale = scale;
        SetMat(plane, "P04_Ground", new Color(0.25f, 0.32f, 0.28f));
        return plane;
    }

    private static Camera CreateCamera(Vector3 position, Vector3 euler, bool withListener)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        Camera cam = go.AddComponent<Camera>();

        if (withListener)
        {
            go.AddComponent<AudioListener>();
        }

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

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = obstacleLayer;
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
        if (go.GetComponent<NavMeshModifier>() != null)
        {
            return;
        }

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

    // Script Praktikum 3 memakai [SerializeField] private, jadi referensinya
    // diisi lewat SerializedObject.
    private static void SetRef(Object component, string field, Object value)
    {
        SerializedProperty property = FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMask(Object component, string field, int mask)
    {
        SetInt(component, field, mask);
    }

    private static void SetInt(Object component, string field, int value)
    {
        SerializedProperty property = FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object component, string field, float value)
    {
        SerializedProperty property = FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty FindProperty(Object component, string field, out SerializedObject so)
    {
        so = null;

        if (component == null)
        {
            return null;
        }

        so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(field);

        if (property == null)
        {
            Debug.LogWarning($"[Praktikum04] Field '{field}' tidak ditemukan pada {component.GetType().Name}.");
        }

        return property;
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
