using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;

    [Header("UI引用")]
    public TMP_InputField ipInputField;
    public TMP_Text tipText;
    public GameObject mainMenuPanel;

    private bool networkStarting;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    void Start()
    {
        // 进入默认主菜单状态，先隐藏触屏操作面板
        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.gameObject.SetActive(false);
        }
    }

    // 创建房间按钮事件
    public void OnClickHost()
    {
        if (networkStarting) return;

        if (CustomNetworkManager.singleton == null)
        {
            SetTip("网络管理器未就绪");
            return;
        }

        networkStarting = true;
        SetTip("正在创建房间...");

        try
        {
            CustomNetworkManager.singleton.StartHost();
        }
        catch (System.Exception e)
        {
            networkStarting = false;
            SetTip("创建房间失败: " + e.Message);
        }
    }

    // 加入房间按钮事件
    public void OnClickJoin()
    {
        if (networkStarting) return;

        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(ip))
        {
            SetTip("请输入主机IP地址");
            return;
        }

        if (!IsValidAddress(ip))
        {
            SetTip("IP地址格式不正确");
            return;
        }

        if (CustomNetworkManager.singleton == null)
        {
            SetTip("网络管理器未就绪");
            return;
        }

        networkStarting = true;
        SetTip("正在连接...");

        try
        {
            CustomNetworkManager.singleton.networkAddress = ip;
            CustomNetworkManager.singleton.StartClient();
        }
        catch (System.Exception e)
        {
            networkStarting = false;
            SetTip("连接失败: " + e.Message);
        }
    }

    // 连接成功后隐藏菜单，同时隐藏触屏操作面板
    public void HideMenu()
    {
        networkStarting = false;
        mainMenuPanel.SetActive(false);

        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.gameObject.SetActive(true);
        }
    }

    // 连接失败 / 被拒绝时回到主菜单并提示
    public void ShowConnectFailed()
    {
        networkStarting = false;
        mainMenuPanel.SetActive(true);

        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.gameObject.SetActive(false);
        }

        SetTip("连接失败，请检查 IP");
    }

    void SetTip(string message)
    {
        if (tipText != null) tipText.text = message;
    }

    // 地址粗校验：允许 IPv4 / 主机名，拒绝空格与非法字符
    bool IsValidAddress(string addr)
    {
        foreach (char c in addr)
        {
            if (char.IsWhiteSpace(c)) return false;
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '-')) return false;
        }
        return true;
    }
}
