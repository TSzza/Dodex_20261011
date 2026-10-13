using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[SelectionBase]
[RequireComponent(typeof(RectTransform))]
public class DefaultPage_Manager : MonoBehaviour
{
    public CanvasLayer_Manager.CanvasType myType;
    [HideInInspector]
    public Vector3 OriPos;
    [HideInInspector]
    public float OriWidth, OriHeight;

    [Header("引用")]
    public RectTransform windowRect;
    //public DefaultPage_Manager pageManager;

    [Header("窗口尺寸限制")]
    public float minWidth = 200f;
    public float minHeight = 150f;
    private float maxWidth;
    private float maxHeight;

    [Header("窗口位置边界（比例）")]
    public float ResizeScreenMargin = 0.05f; // 离屏幕边缘最小距离
    public float DragScreenMargin = 0.2f; // 离屏幕边缘最小距离
    
    [HideInInspector]
    public bool isMaximized = false;
    [HideInInspector]
    public bool isMinimized = false;

    private bool isDraggingWindow;
    private Vector2 dragStartMousePos;
    private Vector2 dragStartWindowAnchoredPos;

    private bool isResizing;
    private ResizeDirection resizeDir;
    private Vector2 resizeStartMousePos;
    private Vector2 resizeStartWindowSize;
    private Vector2 resizeStartWindowAnchoredPos;

    public enum ResizeDirection
    {
        Drag, Left, Right, Top, Bottom,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    void Awake()
    {
        if (windowRect == null) windowRect = GetComponent<RectTransform>();

        if (windowRect == null) return;
        OriPos = windowRect.anchoredPosition3D;
        OriWidth = windowRect.sizeDelta.x;
        OriHeight = windowRect.sizeDelta.y;
    }
	private void Start() // 设置最大值为最大化
	{
        maxWidth = CanvasLayer_Manager.Instance.maximizeSize.x - 1;
        maxHeight = CanvasLayer_Manager.Instance.maximizeSize.y - 1;
	}

	#region 整体窗口拖动
	public void StartDragWindow()
    {
        isDraggingWindow = true;
        dragStartMousePos = Input.mousePosition;
        dragStartWindowAnchoredPos = windowRect.anchoredPosition;

        isMaximized = false;
    }

    public void StopDragWindow()
    {
        if (isDraggingWindow)
        {
            isDraggingWindow = false;
        }
    }
    #endregion

    #region 边缘/四角缩放入口
    public void StartResize(ResizeDirection dir)
    {
        isResizing = true;
        resizeDir = dir;
        resizeStartMousePos = Input.mousePosition;
        resizeStartWindowSize = windowRect.sizeDelta;
        resizeStartWindowAnchoredPos = windowRect.anchoredPosition;

        isMaximized = false;
    }

    public void StopResize()
    {
        if (isResizing)
        {
            isResizing = false;
        }
    }
    #endregion

    void Update()
    {
        if (isDraggingWindow)
        {
            DragWindowUpdate();
        }
        else if (isResizing)
        {
            ResizeUpdate();
        }
    }

    void DragWindowUpdate()
    {
        Vector2 mousePos = Input.mousePosition;
        // 限制在屏幕内
        mousePos.x = Mathf.Clamp(mousePos.x, Screen.width * DragScreenMargin, Screen.width * (1f - DragScreenMargin));
        mousePos.y = Mathf.Clamp(mousePos.y, Screen.height * DragScreenMargin, Screen.height * (1f - DragScreenMargin));
        Vector2 mouseDelta = mousePos - dragStartMousePos;
        mouseDelta *= Root.Instance.MouseMult;
        Vector2 newAnchoredPos = dragStartWindowAnchoredPos + mouseDelta;

        windowRect.anchoredPosition = newAnchoredPos;
    }

    void ResizeUpdate()
    {
        Vector2 mousePos = Input.mousePosition;
        // 限制在屏幕内
        mousePos.x = Mathf.Clamp(mousePos.x, Screen.width * ResizeScreenMargin, Screen.width * (1f - ResizeScreenMargin));
        mousePos.y = Mathf.Clamp(mousePos.y, Screen.height * ResizeScreenMargin, Screen.height * (1f - ResizeScreenMargin));
        Vector2 mouseDelta = mousePos - resizeStartMousePos;
        mouseDelta *= Root.Instance.MouseMult;

        Vector2 newSize = resizeStartWindowSize;
        Vector2 newPos = resizeStartWindowAnchoredPos;
        switch (resizeDir)
        {
            case ResizeDirection.Right:
                newSize.x = resizeStartWindowSize.x + mouseDelta.x;
                // 向右拉伸：中心跟着向右偏移增量的一半，左边固定不动
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                break;
            case ResizeDirection.Left:
                newSize.x = resizeStartWindowSize.x - mouseDelta.x;
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                break;
            case ResizeDirection.Top:
                newSize.y = resizeStartWindowSize.y + mouseDelta.y;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
            case ResizeDirection.Bottom:
                newSize.y = resizeStartWindowSize.y - mouseDelta.y;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
            case ResizeDirection.TopRight:
                newSize.x = resizeStartWindowSize.x + mouseDelta.x;
                newSize.y = resizeStartWindowSize.y + mouseDelta.y;
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
            case ResizeDirection.TopLeft:
                newSize.x = resizeStartWindowSize.x - mouseDelta.x;
                newSize.y = resizeStartWindowSize.y + mouseDelta.y;
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
            case ResizeDirection.BottomRight:
                newSize.x = resizeStartWindowSize.x + mouseDelta.x;
                newSize.y = resizeStartWindowSize.y - mouseDelta.y;
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
            case ResizeDirection.BottomLeft:
                newSize.x = resizeStartWindowSize.x - mouseDelta.x;
                newSize.y = resizeStartWindowSize.y - mouseDelta.y;
                newPos.x = resizeStartWindowAnchoredPos.x + mouseDelta.x / 2f;
                newPos.y = resizeStartWindowAnchoredPos.y + mouseDelta.y / 2f;
                break;
        }


        // 最小最大尺寸限制
        newSize.x = Mathf.Clamp(newSize.x, minWidth, maxWidth);
        newSize.y = Mathf.Clamp(newSize.y, minHeight, maxHeight);

        windowRect.sizeDelta = newSize;
        windowRect.anchoredPosition = newPos;
    }
}
