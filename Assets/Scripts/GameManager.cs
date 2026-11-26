using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;

    [Header("结算 UI")]
    public GameObject gameOverPanel;
    public TMP_Text resultText;

    [Header("调试")]
    [Tooltip("在屏幕左上角显示每台坦克的血量（仅联调时打开）")]
    public bool showDebugHp = false;

    [Tooltip("联网事件、返回菜单等逐帧打印调试日志（仅联调时打开）")]
    public bool verboseLog = false;

    [System.NonSerialized] public bool isGameOver;

    private bool serverEnded;
    private bool returning;
    private bool matchStarted;
    private float nextHeartbeat;

    // 开局倒计时文字与协程
    private TextMeshProUGUI countdownText;
    private Coroutine countdownRoutine;
    private Coroutine hideCountdownRoutine;

    void Awake()
    {
        // 定稿强制关闭调试面板与刷屏日志，覆盖场景里残留的勾选值
        showDebugHp = false;
        verboseLog = false;

        singleton = this;

        if (verboseLog)
        {
            Debug.Log("[GM] Awake id=" + GetInstanceID()
                      + " activeInHierarchy=" + gameObject.activeInHierarchy
                      + " componentEnabled=" + enabled);
        }
    }

    void Start()
    {
        isGameOver = false;
        serverEnded = false;
        returning = false;
        matchStarted = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);

        SfxManager.EnsureExists();
        FxManager.EnsureExists();
        CreateCountdownText();

        if (verboseLog)
        {
            Debug.Log("[GM] Start panel=" + (gameOverPanel != null)
                      + " text=" + (resultText != null)
                      + " EventSystem=" + (EventSystem.current != null)
                      + " clientActive=" + NetworkClient.active
                      + " serverActive=" + NetworkServer.active);
        }
    }

    void CreateCountdownText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("CountdownText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        countdownText = go.AddComponent<TextMeshProUGUI>();
        RectTransform rt = countdownText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 220f);

        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 130f;
        countdownText.fontWeight = FontWeight.Bold;
        countdownText.color = Color.white;

        if (resultText != null && resultText.font != null)
        {
            countdownText.font = resultText.font;
        }

        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!isGameOver && Input.GetKeyDown(KeyCode.F1))
        {
            ShowGameOverUI("测试胜利", TankController.ResultWin);
        }
