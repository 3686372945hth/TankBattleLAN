using Mirror;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随参数")]
    [Tooltip("平滑跟随速度，填 0 表示硬跟随")]
    public float followSpeed = 8f;

    [Header("视野下限")]
    [Tooltip("至少保证看到的视野高度（世界单位）")]
    public float minViewHeight = 13f;

    [Tooltip("至少保证看到的视野宽度（世界单位）")]
    public float minViewWidth = 20f;

    private Camera cam;
    private Transform target;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.orthographic = true;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        UpdateViewSize();
        UpdateTarget();

        Vector3 wanted = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        if (target != null)
        {
            wanted.x = target.position.x;
            wanted.y = target.position.y;
        }

        wanted = ClampToMap(wanted);

        if (followSpeed <= 0f)
        {
            transform.position = wanted;
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            Vector3 p = Vector3.Lerp(transform.position, wanted, t);
            transform.position = ClampToMap(p);
        }
    }

    // 按屏幕宽高比算正交尺寸，保证横屏/竖屏都能看到合理范围
    void UpdateViewSize()
    {
        float aspect = cam.aspect;
        if (aspect <= 0.01f) return;

        float sizeByHeight = minViewHeight / 2f;
        float sizeByWidth = minViewWidth / 2f / aspect;

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }

    // 跟随本地玩家；本地玩家为空时保持当前画面
    void UpdateTarget()
    {
        if (target != null) return;

        NetworkIdentity local = NetworkClient.localPlayer;
        if (local != null) target = local.transform;
    }

    // 相机不越过地图边界，视野比地图大时居中
    Vector3 ClampToMap(Vector3 p)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float x, y;

        if (MapConfig.HalfWidth <= halfW) x = 0f;
        else x = Mathf.Clamp(p.x, -MapConfig.HalfWidth + halfW, MapConfig.HalfWidth - halfW);

        if (MapConfig.HalfHeight <= halfH) y = 0f;
        else y = Mathf.Clamp(p.y, -MapConfig.HalfHeight + halfH, MapConfig.HalfHeight - halfH);

        return new Vector3(x, y, p.z);
    }
}
