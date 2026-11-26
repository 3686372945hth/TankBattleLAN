using Mirror;
using UnityEngine;

public class Base : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnOwnerChanged))]
    public int ownerSlot;   // 0 = 玩家1，1 = 玩家2

    [SyncVar(hook = nameof(OnDestroyedChanged))]
    public bool destroyed;

    private SpriteRenderer baseSprite;

    void Awake()
    {
        baseSprite = GetComponent<SpriteRenderer>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        UpdateBaseColor(ownerSlot);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateBaseColor(ownerSlot);
    }

    [Server]
    public void MarkDestroyed()
    {
        destroyed = true;
    }

    void OnOwnerChanged(int oldSlot, int newSlot)
    {
        UpdateBaseColor(newSlot);
    }

    void OnDestroyedChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        UpdateDestroyedColor();
        DisableColliders();
    }

    void UpdateBaseColor(int slot)
    {
        if (baseSprite == null || destroyed) return;

        baseSprite.color = slot == 0
            ? new Color(0.1f, 0.2f, 0.6f)
            : new Color(0.6f, 0.1f, 0.1f);
    }

    void UpdateDestroyedColor()
    {
        if (baseSprite != null)
        {
            baseSprite.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    // 基地被击毁后移除碰撞阻挡，避免尸体挡路、重复触发子弹命中
    void DisableColliders()
    {
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            col.enabled = false;
        }
    }
}
