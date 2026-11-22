using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public static Joystick singleton;

    [Header("摇杆参数")]
    public float handleRange = 60f; // 摇杆头最大移动半径

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private RectTransform bgRect;
    private RectTransform handleRect;

    void Awake()
    {
        singleton = this;
        bgRect = GetComponent<RectTransform>();
        handleRect = transform.Find("JoystickHandle").GetComponent<RectTransform>();
    }

    // 按下摇杆
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    // 拖拽摇杆
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgRect, eventData.position, eventData.pressEventCamera, out localPos);

        // 计算方向并限制最大半径
        Vector2 direction = localPos / (bgRect.sizeDelta.x / 2f);
        if (direction.magnitude > 1f)
        {
            direction = direction.normalized;
        }

        Horizontal = direction.x;
        Vertical = direction.y;

        // 更新摇杆头位置
        handleRect.anchoredPosition = direction * handleRange;
    }

    // 松开摇杆，回中归零
    public void OnPointerUp(PointerEventData eventData)
    {
        Horizontal = 0;
        Vertical = 0;
        handleRect.anchoredPosition = Vector2.zero;
    }
}
