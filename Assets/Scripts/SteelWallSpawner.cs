using UnityEngine;

public class SteelWallSpawner : MonoBehaviour
{
    public static SteelWallSpawner singleton;

    [Header("钢墙预制体")]
    public GameObject steelWallPrefab;

    [Header("地图边界大小")]
    [Tooltip("和砖墙生成区域尺寸保持一致即可")]
    public float mapWidth = 27f;
    public float mapHeight = 9f;

    [Header("单块钢墙尺寸")]
    [Tooltip("和精灵实际尺寸匹配，默认正方形为1单位")]
    public float wallUnitSize = 1f;

    private bool hasSpawned = false;

    void Awake()
    {
        singleton = this;
    }

    // 生成本地钢墙边界，主机/客户端各自调用，位置完全一致
    public void SpawnBorder()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        // 清空已有子物体，避免重复生成
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        // 生成上边
        for (float x = -halfWidth; x <= halfWidth; x += wallUnitSize)
        {
            SpawnSingleWall(new Vector3(x, halfHeight, 0));
        }

        // 生成下边
        for (float x = -halfWidth; x <= halfWidth; x += wallUnitSize)
        {
            SpawnSingleWall(new Vector3(x, -halfHeight, 0));
        }

        // 生成左边（跳过上下角，避免重复）
        for (float y = -halfHeight + wallUnitSize; y <= halfHeight - wallUnitSize; y += wallUnitSize)
        {
            SpawnSingleWall(new Vector3(-halfWidth, y, 0));
        }

        // 生成右边（跳过上下角，避免重复）
        for (float y = -halfHeight + wallUnitSize; y <= halfHeight - wallUnitSize; y += wallUnitSize)
        {
            SpawnSingleWall(new Vector3(halfWidth, y, 0));
        }
    }

    void SpawnSingleWall(Vector3 position)
    {
        Instantiate(steelWallPrefab, position, Quaternion.identity, transform);
    }
}
