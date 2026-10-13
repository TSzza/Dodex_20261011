using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[SelectionBase]
public class Desktop_IconButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Desktop_Manager desktop_Manager;
    [Header("按钮类型")]
    public CanvasLayer_Manager.CanvasType myCanvasType;
    [Header("双击判定最大间隔")]
    public float doubleClickThreshold = 0.3f;
    public float DragScreenMargin = 0.2f; // 离屏幕边缘最小距离

    private float _lastClickTime;
    private int _clickCount;
    private bool _isDragging;
    private Vector2 _pointerDownPos;
    private Vector2 _dragStartMousePos;
    private Vector2 _dragStartAnchoredPos;

    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    private void Start()
    {
        desktop_Manager.PlaceUI(_rt, _rt.anchoredPosition);
    }

    // 按下
    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = false;
        _pointerDownPos = eventData.position;
        _dragStartMousePos = eventData.position;
        _dragStartAnchoredPos = _rt.anchoredPosition;
        _clickCount++;

        if (Time.time - _lastClickTime <= doubleClickThreshold)
        {
            _clickCount = 2;
        }
        else
        {
            _lastClickTime = Time.time;
        }
    }

    // 抬起
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_clickCount == 2)
        {
            // 双击：打开窗口
            CanvasLayer_Manager.Instance.Open(myCanvasType);
            _clickCount = 0;
        }
        if (_isDragging)
        {
            _isDragging = false;
            // 拖拽结束才调用PlaceUI，拖拽过程不调用
            desktop_Manager.PlaceUI(_rt, _rt.anchoredPosition);
            desktop_Manager.ClickUI(_rt.anchoredPosition);
            _clickCount = 0;
        }

        desktop_Manager.ClickUI(_rt.anchoredPosition);

        if (Time.time - _lastClickTime > doubleClickThreshold)
        {
            _clickCount = 0;
        }
    }

    // 拖拽（和DefaultPage_Manager完全一致的屏幕坐标+Margin+mouseMult逻辑）
    public void OnDrag(PointerEventData eventData)
    {
        float dragDistance = Vector2.Distance(eventData.position, _pointerDownPos);
        if (!_isDragging && dragDistance > 5f) // 开始拖拽
        {
            _isDragging = true;
            desktop_Manager.ClearAtPosition(_rt.anchoredPosition);
        }

        if (_isDragging)
        {
            Vector2 mousePos = eventData.position;
            // 屏幕边界限制
            mousePos.x = Mathf.Clamp(mousePos.x, Screen.width * DragScreenMargin, Screen.width * (1f - DragScreenMargin));
            mousePos.y = Mathf.Clamp(mousePos.y, Screen.height * DragScreenMargin, Screen.height * (1f - DragScreenMargin));

            Vector2 mouseDelta = mousePos - _dragStartMousePos;
            mouseDelta *= Root.Instance.MouseMult;

            Vector2 newAnchoredPos = _dragStartAnchoredPos + mouseDelta;
            _rt.anchoredPosition = newAnchoredPos;
        }
    }

    void Update()
    {
        if (_clickCount == 1 && Time.time - _lastClickTime > doubleClickThreshold)
        {
            _clickCount = 0;
        }
    }
}
