using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // 加new关键字，显式说明隐藏父类单例，消除CS0108警告
    public new static CustomNetworkManager singleton;

    [Header("出生点")]
    public Transform spawnPointP1;
    public Transform spawnPointP2;

    [Header("基地配置")]
    public GameObject basePrefab;
    public float basePosX = 6.5f;

    private int playerCount = 0;

    // 加new关键字，显式说明隐藏父类Awake，消除CS0114警告
    private new void Awake()
    {
        singleton = this;
    }

    // 服务端启动完成：生成网络砖墙 + 双方基地 + 本地钢墙
    public override void OnStartServer()
    {
        base.OnStartServer();
        BrickSpawner.singleton.ServerSpawnBricks();
        SpawnBases();

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnBorder();
        }
    }

    // 客户端连接成功：生成本地钢墙 + 隐藏主菜单
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnBorder();
        }

        if (MenuManager.singleton != null)
        {
            MenuManager.singleton.HideMenu();
        }
    }

    // 生成左右两个基地并标记归属
    void SpawnBases()
    {
        GameObject leftBase = Instantiate(basePrefab, new Vector3(-basePosX, 0, 0), Quaternion.identity);
        NetworkServer.Spawn(leftBase);
        leftBase.GetComponent<Base>().ownerConnectionId = 0;

        GameObject rightBase = Instantiate(basePrefab, new Vector3(basePosX, 0, 0), Quaternion.identity);
        NetworkServer.Spawn(rightBase);
        rightBase.GetComponent<Base>().ownerConnectionId = 1;
    }

    // 玩家加入时在对应出生点生成坦克
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawnPoint = playerCount == 0 ? spawnPointP1 : spawnPointP2;
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkServer.AddPlayerForConnection(conn, player);
        playerCount++;
    }

    // 玩家断开时计数减一
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        playerCount--;
    }
}
