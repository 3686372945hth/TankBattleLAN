using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;

    [Header("UI引用")]
    public TMP_InputField ipInputField;
    public TMP_Text tipText;
    public GameObject mainMenuPanel;

    void Awake()
    {
        singleton = this;
    }

    void Start()
    {
        // 开局默认隐藏安卓触控面板，进入游戏再显示
        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.gameObject.SetActive(false);
        }
    }

    // 创建主机按钮点击事件
    public void OnClickHost()
    {
        tipText.text = "正在创建主机...";
        CustomNetworkManager.singleton.StartHost();
    }

    // 加入游戏按钮点击事件
    public void OnClickJoin()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            tipText.text = "请输入主机IP地址";
            return;
        }

        tipText.text = "正在连接...";
        CustomNetworkManager.singleton.networkAddress = ip;
        CustomNetworkManager.singleton.StartClient();
    }

    // 隐藏主菜单，同时显示游戏触控面板
    public void HideMenu()
    {
        mainMenuPanel.SetActive(false);

        if (TouchInputManager.singleton != null)
        {
            TouchInputManager.singleton.gameObject.SetActive(true);
        }
    }
}
