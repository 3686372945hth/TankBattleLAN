using UnityEngine;
using Mirror;

public class BrickSpawner : MonoBehaviour
{
    public static BrickSpawner singleton;

    [Header("砖墙预制体")]
    public GameObject brickPrefab;

    [Header("生成区域大小")]
    public float areaWidth = 14f;
    public float areaHeight = 9f;

    [Header("生成数量")]
    public int brickCount = 40;

    [Header("左右避让宽度")]
    public float sideAvoidWidth = 2.5f;

    void Awake()
    {
        singleton = this;
    }

    [Server]
    public void ServerSpawnBricks()
    {
        // 清空已有的旧砖墙
        GameObject[] oldBricks = GameObject.FindGameObjectsWithTag("Wall");
        foreach (var brick in oldBricks)
        {
            if (brick.TryGetComponent(out NetworkIdentity netId))
            {
                NetworkServer.Destroy(brick);
            }
        }

        float minX = -areaWidth / 2 + sideAvoidWidth;
        float maxX = areaWidth / 2 - sideAvoidWidth;
        float minY = -areaHeight / 2;
        float maxY = areaHeight / 2;

        for (int i = 0; i < brickCount; i++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            Vector3 pos = new Vector3(x, y, 0);

            // 根节点生成，无父物体，网络同步兼容性最好
            GameObject brick = Instantiate(brickPrefab, pos, Quaternion.identity);
            brick.name = "Brick_" + i;
            NetworkServer.Spawn(brick);
        }
    }
}
