using System.Collections.Generic;
using UnityEngine;

public class SteelWallSpawner : MonoBehaviour
{
    public static SteelWallSpawner singleton;

    [Header("钢墙预制体")]
    public GameObject steelWallPrefab;

    [Header("边框钢墙尺寸")]
    public float wallUnitSize = 1f;

    [Header("内部随机钢墙")]
    [Tooltip("随机钢墙数量")]
    public int randomWallCount = 40;

    [Tooltip("随机钢墙之间的最小中心距，需大于坦克宽度")]
    public float randomWallSpacing = 2.2f;

    [Tooltip("离开出生点 / 基地的最小距离")]
    public float keyPointClearance = 3f;

    [Tooltip("离地图边界的最小距离")]
    public float edgeMargin = 1.5f;

    private bool borderSpawned = false;
    private bool randomSpawned = false;

    // 边框物体单独登记，清理时只动边框，不影响随机钢墙
    private readonly List<GameObject> borderObjects = new List<GameObject>();

    // 内部随机钢墙的位置，服务端生成后供砖墙避让使用
    private readonly List<Vector2> randomPositions = new List<Vector2>();

    void Awake()
    {
        singleton = this;
    }

    // 外围钢墙边界，服务端和客户端各自调用，位置完全一致
    public void SpawnBorder()
    {
        if (borderSpawned) return;
        borderSpawned = true;

        // 只清理上一局遗留的边框，不动随机钢墙
        foreach (GameObject old in borderObjects)
        {
            if (old != null) Destroy(old);
        }
        borderObjects.Clear();

        float halfWidth = MapConfig.HalfWidth;
        float halfHeight = MapConfig.HalfHeight;

        for (float x = -halfWidth; x <= halfWidth; x += wallUnitSize)
        {
            borderObjects.Add(Instantiate(steelWallPrefab, new Vector3(x, halfHeight, 0), Quaternion.identity, transform));
        }

        for (float x = -halfWidth; x <= halfWidth; x += wallUnitSize)
        {
            borderObjects.Add(Instantiate(steelWallPrefab, new Vector3(x, -halfHeight, 0), Quaternion.identity, transform));
        }

        for (float y = -halfHeight + wallUnitSize; y <= halfHeight - wallUnitSize; y += wallUnitSize)
        {
            borderObjects.Add(Instantiate(steelWallPrefab, new Vector3(-halfWidth, y, 0), Quaternion.identity, transform));
        }

        for (float y = -halfHeight + wallUnitSize; y <= halfHeight - wallUnitSize; y += wallUnitSize)
        {
            borderObjects.Add(Instantiate(steelWallPrefab, new Vector3(halfWidth, y, 0), Quaternion.identity, transform));
        }
    }

    // 内部随机钢墙：两端用同一个 seed，算出同一批位置
    public void SpawnRandomWalls(int seed)
    {
        if (randomSpawned) return;
        randomSpawned = true;

        randomPositions.Clear();
        randomPositions.AddRange(BuildRandomPositions(seed));

        foreach (Vector2 p in randomPositions)
        {
            Instantiate(steelWallPrefab, new Vector3(p.x, p.y, 0f), Quaternion.identity, transform);
        }
    }

    // 砖墙生成时用来避让
    public bool IsNearRandomWall(Vector2 p, float radius)
    {
        foreach (Vector2 w in randomPositions)
        {
            if (Vector2.Distance(p, w) < radius) return true;
        }

        return false;
    }

    // 纯计算，不实例化，服务端和客户端算出来的结果必须完全一致
    List<Vector2> BuildRandomPositions(int seed)
    {
        List<Vector2> result = new List<Vector2>();
        LcgRandom rng = new LcgRandom(seed);

        float minX = -MapConfig.HalfWidth + edgeMargin;
        float maxX = MapConfig.HalfWidth - edgeMargin;
        float minY = -MapConfig.HalfHeight + edgeMargin;
        float maxY = MapConfig.HalfHeight - edgeMargin;

        for (int i = 0; i < randomWallCount; i++)
        {
            for (int attempt = 0; attempt < 120; attempt++)
            {
                Vector2 candidate = new Vector2(rng.Range(minX, maxX), rng.Range(minY, maxY));

                if (Vector2.Distance(candidate, MapConfig.SpawnP1) < keyPointClearance) continue;
                if (Vector2.Distance(candidate, MapConfig.SpawnP2) < keyPointClearance) continue;
                if (Vector2.Distance(candidate, MapConfig.BaseLeft) < keyPointClearance) continue;
                if (Vector2.Distance(candidate, MapConfig.BaseRight) < keyPointClearance) continue;

                bool tooClose = false;
                foreach (Vector2 p in result)
                {
                    if (Vector2.Distance(candidate, p) < randomWallSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                result.Add(candidate);
                break;
            }
        }

        return result;
    }
}

// 跨平台确定性的线性同余随机，避免 UnityEngine.Random 在 PC / Android 上序列不一致
public class LcgRandom
{
    private uint state;

    public LcgRandom(int seed)
    {
        state = seed <= 0 ? 1u : (uint)seed;
    }

    public float NextFloat()
    {
        state = unchecked(state * 1664525u + 1013904223u);
        return (state >> 8) / 16777216f;
    }

    public float Range(float min, float max)
    {
        return min + NextFloat() * (max - min);
    }
}
