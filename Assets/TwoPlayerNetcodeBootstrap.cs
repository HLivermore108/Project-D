using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TwoPlayerNetcodeBootstrap : MonoBehaviour
{
    public enum SessionMode
    {
        SinglePlayer,
        Multiplayer
    }

    public static TwoPlayerNetcodeBootstrap Instance { get; private set; }
    public static SessionMode CurrentSessionMode { get; private set; } = SessionMode.SinglePlayer;
    public const int StageCount = 2;

    public string Status => status;
    public bool IsConnected => networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer);
    public bool IsHostSession => networkManager != null && networkManager.IsHost;
    public bool CanHostStartGame => IsHostSession && SceneManager.GetActiveScene().name == MainMenuSceneName && networkManager.ConnectedClientsIds.Count >= 2;
    public string ScoreboardText => NetworkPlayerController2D.BuildFinalScoreText();
    public string SelectedStageLabel => GetStageLabel(selectedStageIndex);

    private const string MainMenuSceneName = "MainMenu";
    private const string SampleSceneName = "SampleScene";
    private const string StageTwoSceneName = "SampleScene2";
    private const string StartGameMessage = "ProjectD.StartGame";
    private const ushort DefaultPort = 7777;
    private const string PlayerPrefabPath = "NetworkPrefabs/NetworkPlayer";
    private const string SinglePlayerPrefabPath = "SinglePlayerPrefabs/SinglePlayerPlayer";
    private const string EnemyPrefabPath = "NetworkPrefabs/NetworkEnemy";
    private const string ProjectilePrefabPath = "NetworkPrefabs/NetworkProjectile";
    private static readonly string[] StageSceneNames = { SampleSceneName, StageTwoSceneName };
    private static readonly string[] StageLabels = { "Stage 1", "Stage 2" };

    [SerializeField] private string connectAddress = "127.0.0.1";
    [SerializeField] private ushort port = DefaultPort;
    [SerializeField] private Vector3 hostSpawnPosition = new Vector3(-6f, 1f, 30.7f);
    [SerializeField] private Vector3 clientSpawnPosition = new Vector3(6f, 1f, 30.7f);

    private NetworkManager networkManager;
    private UnityTransport transport;
    private GameObject playerPrefab;
    private GameObject singlePlayerPrefab;
    private GameObject enemyPrefab;
    private GameObject projectilePrefab;
    private string status = "Offline";
    private int selectedStageIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForMenuOrSampleScene()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        EnsureBootstrap(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        EnsureBootstrap(scene);
    }

    private static void EnsureBootstrap(Scene scene)
    {
        if (!scene.isLoaded || (scene.name != MainMenuSceneName && !IsPlayableScene(scene.name)))
            return;

#if UNITY_2023_2_OR_NEWER
        if (FindFirstObjectByType<TwoPlayerNetcodeBootstrap>() != null)
#else
        if (FindObjectOfType<TwoPlayerNetcodeBootstrap>() != null)
#endif
            return;

        var bootstrapObject = new GameObject("TwoPlayerNetcodeBootstrap");
        bootstrapObject.AddComponent<TwoPlayerNetcodeBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureNetworkManager();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (networkManager == null)
            EnsureNetworkManager();

        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

        if (networkManager.CustomMessagingManager != null)
            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StartGameMessage);
    }

    public static string GetStageLabel(int stageIndex)
    {
        int clampedIndex = Mathf.Clamp(stageIndex, 0, StageLabels.Length - 1);
        return StageLabels[clampedIndex];
    }

    public static string GetStageSceneName(int stageIndex)
    {
        int clampedIndex = Mathf.Clamp(stageIndex, 0, StageSceneNames.Length - 1);
        return StageSceneNames[clampedIndex];
    }

    public void SelectStage(int stageIndex)
    {
        selectedStageIndex = Mathf.Clamp(stageIndex, 0, StageSceneNames.Length - 1);
    }

    public void StartSinglePlayerGame()
    {
        CurrentSessionMode = SessionMode.SinglePlayer;
        Shutdown();
        Time.timeScale = 1f;
        SceneManager.LoadScene(GetStageSceneName(selectedStageIndex));
    }

    public void StartHostSession(string address, ushort sessionPort)
    {
        CurrentSessionMode = SessionMode.Multiplayer;
        connectAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address;
        port = sessionPort == 0 ? DefaultPort : sessionPort;
        ConfigureTransport();
        ConfigurePrefabs();

        bool started = networkManager.StartHost();
        status = started ? $"Hosting on {port}. Waiting for client..." : "Host failed";
        RegisterMessageHandlers();
    }

    public void StartClientSession(string address, ushort sessionPort)
    {
        CurrentSessionMode = SessionMode.Multiplayer;
        connectAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address;
        port = sessionPort == 0 ? DefaultPort : sessionPort;
        ConfigureTransport();
        ConfigurePrefabs();

        bool started = networkManager.StartClient();
        status = started ? $"Connecting to {connectAddress}:{port}" : "Client failed";
        RegisterMessageHandlers();
    }

    public void HostStartGame()
    {
        if (!CanHostStartGame)
            return;

        using (var writer = new FastBufferWriter(128, Allocator.Temp))
        {
            var sceneName = new FixedString64Bytes(GetStageSceneName(selectedStageIndex));
            writer.WriteValueSafe(sceneName);
            networkManager.CustomMessagingManager.SendNamedMessageToAll(StartGameMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(GetStageSceneName(selectedStageIndex));
    }

    public void Shutdown()
    {
        if (networkManager != null && IsConnected)
            networkManager.Shutdown();

        status = "Offline";
    }

    private void EnsureNetworkManager()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            var networkObject = new GameObject("NetworkManager");
            networkManager = networkObject.AddComponent<NetworkManager>();
            transport = networkObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig = new NetworkConfig();
            DontDestroyOnLoad(networkObject);
        }
        else
        {
            transport = networkManager.GetComponent<UnityTransport>();
            if (transport == null)
                transport = networkManager.gameObject.AddComponent<UnityTransport>();

            if (networkManager.NetworkConfig == null)
                networkManager.NetworkConfig = new NetworkConfig();
        }

        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApproveOnlyTwoPlayers;

        ConfigureTransport();
        ConfigurePrefabs();
    }

    private void ConfigureTransport()
    {
        if (transport != null)
            transport.SetConnectionData(true, connectAddress, port, "0.0.0.0");
    }

    private void ConfigurePrefabs()
    {
        playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
        singlePlayerPrefab = LoadSinglePlayerPrefab();
        enemyPrefab = Resources.Load<GameObject>(EnemyPrefabPath);
        projectilePrefab = Resources.Load<GameObject>(ProjectilePrefabPath);

        if (playerPrefab != null)
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

        RegisterNetworkPrefab(enemyPrefab);
        RegisterNetworkPrefab(projectilePrefab);
        NetworkProjectile.SetPrefab(projectilePrefab);
    }

    private void RegisterNetworkPrefab(GameObject prefab)
    {
        if (networkManager == null || prefab == null)
            return;

        if (IsNetworkPrefabRegistered(prefab))
            return;

        networkManager.AddNetworkPrefab(prefab);
    }

    private bool IsNetworkPrefabRegistered(GameObject prefab)
    {
        if (networkManager.NetworkConfig?.Prefabs == null)
            return false;

        var candidate = new NetworkPrefab { Prefab = prefab };
        uint candidateHash = candidate.SourcePrefabGlobalObjectIdHash;

        foreach (NetworkPrefab registeredPrefab in networkManager.NetworkConfig.Prefabs.Prefabs)
        {
            if (registeredPrefab.Prefab == prefab || registeredPrefab.SourcePrefabGlobalObjectIdHash == candidateHash)
                return true;
        }

        return false;
    }

    private void ApproveOnlyTwoPlayers(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = networkManager.ConnectedClientsIds.Count < 2;
        response.CreatePlayerObject = false;
        response.Position = null;
        response.Rotation = null;
        response.Reason = response.Approved ? string.Empty : "Session already has two players.";
    }

    private void RegisterMessageHandlers()
    {
        if (networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StartGameMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(StartGameMessage, HandleStartGameMessage);
    }

    private void HandleClientConnected(ulong clientId)
    {
        status = networkManager.IsHost
            ? $"Host connected players: {networkManager.ConnectedClientsIds.Count}/2"
            : "Connected. Waiting for host to start.";

        RegisterMessageHandlers();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        status = IsConnected ? "Connected" : "Offline";
    }

    private void HandleStartGameMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager.IsHost)
            return;

        reader.ReadValueSafe(out FixedString64Bytes sceneName);
        string receivedSceneName = sceneName.ToString();
        selectedStageIndex = GetStageIndex(receivedSceneName);
        CurrentSessionMode = SessionMode.Multiplayer;
        Time.timeScale = 1f;
        SceneManager.LoadScene(GetStageSceneName(selectedStageIndex));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsPlayableScene(scene.name))
            return;

        Time.timeScale = 1f;
        if (CurrentSessionMode == SessionMode.SinglePlayer)
        {
            StartCoroutine(SpawnSinglePlayerAfterSceneLoad());
            return;
        }

        if (CurrentSessionMode != SessionMode.Multiplayer)
            return;

        ConfigurePrefabs();
        PrepareMultiplayerScene();
        if (networkManager != null && networkManager.IsServer)
            StartCoroutine(SpawnPlayersAfterSceneLoad());
    }

    private void PrepareMultiplayerScene()
    {
        foreach (PlayerController2D controller in FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None))
        {
            if (controller.GetComponent<NetworkObject>() == null)
                controller.gameObject.SetActive(false);
        }

        foreach (GameObject scenePlayer in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (scenePlayer.GetComponent<NetworkPlayerController2D>() == null)
                scenePlayer.SetActive(false);
        }

        foreach (SpawnerSimple spawner in FindObjectsByType<SpawnerSimple>(FindObjectsSortMode.None))
            spawner.enabled = false;

        if (FindFirstObjectByType<NetworkMultiplayerHud>() == null)
            new GameObject("NetworkMultiplayerHud").AddComponent<NetworkMultiplayerHud>();

        if (FindFirstObjectByType<NetworkPlayerUiBinder>() == null)
            new GameObject("NetworkPlayerUiBinder").AddComponent<NetworkPlayerUiBinder>();

        if (networkManager != null && networkManager.IsServer && FindFirstObjectByType<NetworkEnemySpawner>() == null)
        {
            var spawnerObject = new GameObject("NetworkEnemySpawner");
            var spawner = spawnerObject.AddComponent<NetworkEnemySpawner>();
            spawner.enemyPrefab = enemyPrefab;
        }
    }

    private IEnumerator SpawnPlayersAfterSceneLoad()
    {
        yield return null;

        if (playerPrefab == null)
        {
            Debug.LogError("NetworkPlayer prefab was not found. Use Tools/Project D/Build Network Prefabs, or let Unity reimport the editor setup script.");
            yield break;
        }

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
            SpawnPlayerIfMissing(clientId);
    }

    private IEnumerator SpawnSinglePlayerAfterSceneLoad()
    {
        yield return null;

        PrepareSinglePlayerScene(out Vector3 spawnPosition, out Quaternion spawnRotation);
        if (singlePlayerPrefab == null)
        {
            Debug.LogError("SinglePlayerPlayer prefab was not found. Use Tools/Project D/Build Network Prefabs, or let Unity reimport the editor setup script.");
            yield break;
        }

        GameObject player = Instantiate(singlePlayerPrefab, spawnPosition, spawnRotation);
        player.name = "SinglePlayerPlayer";
        player.tag = "Player";
        ConfigureSinglePlayer(player);
    }

    private void SpawnPlayerIfMissing(ulong clientId)
    {
        if (networkManager.SpawnManager.GetPlayerNetworkObject(clientId) != null)
            return;

        Vector3 spawnPosition = clientId == NetworkManager.ServerClientId ? hostSpawnPosition : clientSpawnPosition;
        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    private void PrepareSinglePlayerScene(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = hostSpawnPosition;
        spawnRotation = Quaternion.identity;

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            spawnPosition = existingPlayer.transform.position;
            spawnRotation = existingPlayer.transform.rotation;
            existingPlayer.SetActive(false);
        }

        foreach (PlayerController2D sceneController in FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None))
        {
            if (sceneController == null || !sceneController.gameObject.activeInHierarchy)
                continue;

            spawnPosition = sceneController.transform.position;
            spawnRotation = sceneController.transform.rotation;
            sceneController.gameObject.SetActive(false);
        }
    }

    private void ConfigureSinglePlayer(GameObject player)
    {
        PlayerController2D controller = player.GetComponent<PlayerController2D>();
        if (controller == null)
            return;

        controller.scoringHealth = FindFirstObjectByType<ScoringHealth>();
        controller.bulletPool = FindPoolWithComponent<Bullet3D>();
        if (controller.firePoint == null)
            controller.firePoint = EnsureFirePoint(player.transform);

        if (controller.audioSource == null)
            controller.audioSource = player.GetComponentInChildren<AudioSource>();

        CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.player = player.transform;
    }

    private ObjectPool FindPoolWithComponent<T>() where T : Component
    {
        foreach (ObjectPool pool in FindObjectsByType<ObjectPool>(FindObjectsSortMode.None))
        {
            if (pool != null && pool.prefab != null && pool.prefab.GetComponent<T>() != null)
                return pool;
        }

        return null;
    }

    private Transform EnsureFirePoint(Transform root)
    {
        Transform firePoint = root.Find("NetworkFirePoint");
        if (firePoint != null)
            return firePoint;

        var firePointObject = new GameObject("NetworkFirePoint");
        firePointObject.transform.SetParent(root, false);
        firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 1.2f);
        firePointObject.transform.localRotation = Quaternion.identity;
        return firePointObject.transform;
    }

    private GameObject LoadSinglePlayerPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(SinglePlayerPrefabPath);
#if UNITY_EDITOR
        if (prefab == null)
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PBRCharacter Variant.prefab");
#endif
        return prefab;
    }

    private static bool IsPlayableScene(string sceneName)
    {
        for (int i = 0; i < StageSceneNames.Length; i++)
        {
            if (StageSceneNames[i] == sceneName)
                return true;
        }

        return false;
    }

    private static int GetStageIndex(string sceneName)
    {
        for (int i = 0; i < StageSceneNames.Length; i++)
        {
            if (StageSceneNames[i] == sceneName)
                return i;
        }

        return 0;
    }
}
