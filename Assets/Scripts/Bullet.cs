using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public int damage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 客户端本地预判销毁子弹，消除延迟
        if (!isServer)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Steel") || other.CompareTag("Tank") || other.CompareTag("Base"))
            {
                Destroy(gameObject);
            }
            return;
        }

        // 服务端权威判定
        if (other.CompareTag("Wall"))
        {
            NetworkServer.Destroy(other.gameObject);
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Steel"))
        {
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Tank"))
        {
            TankHealth health = other.GetComponent<TankHealth>();
            if (health != null)
            {
                // 传入击杀者的连接，修正获胜方判定
                health.TakeDamage(damage, connectionToClient);
            }
            NetworkServer.Destroy(gameObject);
        }
        else if (other.CompareTag("Base"))
        {
            Base baseObj = other.GetComponent<Base>();
            if (baseObj != null)
            {
                int shooterId = connectionToClient.connectionId;
                int baseOwnerId = baseObj.ownerConnectionId;

                NetworkConnectionToClient winnerConn;
                if (shooterId != baseOwnerId)
                {
                    // 击中敌方基地：发射方获胜
                    winnerConn = connectionToClient;
                }
                else
                {
                    // 击中己方基地：对方获胜
                    winnerConn = null;
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        if (conn.connectionId != shooterId)
                        {
                            winnerConn = conn;
                            break;
                        }
                    }
                }

                if (winnerConn != null)
                {
                    GameManager.singleton.ServerEndGame(winnerConn);
                }
            }
            NetworkServer.Destroy(gameObject);
        }
    }
}
