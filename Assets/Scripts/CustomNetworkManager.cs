using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public new static CustomNetworkManager singleton;

    public static bool quitting = false;

    [Header("出生点")]
    public Transform spawnPointP1;
    public Transform spawnPointP2;

    [Header("基地配置")]
    public GameObject basePrefab;
    public float basePosX = 6.5f;

    public override void Awake()
    {
        base.Awake();

        if (this != null)
        {
            singleton = this;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        quitting = false;

        BrickSpawner.singleton.ServerSpawnBricks();
        SpawnBases();

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnBorder();
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        quitting = false;

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnBorder();
        }

        if (MenuManager.singleton != null)
        {
            MenuManager.singleton.HideMenu();
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (quitting) return;
        if (GameManager.singleton == null) return;
        if (GameManager.singleton.isGameOver) return;

        GameManager.singleton.ShowGameOverUI("房主已断开", TankController.ResultOpponentLeft);
    }

    void SpawnBases()
    {
        GameObject leftBase = Instantiate(basePrefab, new Vector3(-basePosX, 0, 0), Quaternion.identity);
        leftBase.GetComponent<Base>().ownerSlot = 0;
        NetworkServer.Spawn(leftBase);

        GameObject rightBase = Instantiate(basePrefab, new Vector3(basePosX, 0, 0), Quaternion.identity);
        rightBase.GetComponent<Base>().ownerSlot = 1;
        NetworkServer.Spawn(rightBase);
    }

    // 找一个没被占用的槽位（0 或 1），都占了就返回 1
    int FindFreeSlot()
    {
        for (int slot = 0; slot <= 1; slot++)
        {
            bool used = false;

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == null || conn.identity == null) continue;

                TankController tank = conn.identity.GetComponent<TankController>();
                if (tank != null && tank.playerSlot == slot)
                {
                    used = true;
                    break;
                }
            }

            if (!used) return slot;
        }

        return 1;
    }

    int CountPlayers()
    {
        int count = 0;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null) count++;
        }
        return count;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        int slot = FindFreeSlot();
        Transform spawnPoint = slot == 0 ? spawnPointP1 : spawnPointP2;

        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // 关键：槽位必须在 AddPlayerForConnection 之前写入，才会随生成消息下发
        TankController tank = player.GetComponent<TankController>();
        if (tank != null)
        {
            tank.playerSlot = slot;
        }

        NetworkServer.AddPlayerForConnection(conn, player);

        if (CountPlayers() >= 2 && GameManager.singleton != null)
        {
            GameManager.singleton.ServerMarkMatchStarted();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        if (GameManager.singleton != null)
        {
            GameManager.singleton.ServerNotifyOpponentLeft(conn);
        }
    }
}
