using System.Collections;
using Mirror;
using UnityEngine;

public enum HitKind { Wall, Steel, Tank, Base }

public class FxManager : MonoBehaviour
{
    public static FxManager singleton;

    public static void EnsureExists()
    {
        if (singleton == null)
        {
            GameObject go = new GameObject("FxManager");
            go.AddComponent<FxManager>();
        }
    }

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    public void SpawnHit(Vector3 pos, HitKind kind)
    {
        Color color;
        bool big;

        switch (kind)
        {
            case HitKind.Wall: color = new Color(0.85f, 0.55f, 0.25f); big = false; break;
            case HitKind.Steel: color = new Color(0.60f, 0.70f, 0.85f); big = false; break;
            case HitKind.Tank: color = new Color(1f, 0.45f, 0.20f); big = true; break;
            default: color = Color.red; big = true; break;
        }

        StartCoroutine(HitRoutine(pos, color, big));
    }

    IEnumerator HitRoutine(Vector3 pos, Color color, bool big)
    {
        GameObject template = (SteelWallSpawner.singleton != null)
            ? SteelWallSpawner.singleton.steelWallPrefab
            : null;

        GameObject fx;
        SpriteRenderer sr;

        if (template != null)
        {
            fx = Instantiate(template, pos, Quaternion.identity);
            sr = fx.GetComponent<SpriteRenderer>();

            // 特效副本不需要碰撞体，避免挡住后续子弹
            Collider2D col = fx.GetComponent<Collider2D>();
            if (col != null) Destroy(col);
        }
        else
        {
            fx = new GameObject("HitFx");
            fx.layer = 0;
            fx.transform.position = pos;

            sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveSprite();
        }

        if (sr == null)
        {
            Destroy(fx);
            yield break;
        }

        sr.color = color;
        sr.sortingOrder = 20;

        float startScale = big ? 0.5f : 0.35f;
        float endScale = big ? 1.4f : 0.8f;
        float duration = big ? 0.40f : 0.25f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            fx.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
            sr.color = new Color(color.r, color.g, color.b, 1f - k);

            yield return null;
        }

        Destroy(fx);
    }

    Sprite ResolveSprite()
    {
        SpriteRenderer sr;

        if (NetworkManager.singleton != null && NetworkManager.singleton.playerPrefab != null)
        {
            sr = NetworkManager.singleton.playerPrefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        TankController[] tanks = FindObjectsOfType<TankController>();
        foreach (TankController t in tanks)
        {
            sr = t.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (Bullet b in bullets)
        {
            sr = b.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        return null;
    }
}
