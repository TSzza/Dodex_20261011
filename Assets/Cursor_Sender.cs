using UnityEngine;
using UnityEngine.EventSystems;

public class Cursor_Sender : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    /// <summary>
    /// 行为模式
    /// NormalButton：悬浮link，按下pin，离开恢复pointer（标题栏、普通按钮）
    /// ResizeHandle：永远使用指定光标，按下不改变（四边、四角拉伸手柄）
    /// </summary>
    public enum HoverCursorBehavior
    {
        NormalButton,
        ResizeHandle
    }

    [Header("光标行为模式")]
    public HoverCursorBehavior behavior;

    [Header("ResizeHandle模式：固定使用的光标")]
    public CursorType resizeCursor;

    private bool _isHovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        if (Cursor_Manager.Instance == null) return;

        if (behavior == HoverCursorBehavior.NormalButton)
        {
            Cursor_Manager.Instance.SetCursorType(CursorType.link);
        }
        else if (behavior == HoverCursorBehavior.ResizeHandle)
        {
            Cursor_Manager.Instance.SetCursorType(resizeCursor);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (Cursor_Manager.Instance == null) return;
        Cursor_Manager.Instance.SetCursorType(CursorType.pointer);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isHovering || Cursor_Manager.Instance == null) return;

        if (behavior == HoverCursorBehavior.NormalButton)
        {
            //普通按钮按下切换pin
            Cursor_Manager.Instance.SetCursorType(CursorType.pin);
        }
        // ResizeHandle：按下不变化，保持原有resizeCursor
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isHovering || Cursor_Manager.Instance == null) return;

        if (behavior == HoverCursorBehavior.NormalButton)
        {
            //松开回到link
            Cursor_Manager.Instance.SetCursorType(CursorType.link);
        }
        // ResizeHandle：松开不变
    }
}