#endif

        if (!isGameOver) return;

        if (verboseLog && Time.unscaledTime >= nextHeartbeat)
        {
            nextHeartbeat = Time.unscaledTime + 0.5f;
            Vector3 mp = Input.mousePosition;
            Debug.Log("[GM] 结算 鼠标=(" + mp.x + "," + mp.y + ")"
                      + " 按压=" + Input.GetMouseButton(0)
                      + " 触控=" + Input.touchCount
                      + " EventSystem=" + (EventSystem.current != null));
        }

        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!pressed && Input.touchCount > 0)
        {
            pressed = Input.GetTouch(0).phase == TouchPhase.Began;
        }

        if (pressed)
        {
            ReturnToMenu();
        }
    }

    // 调试用：屏幕左上角显示每台坦克的血量
    void OnGUI()
    {
        if (!showDebugHp) return;

        TankController[] tanks = FindObjectsOfType<TankController>();
        GUILayout.BeginArea(new Rect(10, 10, 320, 40 + tanks.Length * 26));
        GUILayout.Label("HP 调试");

        foreach (TankController t in tanks)
        {
            TankHealth h = t.GetComponent<TankHealth>();
            if (h != null)
            {
                GUILayout.Label("netId " + t.netId + " : " + h.currentHealth + "/" + h.maxHealth
                                + (t.isLocalPlayer ? "  (本地)" : ""));
            }
        }

        GUILayout.EndArea();
    }

    public void ServerMarkMatchStarted()
    {
        if (!NetworkServer.active) return;
        matchStarted = true;

        if (verboseLog) Debug.Log("[GM] 对局开始，启动开局倒计时");

        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(ServerCountdownRoutine());
    }

    IEnumerator ServerCountdownRoutine()
    {
        SetAllTanksStage(1);
        yield return new WaitForSeconds(1f);

        SetAllTanksStage(2);
        yield return new WaitForSeconds(1f);

        SetAllTanksStage(3);
        yield return new WaitForSeconds(1f);

        SetAllTanksStage(4);
    }

    void SetAllTanksStage(int stage)
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;

            TankController tank = conn.identity.GetComponent<TankController>();
            if (tank != null) tank.countdownStage = stage;
        }
    }

    // 由坦克 SyncVar hook 回调，在两端本地刷新倒计时显示
    public void SetCountdownStage(int stage)
    {
        if (countdownText == null) return;

        if (stage == 1)
        {
            ShowCountdown("3");
            if (SfxManager.singleton != null) SfxManager.singleton.PlayBeep();
        }
        else if (stage == 2)
        {
            ShowCountdown("2");
            if (SfxManager.singleton != null) SfxManager.singleton.PlayBeep();
        }
        else if (stage == 3)
        {
            ShowCountdown("1");
            if (SfxManager.singleton != null) SfxManager.singleton.PlayBeep();
        }
        else if (stage == 4)
        {
            ShowCountdown("开战");
            if (SfxManager.singleton != null) SfxManager.singleton.PlayGo();

            if (hideCountdownRoutine != null) StopCoroutine(hideCountdownRoutine);
            hideCountdownRoutine = StartCoroutine(HideCountdownRoutine(0.8f));
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void ShowCountdown(string text)
    {
        countdownText.text = text;
        countdownText.gameObject.SetActive(true);
    }

    IEnumerator HideCountdownRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void ServerEndGame(NetworkConnectionToClient winnerConn)
    {
        if (!NetworkServer.active) return;
        if (serverEnded) return;
        serverEnded = true;

        if (verboseLog)
        {
            Debug.Log("[GM] ServerEndGame 胜者连接="
                      + (winnerConn != null ? winnerConn.connectionId.ToString() : "null"));
        }

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;

            TankController tank = conn.identity.GetComponent<TankController>();
            if (tank == null) continue;

            tank.ServerSetMatchResult(conn == winnerConn
                ? TankController.ResultWin
                : TankController.ResultLose);
        }
    }

    public void ServerNotifyOpponentLeft(NetworkConnectionToClient quitter)
    {
        if (!NetworkServer.active) return;
        if (serverEnded) return;
        if (!matchStarted) return;
        if (CustomNetworkManager.quitting) return;

        int others = 0;
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn == quitter) continue;
            others++;
        }
        if (others == 0) return;

        serverEnded = true;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn == quitter || conn.identity == null) continue;

            TankController tank = conn.identity.GetComponent<TankController>();
            if (tank != null)
            {
                tank.ServerSetMatchResult(TankController.ResultOpponentLeft);
            }
        }
    }

    public void ShowGameOverUI(string resultStr, int state)
    {
        if (verboseLog)
        {
            Debug.Log("[GM] ShowGameOverUI 面板 id=" + GetInstanceID()
                      + " state=" + state
                      + " active=" + gameObject.activeInHierarchy
                      + " componentEnabled=" + enabled
                      + " panel=" + (gameOverPanel != null)
                      + " text=" + (resultText != null));
        }

        isGameOver = true;
        nextHeartbeat = 0f;

        // 收起触屏输入，避免摇杆和返回菜单按钮同时抢点击
        if (TouchInputManager.singleton != null && TouchInputManager.singleton.gameObject.activeSelf)
        {
            TouchInputManager.singleton.gameObject.SetActive(false);
            if (verboseLog) Debug.Log("[GM] 已收起 TouchInputManager");
        }

        // 面板置顶并自己接管点击，确保任意点击都能返回菜单
        if (gameOverPanel != null)
        {
            gameOverPanel.transform.SetAsLastSibling();
            gameOverPanel.SetActive(true);

            Graphic panelGraphic = gameOverPanel.GetComponent<Graphic>();
            if (panelGraphic != null) panelGraphic.raycastTarget = true;

            Button panelButton = gameOverPanel.GetComponent<Button>();
            if (panelButton == null) panelButton = gameOverPanel.AddComponent<Button>();

            panelButton.transition = Selectable.Transition.None;
            panelButton.targetGraphic = panelGraphic;
            panelButton.onClick.RemoveAllListeners();
            panelButton.onClick.AddListener(ReturnToMenu);

            if (verboseLog) Debug.Log("[GM] 已挂载返回按钮");
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            string head = "";
            if (state == TankController.ResultWin) head = "胜利";
            else if (state == TankController.ResultLose) head = "失败";

            resultText.text = head.Length > 0 ? head + "\n" + resultStr : resultText.text;
        }
    }

    // 点按钮 / 点屏幕返回菜单：先停网络，等状态归零再重载场景
    public void ReturnToMenu()
    {
        if (returning) return;
        returning = true;

        CustomNetworkManager.quitting = true;

        // 关键：若组件已被禁用（断线时 Mirror 会清理物体），协程无法启动，直接走同步路径
        if (!isActiveAndEnabled)
        {
            if (verboseLog) Debug.Log("[GM] 组件未激活，走同步返回");

            if (resultText != null) resultText.text = "正在返回菜单";

            StopNetworkSafe();
            TryLoadSceneSync();
            return;
        }

        StartCoroutine(ReturnRoutine());
    }

    void StopNetworkSafe()
    {
        try
        {
            NetworkManager nm = NetworkManager.singleton;
            if (verboseLog)
            {
                Debug.Log("[GM] 停止网络 前 clientActive=" + NetworkClient.active
                          + " serverActive=" + NetworkServer.active);
            }

            if (nm != null)
            {
                if (NetworkClient.active && NetworkServer.active) nm.StopHost();
                else if (NetworkClient.active) nm.StopClient();
                else if (NetworkServer.active) nm.StopServer();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 停止网络异常: " + e);
        }
    }

    System.Collections.IEnumerator ReturnRoutine()
    {
        if (resultText != null) resultText.text = "正在返回菜单";

        StopNetworkSafe();

        float deadline = Time.realtimeSinceStartup + 2f;
        while ((NetworkClient.active || NetworkServer.active) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (verboseLog)
        {
            Debug.Log("[GM] 停止网络 后 clientActive=" + NetworkClient.active
                      + " serverActive=" + NetworkServer.active);
        }

        TryLoadSceneSync();
    }

    void TryLoadSceneSync()
    {
        try
        {
            string sceneName = SceneManager.GetActiveScene().name;
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;

            if (verboseLog)
            {
                Debug.Log("[GM] 场景返回 name=" + sceneName + " index=" + sceneIndex);
            }

            if (sceneIndex >= 0) SceneManager.LoadScene(sceneIndex);
            else SceneManager.LoadScene(sceneName);

            if (verboseLog) Debug.Log("[GM] LoadScene 已触发");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 场景返回异常: " + e);
            if (resultText != null) resultText.text = "返回菜单出错: " + e.Message;
            returning = false;
        }
    }
}
