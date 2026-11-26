using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public new static CustomNetworkManager singleton;

    public static bool quitting = false;

    [Header("基地配置")]
    public GameObject basePrefab;

    private int mapSeed = 0;

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

        // 每局换一个种子，两端据此算出同一张地图
        mapSeed = Random.Range(1000, int.MaxValue);
        if (mapSeed <= 0) mapSeed = 12345;

        // 顺序很重要：先放随机钢墙，砖墙才能避让
        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnRandomWalls(mapSeed);
        }

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
        GameObject leftBase = Instantiate(basePrefab, new Vector3(-MapConfig.BaseX, 0f, 0f), Quaternion.identity);
        leftBase.GetComponent<Base>().ownerSlot = 0;
        NetworkServer.Spawn(leftBase);

        GameObject rightBase = Instantiate(basePrefab, new Vector3(MapConfig.BaseX, 0f, 0f), Quaternion.identity);
        rightBase.GetComponent<Base>().ownerSlot = 1;
        NetworkServer.Spawn(rightBase);
    }

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

        Vector3 spawnPos = slot == 0
            ? new Vector3(-MapConfig.SpawnX, 0f, 0f)
            : new Vector3(MapConfig.SpawnX, 0f, 0f);

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        TankController tank = player.GetComponent<TankController>();
        if (tank != null)
        {
            tank.playerSlot = slot;
            tank.mapSeed = mapSeed;
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
