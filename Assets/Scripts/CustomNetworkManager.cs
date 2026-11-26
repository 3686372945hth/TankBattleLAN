using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public new static CustomNetworkManager singleton;

    public static bool quitting = false;

    // 本局是否已成功加入对局，用于区分“未进对局的连接失败”和“对局中断线”
    public static bool matchJoined = false;

    [Header("基地预制体")]
    public GameObject basePrefab;

    private int mapSeed = 0;

    public override void Awake()
    {
        base.Awake();
        singleton = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        quitting = false;
        matchJoined = false;

        // 每局只生成一次，双方共用同一个种子，保证同一张地图
        mapSeed = Random.Range(1000, int.MaxValue);
        if (mapSeed <= 0) mapSeed = 12345;

        // 顺序：先确定随机钢墙位置，再生成砖墙（砖墙避让钢墙）
        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnRandomWalls(mapSeed);
        }

        if (BrickSpawner.singleton != null)
        {
            BrickSpawner.singleton.ServerSpawnBricks();
        }

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
        matchJoined = false;

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

        // 未成功加入对局：连接失败 / 被满员拒绝，提示重新显示主菜单，不弹结算面板
        if (!matchJoined)
        {
            matchJoined = false;
            if (MenuManager.singleton != null)
            {
                MenuManager.singleton.ShowConnectFailed();
            }
            return;
        }

        // 对局中断线：胜负以服务端 SyncVar 结果为准，本地仅兜底
        matchJoined = false;
        if (GameManager.singleton == null || GameManager.singleton.isGameOver) return;

        GameManager.singleton.ShowGameOverUI("对手已退出", TankController.ResultOpponentLeft);
    }

    void SpawnBases()
    {
        if (basePrefab == null) return;

        GameObject leftBase = Instantiate(basePrefab, new Vector3(-MapConfig.BaseX, 0f, 0f), Quaternion.identity);
        Base left = leftBase.GetComponent<Base>();
        if (left != null) left.ownerSlot = 0;
        NetworkServer.Spawn(leftBase);

        GameObject rightBase = Instantiate(basePrefab, new Vector3(MapConfig.BaseX, 0f, 0f), Quaternion.identity);
        Base right = rightBase.GetComponent<Base>();
        if (right != null) right.ownerSlot = 1;
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
        // 满员拒绝新连接，不分配玩家，也不占用玩家槽位
        if (CountPlayers() >= 2)
        {
            conn.Disconnect();
            return;
        }

        if (playerPrefab == null) return;

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

        // 被拒绝的连接没有玩家身份，不参与对局退出结算
        if (conn == null || conn.identity == null) return;

        if (GameManager.singleton != null)
        {
            GameManager.singleton.ServerNotifyOpponentLeft(conn);
        }
    }
}
