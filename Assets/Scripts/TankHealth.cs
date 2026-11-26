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
        // 已经结算过就不再处理
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

        if (FxManager.singleton != null) FxManager.singleton.SpawnHit(transform.position, HitKind.Tank);
        if (SfxManager.singleton != null) SfxManager.singleton.PlayExplosion();
    }
}
