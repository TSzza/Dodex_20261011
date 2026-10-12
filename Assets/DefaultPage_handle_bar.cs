using UnityEngine;
using UnityEngine.EventSystems;

public class DefaultPage_handle_bar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public DefaultPage_Manager window;
    public DefaultPage_Manager.ResizeDirection resizeMode;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (resizeMode == DefaultPage_Manager.ResizeDirection.Drag)
        {
            // 标题栏，整体拖动
            window.StartDragWindow();
        }
        else
        {
            // 边缘/四角拉伸
            window.StartResize(resizeMode);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (resizeMode == DefaultPage_Manager.ResizeDirection.Drag)
        {
            window.StopDragWindow();
        }
        else
        {
            window.StopResize();
        }
    }
}
