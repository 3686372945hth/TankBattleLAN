using Mirror;
using UnityEngine;

public class Base : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnOwnerChanged))]
    public int ownerSlot;   // 0 = Íæ¼Ò1£¬1 = Íæ¼Ò2

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

    void OnOwnerChanged(int oldSlot, int newSlot)
    {
        UpdateBaseColor(newSlot);
    }

    void UpdateBaseColor(int slot)
    {
        if (baseSprite == null) return;

        baseSprite.color = slot == 0
            ? new Color(0.1f, 0.2f, 0.6f)
            : new Color(0.6f, 0.1f, 0.1f);
    }
}
