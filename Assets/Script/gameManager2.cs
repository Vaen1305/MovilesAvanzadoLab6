using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Collections;

public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;

    public bool Equals(LobbyPlayerData other)
    {
        return ClientId == other.ClientId && PlayerName == other.PlayerName && IsReady == other.IsReady;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }
}

public class gameManager2 : NetworkBehaviour
{
    public static gameManager2 Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    private List<Transform> lobbySpawnPoints = new List<Transform>();
    private int nextSpawnPointIndex = 0;

    [Header("Lobby & Scene Settings")]
    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "Juego";

    public Dictionary<string, PlayerData> playerStatesByAccountID = new();
    public NetworkList<LobbyPlayerData> lobbyPlayers;
    
    private string localPlayerName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            lobbyPlayers = new NetworkList<LobbyPlayerData>();
        }
    }

    public void SetLocalPlayerName(string name)
    {
        localPlayerName = name;
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleteForPlayer;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        }

        if (IsClient && !IsHost)
        {
            AnnounceSelfServerRpc(localPlayerName);
        }
    }
    
    private void HandleSceneLoadCompleteForPlayer(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsHost && sceneName == lobbySceneName && clientsCompleted.Contains(NetworkManager.Singleton.LocalClientId))
        {
            AnnounceSelfServerRpc(localPlayerName);
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleteForPlayer;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleteForPlayer;
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
            }
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                lobbyPlayers.RemoveAt(i);
                break;
            }
        }
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            client.PlayerObject.Despawn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AnnounceSelfServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId) return;
        }

        if (lobbyPlayers.Count < maxPlayers)
        {
            lobbyPlayers.Add(new LobbyPlayerData
            {
                ClientId = clientId,
                PlayerName = name,
                IsReady = false
            });

            if (lobbySpawnPoints.Count == 0)
            {
                GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
                lobbySpawnPoints = spawnPointObjects.OrderBy(go => go.name).Select(go => go.transform).ToList();
            }

            Transform spawnPoint = null;
            if (lobbySpawnPoints.Count > 0)
            {
                spawnPoint = lobbySpawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % lobbySpawnPoints.Count;
            }

            GameObject playerObject = spawnPoint != null ? 
                Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation) : 
                Instantiate(playerPrefab);
            
            playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, false);
            
            var controller = playerObject.GetComponent<SimplePlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == rpcParams.Receive.SenderClientId)
            {
                var playerData = lobbyPlayers[i];
                playerData.IsReady = !playerData.IsReady;
                lobbyPlayers[i] = playerData;
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.Singleton.LocalClientId) return;

        bool allReady = true;
        foreach (var p in lobbyPlayers)
        {
            if (!p.IsReady)
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnSceneLoadComplete(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        bool isGameScene = sceneName == gameSceneName;

        if (isGameScene)
        {
            nextSpawnPointIndex = 0;
            lobbySpawnPoints.Clear();


            GameObject[] gameSpawnObjects = GameObject.FindGameObjectsWithTag("GameSpawnPoint");
            List<Transform> gameSpawnPoints = gameSpawnObjects.OrderBy(go => go.name).Select(go => go.transform).ToList();

            if (gameSpawnPoints.Count == 0)
            {
                Debug.LogError("Spawn");
            }
            else
            {
                int spawnIndex = 0;
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null)
                    {
                        var networkTransform = client.PlayerObject.GetComponent<NetworkTransform>();
                        if (networkTransform != null)
                        {
                            Transform spawnPoint = gameSpawnPoints[spawnIndex % gameSpawnPoints.Count];
                            networkTransform.Teleport(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                            spawnIndex++;
                        }
                    }
                }
            }
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var controller = client.PlayerObject.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    controller.enabled = isGameScene;
                }
            }
        }
    }
}