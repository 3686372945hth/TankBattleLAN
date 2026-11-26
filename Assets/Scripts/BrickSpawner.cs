using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    public static BrickSpawner singleton;

    [Header("砖墙预制体")]
    public GameObject brickPrefab;

    private const int BrickCount = 180;
    private const float SpawnSafeRadius = 1.8f;
    private const float BaseSafeRadius = 2.0f;
    private const float BrickSpacing = 1.15f;
    private const float SteelClearance = 1.3f;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    [Server]
    public void ServerSpawnBricks()
    {
        GameObject[] oldBricks = GameObject.FindGameObjectsWithTag("Wall");
        foreach (var brick in oldBricks)
        {
            if (brick.TryGetComponent(out NetworkIdentity netId))
            {
                NetworkServer.Destroy(brick);
            }
        }

        float minX = -MapConfig.HalfWidth + MapConfig.BrickPadding;
        float maxX = MapConfig.HalfWidth - MapConfig.BrickPadding;
        float minY = -MapConfig.HalfHeight + MapConfig.BrickPadding;
        float maxY = MapConfig.HalfHeight - MapConfig.BrickPadding;

        List<Vector2> placed = new List<Vector2>();

        for (int i = 0; i < BrickCount; i++)
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
                    if (Vector2.Distance(candidate, p) < BrickSpacing)
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

            if (!found) continue;

            placed.Add(pos);

            GameObject brick = Instantiate(brickPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            brick.name = "Brick_" + i;
            NetworkServer.Spawn(brick);
        }
    }

    bool IsBlocked(Vector2 p)
    {
        if (Vector2.Distance(p, MapConfig.SpawnP1) < SpawnSafeRadius) return true;
        if (Vector2.Distance(p, MapConfig.SpawnP2) < SpawnSafeRadius) return true;
        if (Vector2.Distance(p, MapConfig.BaseLeft) < BaseSafeRadius) return true;
        if (Vector2.Distance(p, MapConfig.BaseRight) < BaseSafeRadius) return true;

        // 避开内部随机钢墙，防止砖墙和钢墙叠在一起
        if (SteelWallSpawner.singleton != null &&
            SteelWallSpawner.singleton.IsNearRandomWall(p, SteelClearance)) return true;

        return false;
    }
}
