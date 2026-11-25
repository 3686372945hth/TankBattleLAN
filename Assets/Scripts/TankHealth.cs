using Mirror;
using UnityEngine;

public class TankHealth : NetworkBehaviour
{
    [SyncVar] public int maxHealth = 3;
    [SyncVar] public int currentHealth = 3;

    // 只初始化一次，避免对象重发（所有权变化）时把血回满
    private bool healthInitialized;

    // 生成时在服务端初始化：OnStartServer 一定早于生成消息发出
    public override void OnStartServer()
    {
        base.OnStartServer();

        if (healthInitialized) return;
        healthInitialized = true;

        if (maxHealth < 1) maxHealth = 3;
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(int damage, NetworkConnectionToClient attackerConn)
    {
        // 已经结算过就不再处理
        if (GameManager.singleton != null && GameManager.singleton.isGameOver) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;

            if (GameManager.singleton != null)
            {
                GameManager.singleton.ServerEndGame(attackerConn);
            }

            // 不销毁坦克：对象留着，结算状态才发得出去
        }
    }
}
