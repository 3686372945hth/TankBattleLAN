using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public int damage = 1;

    private const float LifeTime = 2f;

    private bool resolved;
    private float lifeTimer;

    void Update()
    {
        if (!isServer) return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= LifeTime)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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
                baseObj.MarkDestroyed();

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

    void LocalHitFx(Vector3 pos, HitKind kind)
    {
        if (FxManager.singleton != null) FxManager.singleton.SpawnHit(pos, kind);
        if (SfxManager.singleton != null) SfxManager.singleton.PlayHit(kind, pos);
    }

    [ClientRpc]
    void RpcHitFx(Vector3 pos, int kind)
    {
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
