using UnityEngine;

// 地图与关卡尺寸的唯一来源，服务端和客户端都读这里，保证两端一致
public static class MapConfig
{
    // 地图总尺寸（世界单位）
    public const float Width = 48f;
    public const float Height = 28f;

    public const float HalfWidth = Width / 2f;    // 24
    public const float HalfHeight = Height / 2f;  // 14

    // 出生点与基地，都在 y = 0 这条线上
    public const float SpawnX = 18f;
    public const float BaseX = 21f;

    // 砖墙活动区到地图边界留的空白
    public const float BrickPadding = 2f;

    public static Vector2 SpawnP1 => new Vector2(-SpawnX, 0f);
    public static Vector2 SpawnP2 => new Vector2(SpawnX, 0f);

    public static Vector2 BaseLeft => new Vector2(-BaseX, 0f);
    public static Vector2 BaseRight => new Vector2(BaseX, 0f);
}
