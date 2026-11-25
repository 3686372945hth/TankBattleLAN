using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public int damage = 1;

    private bool resolved;

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
            NetworkServer.Destroy(other.gameObject);
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Steel"))
        {
            resolved = true;
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
                    // 打中敌方基地：射手获胜
                    winnerConn = connectionToClient;
                }
                else
                {
                    // 打中己方基地：对手获胜
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
            NetworkServer.Destroy(gameObject);
        }
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
