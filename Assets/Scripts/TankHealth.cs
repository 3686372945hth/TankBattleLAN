using UnityEngine;
using Mirror;

public class TankHealth : NetworkBehaviour
{
    [SyncVar] public int maxHealth = 3;
    [SyncVar] public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(int damage, NetworkConnectionToClient attackerConn)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            // 击杀者获胜，而非被击毁者获胜
            GameManager.singleton.ServerEndGame(attackerConn);
            NetworkServer.Destroy(gameObject);
        }
    }
}
