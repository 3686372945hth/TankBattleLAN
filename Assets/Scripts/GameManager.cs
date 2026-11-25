using Mirror;
using UnityEngine;
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
    [Tooltip("屏幕左上角显示每台坦克血量，联调完记得关掉")]
    public bool showDebugHp = true;

    [Tooltip("把结算、点击、返回流程的每一步都打进日志")]
    public bool verboseLog = true;

    [System.NonSerialized] public bool isGameOver;

    private bool serverEnded;
    private bool returning;
    private bool matchStarted;
    private float nextHeartbeat;

    void Awake()
    {
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

        if (verboseLog)
        {
            Debug.Log("[GM] Start panel=" + (gameOverPanel != null)
                      + " text=" + (resultText != null)
                      + " EventSystem=" + (EventSystem.current != null)
                      + " clientActive=" + NetworkClient.active
                      + " serverActive=" + NetworkServer.active);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!isGameOver && Input.GetKeyDown(KeyCode.F1))
        {
            ShowGameOverUI("测试结算", TankController.ResultWin);
        }
#endif

        if (!isGameOver) return;

        // 心跳：每 0.5 秒证明 Update 还在跑，并输出鼠标状态
        if (verboseLog && Time.unscaledTime >= nextHeartbeat)
        {
            nextHeartbeat = Time.unscaledTime + 0.5f;
            Vector3 mp = Input.mousePosition;
            Debug.Log("[GM] 心跳 鼠标=(" + mp.x + "," + mp.y + ")"
                      + " 按住=" + Input.GetMouseButton(0)
                      + " EventSystem=" + (EventSystem.current != null));
        }

        // 输入探针：任何按键 / 鼠标按下都会留痕
        if (Input.anyKeyDown)
        {
            Debug.Log("[GM] 收到输入 鼠标左键=" + Input.GetMouseButtonDown(0)
                      + " 空格=" + Input.GetKeyDown(KeyCode.Space));
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

    // 临时调试：屏幕左上角显示每台坦克的血量
    void OnGUI()
    {
        if (!showDebugHp) return;

        TankController[] tanks = FindObjectsOfType<TankController>();
        GUILayout.BeginArea(new Rect(10, 10, 320, 40 + tanks.Length * 26));
        GUILayout.Label("HP 监控");

        foreach (TankController t in tanks)
        {
            TankHealth h = t.GetComponent<TankHealth>();
            if (h != null)
            {
                GUILayout.Label("netId " + t.netId + " : " + h.currentHealth + "/" + h.maxHealth
                                + (t.isLocalPlayer ? "  (本机)" : ""));
            }
        }

        GUILayout.EndArea();
    }

    public void ServerMarkMatchStarted()
    {
        if (!NetworkServer.active) return;
        matchStarted = true;

        if (verboseLog) Debug.Log("[GM] 对局已开始");
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
            Debug.Log("[GM] ShowGameOverUI 进入 id=" + GetInstanceID()
                      + " state=" + state
                      + " active=" + gameObject.activeInHierarchy
                      + " componentEnabled=" + enabled
                      + " panel=" + (gameOverPanel != null)
                      + " text=" + (resultText != null));
        }

        isGameOver = true;
        nextHeartbeat = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            string head = "";
            if (state == TankController.ResultWin) head = "胜利！";
            else if (state == TankController.ResultLose) head = "失败";

            resultText.text = head.Length > 0 ? head + "\n" + resultStr : resultStr;
        }
    }

    // 按钮 OnClick 绑定这里，空格 / 点击屏幕也走这里
    public void ReturnToMenu()
    {
        Debug.Log("[GM] ReturnToMenu 进入 returning=" + returning
                  + " clientActive=" + NetworkClient.active
                  + " serverActive=" + NetworkServer.active
                  + " id=" + GetInstanceID());

        if (returning) return;
        returning = true;

        try
        {
            StartCoroutine(ReturnRoutine());
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 协程启动失败: " + e);
            returning = false;
        }
    }

    System.Collections.IEnumerator ReturnRoutine()
    {
        CustomNetworkManager.quitting = true;

        if (resultText != null) resultText.text = "正在返回主菜单";

        // 第一步：停网络（包在 try 里，异常只记录不中断）
        try
        {
            NetworkManager nm = NetworkManager.singleton;
            Debug.Log("[GM] 停止网络 前 clientActive=" + NetworkClient.active
                      + " serverActive=" + NetworkServer.active);

            if (nm != null)
            {
                if (NetworkClient.active && NetworkServer.active)
                {
                    nm.StopHost();
                }
                else if (NetworkClient.active)
                {
                    nm.StopClient();
                }
                else if (NetworkServer.active)
                {
                    nm.StopServer();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 停止网络异常: " + e);
        }

        // 第二步：等网络真正停干净（最多 2 秒）
        float deadline = Time.realtimeSinceStartup + 2f;
        while ((NetworkClient.active || NetworkServer.active) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Debug.Log("[GM] 停止网络 后 clientActive=" + NetworkClient.active
                  + " serverActive=" + NetworkServer.active);

        // 第三步：重载场景
        try
        {
            string sceneName = SceneManager.GetActiveScene().name;
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;

            Debug.Log("[GM] 重载场景 name=" + sceneName + " index=" + sceneIndex);

            if (sceneIndex >= 0)
            {
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }

            Debug.Log("[GM] LoadScene 调用完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GM] 重载场景异常: " + e);
            if (resultText != null) resultText.text = "重载异常: " + e.Message;
            returning = false;
        }
    }
}
