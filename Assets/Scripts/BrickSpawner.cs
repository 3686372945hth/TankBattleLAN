using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    public static BrickSpawner singleton;

    [Header("砖墙预制体")]
    public GameObject brickPrefab;

    [Header("场地大小")]
    public float areaWidth = 14f;
    public float areaHeight = 9f;

    [Header("砖墙数量")]
    public int brickCount = 40;

    [Header("左右留白")]
    public float sideAvoidWidth = 2.5f;

    [Header("避让距离")]
    [Tooltip("出生点周围不放砖墙的半径")]
    public float spawnSafeRadius = 1.6f;

    [Tooltip("基地周围不放砖墙的半径")]
    public float baseSafeRadius = 1.8f;

    [Tooltip("砖墙中心之间的最小距离")]
    public float brickSpacing = 1.15f;

    void Awake()
    {
        singleton = this;
    }

    [Server]
    public void ServerSpawnBricks()
    {
        // 先清掉旧的砖墙
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

        List<Vector2> placed = new List<Vector2>();

        for (int i = 0; i < brickCount; i++)
        {
            Vector2 pos = Vector2.zero;
            bool found = false;

            for (int attempt = 0; attempt < 200; attempt++)
            {
                Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

                if (IsBlocked(candidate)) continue;

                bool tooClose = false;
                foreach (Vector2 p in placed)
                {
                    if (Vector2.Distance(candidate, p) < brickSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                pos = candidate;
                found = true;
                break;
            }

            if (!found) continue;   // 实在放不下就少放一块，不影响对局

            placed.Add(pos);

            GameObject brick = Instantiate(brickPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            brick.name = "Brick_" + i;
            NetworkServer.Spawn(brick);
        }
    }

    // 出生点 / 基地附近的区域不许放砖
    bool IsBlocked(Vector2 p)
    {
        CustomNetworkManager nm = CustomNetworkManager.singleton;
        if (nm == null) return false;

        if (nm.spawnPointP1 != null && Vector2.Distance(p, nm.spawnPointP1.position) < spawnSafeRadius) return true;
        if (nm.spawnPointP2 != null && Vector2.Distance(p, nm.spawnPointP2.position) < spawnSafeRadius) return true;
        if (Vector2.Distance(p, new Vector2(-nm.basePosX, 0)) < baseSafeRadius) return true;
        if (Vector2.Distance(p, new Vector2(nm.basePosX, 0)) < baseSafeRadius) return true;

        return false;
    }
}
