using UnityEngine;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager singleton;

    [Header("结算面板")]
    public GameObject gameOverPanel;
    public TMP_Text resultText;

    void Awake()
    {
        singleton = this;
    }

    void Start()
    {
        // 开局强制隐藏面板和文本，确保初始状态完全正确
        gameOverPanel.SetActive(false);
        resultText.gameObject.SetActive(false);
    }

    [Server]
    public void ServerEndGame(NetworkConnectionToClient winnerConn)
    {
        string result = winnerConn.connectionId == 0 ? "玩家1（主机）获胜！" : "玩家2（客户端）获胜！";
        RpcShowGameOver(result);
    }

    [ClientRpc]
    void RpcShowGameOver(string resultStr)
    {
        gameOverPanel.SetActive(true);
        resultText.gameObject.SetActive(true);
        resultText.text = resultStr;
    }
}
