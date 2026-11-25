using Mirror;
using UnityEngine;

public class TankController : NetworkBehaviour
{
    public const int ResultNone = 0;
    public const int ResultWin = 1;
    public const int ResultLose = -1;
    public const int ResultOpponentLeft = 2;

    // 0 = 玩家1（左），1 = 玩家2（右）。由服务端在生成前写入，随生成消息下发
    [SyncVar]
    public int playerSlot = -1;

    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float keyboardRotateSpeed = 180f;
    public float joystickRotateSpeed = 480f;

    [Header("射击参数")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.5f;

    [SyncVar(hook = nameof(OnMatchResultChanged))]
    private int matchResult = ResultNone;

    [SyncVar(hook = nameof(OnColorChanged))]
    private Color tankColor;

    private float lastFireTime;
    private Rigidbody2D rb;
    private SpriteRenderer tankSprite;
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

        tankColor = playerSlot == 0
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

    [Server]
    public void ServerSetMatchResult(int result)
    {
        matchResult = result;
    }

    void OnMatchResultChanged(int oldValue, int newValue)
    {
        if (newValue == ResultNone) return;

        // 主机同时拥有两台坦克，只有自己那台才该刷本机面板
        if (!isLocalPlayer) return;

        if (GameManager.singleton != null)
        {
            string resultStr = newValue == ResultOpponentLeft ? "对手已断开" : "对局结束";
            GameManager.singleton.ShowGameOverUI(resultStr, newValue);
        }
    }

    bool IsFrozen()
    {
        return GameManager.singleton != null && GameManager.singleton.isGameOver;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (IsFrozen()) return;

        HandleInput();
        HandleShootInput();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (IsFrozen())
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        HandleMovement();
    }

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

    void HandleMovement()
    {
        if (!useJoystickMode)
        {
            Vector2 moveDir = transform.up * moveInput * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveDir);

            float targetAngle = rb.rotation - rotateInput * keyboardRotateSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(targetAngle);
        }
        else
        {
            if (joystickDir.magnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(joystickDir.y, joystickDir.x) * Mathf.Rad2Deg - 90f;
                rb.MoveRotation(targetAngle);

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
        if (bulletRb != null)
        {
            bulletRb.velocity = firePoint.up * 12f;
        }

        Destroy(bullet, 2f);
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        tankSprite.color = newColor;
    }
}
