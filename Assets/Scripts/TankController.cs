using Mirror;
using UnityEngine;

public class TankController : NetworkBehaviour
{
    public const int ResultNone = 0;
    public const int ResultWin = 1;
    public const int ResultLose = -1;
    public const int ResultOpponentLeft = 2;

    [SyncVar]
    public int playerSlot = -1;

    // 地图种子：两端用同一个 seed 生成同一套布局
    [SyncVar(hook = nameof(OnMapSeedChanged))]
    public int mapSeed = 0;

    [SyncVar(hook = nameof(OnCountdownStageChanged))]
    public int countdownStage = 0;

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
    private float lastServerFireTime;
    private Rigidbody2D rb;
    private SpriteRenderer tankSprite;
    private Vector2 joystickDir;
    private bool useJoystickMode;
    private float moveInput;
    private float rotateInput;

    public bool IsCountingDown => countdownStage >= 1 && countdownStage < 4;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tankSprite = GetComponent<SpriteRenderer>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        tankColor = playerSlot == 0
            ? new Color(0.2f, 0.4f, 1f)
            : new Color(1f, 0.4f, 0.4f);

        if (tankSprite != null) tankSprite.color = tankColor;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        CustomNetworkManager.matchJoined = true;

        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.BindLocalTank(this);
        }

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnRandomWalls(mapSeed);
        }

        if (GameManager.singleton != null)
        {
            GameManager.singleton.SetCountdownStage(countdownStage);
        }
    }

    void OnMapSeedChanged(int oldSeed, int newSeed)
    {
        if (!isLocalPlayer) return;

        if (SteelWallSpawner.singleton != null)
        {
            SteelWallSpawner.singleton.SpawnRandomWalls(newSeed);
        }
    }

    [Server]
    public void ServerSetMatchResult(int result)
    {
        matchResult = result;
    }

    public void SetDeadVisual()
    {
        if (tankSprite != null)
        {
            tankSprite.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        }
    }

    void OnMatchResultChanged(int oldValue, int newValue)
    {
        if (newValue == ResultNone) return;
        if (!isLocalPlayer) return;

        string resultStr;
        if (newValue == ResultOpponentLeft)
        {
            resultStr = "对手已退出";
        }
        else if (newValue == ResultWin)
        {
            resultStr = "对手被击毁了";
        }
        else
        {
            resultStr = "你被击毁了";
        }

        if (GameManager.singleton != null)
        {
            GameManager.singleton.ShowGameOverUI(resultStr, newValue);
        }
    }

    void OnCountdownStageChanged(int oldStage, int newStage)
    {
        if (GameManager.singleton != null)
        {
            GameManager.singleton.SetCountdownStage(newStage);
        }
    }

    bool IsFrozen()
    {
        if (IsCountingDown) return true;
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
        if (IsFrozen()) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;
        CmdFire();
    }

    [Command]
    void CmdFire()
    {
        // 服务端兜底：对局已结束，不再生成子弹
        if (GameManager.singleton != null && GameManager.singleton.isGameOver) return;

        // 本人已阵亡，不再开火
        TankHealth health = GetComponent<TankHealth>();
        if (health != null && health.isDead) return;

        // 服务端独立冷却，防止客户端绕过本地冷却连发
        if (Time.time - lastServerFireTime < fireCooldown) return;
        lastServerFireTime = Time.time;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        NetworkServer.Spawn(bullet, connectionToClient);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = firePoint.up * 12f;
        }

        LocalPlayShot();
        RpcPlayShot();
    }

    void LocalPlayShot()
    {
        if (SfxManager.singleton != null)
        {
            SfxManager.singleton.PlayShot(transform.position);
        }
    }

    [ClientRpc]
    void RpcPlayShot()
    {
        if (isServer) return;
        LocalPlayShot();
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        if (tankSprite != null)
        {
            tankSprite.color = newColor;
        }
    }
}
