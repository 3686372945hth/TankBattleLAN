using Mirror;

// 地图种子网络消息：服务端发给客户端，同步随机种子
public struct MapSeedMessage : NetworkMessage
{
    public int seed;
}
