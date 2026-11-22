using UnityEngine;
using Mirror;

public class TankController : NetworkBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float keyboardRotateSpeed = 180f;   // 键盘模式：平滑旋转
    public float joystickRotateSpeed = 480f;   // 预留参数，瞬间转向模式不生效

    [Header("射击参数")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.5f;

    private float lastFireTime;
    private Rigidbody2D rb;
    private SpriteRenderer tankSprite;

    [SyncVar(hook = nameof(OnColorChanged))]
    private Color tankColor;

    private Vector2 joystickDir;
    private bool useJoystickMode;
    private float moveInput;
    private float rotateInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tankSprite = GetComponent<SpriteRenderer>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        tankColor = connectionToClient.connectionId == 0
            ? new Color(0.2f, 0.4f, 1f)    // 玩家1：蓝色
            : new Color(1f, 0.4f, 0.4f);   // 玩家2：浅红色
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.BindLocalTank(this);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        HandleInput();
        HandleShootInput();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        HandleMovement();
    }

    // 输入处理：键盘优先，无键盘用摇杆
    void HandleInput()
    {
        float kbMove = Input.GetAxis("Vertical");
        float kbRotate = Input.GetAxis("Horizontal");

        if (Mathf.Abs(kbMove) > 0.01f || Mathf.Abs(kbRotate) > 0.01f)
        {
            useJoystickMode = false;
            moveInput = kbMove;
            rotateInput = kbRotate;
        }
        else if (Joystick.singleton != null)
        {
            useJoystickMode = true;
            float joyH = Joystick.singleton.Horizontal;
            float joyV = Joystick.singleton.Vertical;
            joystickDir = new Vector2(joyH, joyV);
        }
    }

    // 刚体移动：物理引擎自动处理墙体阻挡
    void HandleMovement()
    {
        if (!useJoystickMode)
        {
            // 键盘模式：平滑旋转+前后移动
            Vector2 moveDir = transform.up * moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveDir);

            float targetAngle = rb.rotation - rotateInput * keyboardRotateSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(targetAngle);
        }
        else
        {
            // 摇杆模式：瞬间对准方向 + 朝向前方移动
            if (joystickDir.magnitude > 0.01f)
            {
                // 瞬间转向：修正90度基准偏移，摇杆方向=炮管朝向
                float targetAngle = Mathf.Atan2(joystickDir.y, joystickDir.x) * Mathf.Rad2Deg - 90f;
                rb.MoveRotation(targetAngle);

                // 朝炮管方向前进
                Vector2 moveDir = transform.up * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + moveDir);
            }
        }
    }

    void HandleShootInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryFire();
        }
    }

    public void TryFire()
    {
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;
        CmdFire();
    }

    [Command]
    void CmdFire()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        NetworkServer.Spawn(bullet, connectionToClient);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.velocity = firePoint.up * 12f;

        Destroy(bullet, 2f);
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        tankSprite.color = newColor;
    }
}
