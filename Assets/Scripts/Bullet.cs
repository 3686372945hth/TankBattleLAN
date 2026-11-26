using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public int damage = 1;

    private bool resolved;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 客机只负责隐藏自己的子弹，命中判定与特效/音效统一走服务端 + Rpc 路径
        if (!isServer)
        {
            if (IsHittable(other) && !IsOwnTankOnClient(other))
            {
                gameObject.SetActive(false);
            }
            return;
        }

        if (resolved) return;

        if (other.CompareTag("Wall"))
        {
            resolved = true;
            LocalHitFx(transform.position, HitKind.Wall);
            RpcHitFx(transform.position, (int)HitKind.Wall);
            NetworkServer.Destroy(other.gameObject);
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Steel"))
        {
            resolved = true;
            LocalHitFx(transform.position, HitKind.Steel);
            RpcHitFx(transform.position, (int)HitKind.Steel);
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Tank"))
        {
            if (IsOwnTankOnServer(other)) return;

            resolved = true;

            TankHealth health = other.GetComponent<TankHealth>();
            if (health != null && connectionToClient != null)
            {
                health.TakeDamage(damage, connectionToClient);
            }

            LocalHitFx(transform.position, HitKind.Tank);
            RpcHitFx(transform.position, (int)HitKind.Tank);
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Base"))
        {
            resolved = true;

            Base baseObj = other.GetComponent<Base>();
            if (baseObj != null && connectionToClient != null)
            {
                int shooterSlot = GetSlotOf(connectionToClient);
                NetworkConnectionToClient winnerConn = null;

                if (baseObj.ownerSlot != shooterSlot)
                {
                    winnerConn = connectionToClient;
                }
                else
                {
                    foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                    {
                        if (conn == null || conn.identity == null) continue;
                        if (conn.connectionId == connectionToClient.connectionId) continue;

                        if (GetSlotOf(conn) != shooterSlot)
                        {
                            winnerConn = conn;
                            break;
                        }
                    }
                }

                if (winnerConn != null && GameManager.singleton != null)
                {
                    GameManager.singleton.ServerEndGame(winnerConn);
                }
            }

            LocalHitFx(transform.position, HitKind.Base);
            RpcHitFx(transform.position, (int)HitKind.Base);
            NetworkServer.Destroy(gameObject);
        }
    }

    // 服务端 / 主机本地直接播放一次。
    // Host 本地收不到挂在临时子弹上的 ClientRpc，所以在命中分支本地直播。
    void LocalHitFx(Vector3 pos, HitKind kind)
    {
        if (FxManager.singleton != null) FxManager.singleton.SpawnHit(pos, kind);
        if (SfxManager.singleton != null) SfxManager.singleton.PlayHit(kind, pos);
    }

    [ClientRpc]
    void RpcHitFx(Vector3 pos, int kind)
    {
        // 主机/服务端已在本地播过，这里跳过避免重复
        if (isServer) return;

        LocalHitFx(pos, (HitKind)kind);
    }

    int GetSlotOf(NetworkConnectionToClient conn)
    {
        if (conn == null || conn.identity == null) return -1;
        TankController tank = conn.identity.GetComponent<TankController>();
        return tank != null ? tank.playerSlot : -1;
    }

    bool IsHittable(Collider2D other)
    {
        return other.CompareTag("Wall")
            || other.CompareTag("Steel")
            || other.CompareTag("Tank")
            || other.CompareTag("Base");
    }

    bool IsOwnTankOnServer(Collider2D other)
    {
        if (connectionToClient == null) return false;
        NetworkIdentity id = other.GetComponent<NetworkIdentity>();
        return id != null && id.connectionToClient == connectionToClient;
    }

    bool IsOwnTankOnClient(Collider2D other)
    {
        NetworkIdentity id = other.GetComponent<NetworkIdentity>();
        return id != null && NetworkClient.localPlayer != null && id == NetworkClient.localPlayer;
    }
}
