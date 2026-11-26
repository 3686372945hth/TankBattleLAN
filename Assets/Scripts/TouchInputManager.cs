using UnityEngine;

public class TouchInputManager : MonoBehaviour
{
    public static TouchInputManager singleton;

    private TankController localTank;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    // 本地玩家生成时调用，绑定控制对象
    public void BindLocalTank(TankController tank)
    {
        localTank = tank;
    }

    // 射击按钮点击事件
    public void OnClickFire()
    {
        if (localTank != null)
        {
            localTank.TryFire();
        }
    }
}
