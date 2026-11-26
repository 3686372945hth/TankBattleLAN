using Mirror;
using UnityEngine;

public class TankHealth : NetworkBehaviour
{
    [SyncVar] public int maxHealth = 3;
    [SyncVar] public int currentHealth = 3;

    [SyncVar(hook = nameof(OnDeadChanged))]
    public bool isDead;

    // 只初始化一次，避免对象重发（所有权变化）时把血回满
    private bool healthInitialized;

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
        // 已阵亡不再受击，血量不扣成负数
        if (isDead) return;
        if (GameManager.singleton != null && GameManager.singleton.isGameOver) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;

            if (GameManager.singleton != null)
            {
                GameManager.singleton.ServerEndGame(attackerConn);
            }
        }
    }

    void OnDeadChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        TankController tc = GetComponent<TankController>();
        if (tc != null) tc.SetDeadVisual();

        // 尸体不再参与碰撞，避免子弹继续打尸体
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            col.enabled = false;
        }

        if (FxManager.singleton != null) FxManager.singleton.SpawnHit(transform.position, HitKind.Tank);
        if (SfxManager.singleton != null) SfxManager.singleton.PlayExplosion();
    }
}
