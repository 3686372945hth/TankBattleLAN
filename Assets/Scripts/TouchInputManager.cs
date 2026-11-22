using UnityEngine;

public class TouchInputManager : MonoBehaviour
{
    public static TouchInputManager singleton;

    private TankController localTank;

    void Awake()
    {
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

    // 获取摇杆移动输入（对应坦克前后移动）
    public float GetMoveInput()
    {
        if (Joystick.singleton == null) return 0;
        return Joystick.singleton.Vertical;
    }

    // 获取摇杆旋转输入（对应坦克左右旋转）
    public float GetRotateInput()
    {
        if (Joystick.singleton == null) return 0;
        return Joystick.singleton.Horizontal;
    }
}
