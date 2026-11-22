using Mirror;
using UnityEngine;

public class Base : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnOwnerChanged))]
    public int ownerConnectionId;

    private SpriteRenderer baseSprite;

    void Awake()
    {
        baseSprite = GetComponent<SpriteRenderer>();
    }

    // 服务端生成后赋值，触发颜色同步
    public override void OnStartServer()
    {
        base.OnStartServer();
        UpdateBaseColor(ownerConnectionId);
    }

    // 客户端初始化时强制刷新颜色，解决初始同步不触发hook的问题
    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateBaseColor(ownerConnectionId);
    }

    // 归属ID变化时自动更新颜色
    void OnOwnerChanged(int oldId, int newId)
    {
        UpdateBaseColor(newId);
    }

    void UpdateBaseColor(int ownerId)
    {
        if (ownerId == 0)
        {
            // 玩家1（主机方）基地：深蓝色
            baseSprite.color = new Color(0.1f, 0.2f, 0.6f);
        }
        else
        {
            // 玩家2（客户端方）基地：深红色
            baseSprite.color = new Color(0.6f, 0.1f, 0.1f);
        }
    }
}
