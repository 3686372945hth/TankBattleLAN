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
    [Tooltip("在屏幕左上角显示每台坦克的血量，仅调试时开启")]
    public bool showDebugHp = false;

    [Tooltip("在关键流程打印详细日志，排查问题时开启")]
    public bool verboseLog = false;

    [System.NonSerialized] public bool isGameOver;

    private bool serverEnded;
    private bool returning;
    private bool matchStarted;
    private float nextHeartbeat;

    private TextMeshProUGUI countdownText;
    private Coroutine countdownRoutine;
    private Coroutine hideCountdownRoutine;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;

        // 强制关闭调试面板与详细日志，避免场景里勾选的旧值残留
        showDebugHp = false;
        verboseLog = false;

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
        if (!isGameOver) return;

        if (verboseLog && Time.unscaledTime >= nextHeartbeat)
        {
            nextHeartbeat = Time.unscaledTime + 0.5f;
            Vector3 mp = Input.mousePosition;
            Debug.Log("[GM] 鼠标 位置=(" + mp.x + "," + mp.y + ")"
                      + " 左键=" + Input.GetMouseButton(0)
                      + " 触摸=" + Input.touchCount
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

    // 调试用：在屏幕左上角显示每台坦克的血量
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

        if (verboseLog) Debug.Log("[GM] 对局开始，准备进入倒数");

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

    // 通过 SyncVar hook 在客户端触发，刷新本地的倒数显示
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

    // 对局结束时停止仍在进行的倒数协程
    void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    public void ServerEndGame(NetworkConnectionToClient winnerConn)
    {
        if (!NetworkServer.active) return;
        if (serverEnded) return;
        serverEnded = true;

        StopCountdown();

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
        StopCountdown();

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
            Debug.Log("[GM] ShowGameOverUI 实例 id=" + GetInstanceID()
                      + " state=" + state
                      + " active=" + gameObject.activeInHierarchy
                      + " componentEnabled=" + enabled
                      + " panel=" + (gameOverPanel != null)
                      + " text=" + (resultText != null));
        }

        isGameOver = true;
        nextHeartbeat = 0f;

        // 结算时禁用触屏操作面板，避免开火按钮与返回菜单按钮同时响应
        if (TouchInputManager.singleton != null && TouchInputManager.singleton.gameObject.activeSelf)
        {
            TouchInputManager.singleton.gameObject.SetActive(false);
            if (verboseLog) Debug.Log("[GM] 已禁用触屏输入");
        }

        // 结算面板置顶，并保证整面板可点击，点击任意位置返回菜单
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

            if (verboseLog) Debug.Log("[GM] 已绑定结算面板按钮");
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            string head = "";
            if (state == TankController.ResultWin) head = "胜利";
            else if (state == TankController.ResultLose) head = "失败";

            // 对手退出时没有标题，直接显示副标题；其余情况标题 + 副标题
            resultText.text = head.Length > 0 ? head + "\n" + resultStr : resultStr;
        }
    }

    // 点击按钮 / 点屏幕返回菜单；防重入，场景被禁用时走同步兜底
    public void ReturnToMenu()
    {
        if (returning) return;
        returning = true;

        CustomNetworkManager.quitting = true;

        // 组件尚未激活时，等待期间 Mirror 可能已经清理禁用，无法走协程，直接同步处理
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
                Debug.Log("[GM] 停止网络前 clientActive=" + NetworkClient.active
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
            Debug.Log("[GM] 停止网络后 clientActive=" + NetworkClient.active
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
                Debug.Log("[GM] 加载场景 name=" + sceneName + " index=" + sceneIndex);
            }

            if (sceneIndex >= 0) SceneManager.LoadScene(sceneIndex);
            else SceneManager.LoadScene(sceneName);

            if (verboseLog) Debug.Log("[GM] LoadScene 已调用");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 加载场景异常: " + e);
            if (resultText != null) resultText.text = "返回菜单失败: " + e.Message;
            returning = false;
        }
    }
}
