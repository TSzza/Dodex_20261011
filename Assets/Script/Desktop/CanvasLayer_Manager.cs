using UnityEngine;
using System.Collections.Generic;
using static UnityEditor.Progress;

public class CanvasLayer_Manager : MonoBehaviour
{
    // 和InteractButton保持一致
    public enum CanvasType
    {
        music,
        dodex
    }
    public static CanvasLayer_Manager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    [System.Serializable]
    public class CanvasLayerItem
    {
        public CanvasType type;
        public Canvas canvas;
        public GameObject IconButton;
        public GameObject BigButton;
        public GameObject SmallButton;
    }

    [Header("画布条目配置（Inspector配置）")]
    public List<CanvasLayerItem> canvasItemList = new List<CanvasLayerItem>();

    [Header("最大化尺寸参数（对外可读写）")]
    public Vector3 maximizePos = new Vector3(-497, 15, 0);
    public Vector2 maximizeSize = new Vector2(750, 384);

    // 用于排序记录的列表，保存层级顺序
    private List<CanvasLayerItem> _sortedCanvasList = new List<CanvasLayerItem>();

    void Start()
    {
        InitializeOrder();
    }

    /// <summary>
    /// 初始化：从1开始依次给所有Canvas分配OrderInLayer
    /// </summary>
    public void InitializeOrder()
    {
        _sortedCanvasList.Clear();
        _sortedCanvasList.AddRange(canvasItemList);

        for (int i = 0; i < _sortedCanvasList.Count; i++)
        {
            CanvasLayerItem item = _sortedCanvasList[i];
            if (item.canvas != null)
            {
                item.canvas.sortingOrder = i + 1;
            }
        }
    }

    /// <summary>
    /// 双击触发：传入类型，激活对应Canvas，放到最顶层，其余顺序保持不变
    /// </summary>
    public void Open(CanvasType targetType)
    {
        //Debug.Log($"Open {targetType}");

        // 找到目标条目
        CanvasLayerItem item = _sortedCanvasList.Find(x => x.type == targetType);
        
        //Debug.Log($"Open {item} {item.canvas}");

        if (item == null || item.canvas == null) return;

        // 激活目标Canvas
        item.canvas.gameObject.SetActive(true);

        // 从原有列表移除，插入到末尾（最顶层）
        _sortedCanvasList.Remove(item);
        _sortedCanvasList.Add(item);

        // 重新分配order
        for (int i = 0; i < _sortedCanvasList.Count; i++)
        {
            CanvasLayerItem otheritem = _sortedCanvasList[i];
            if (otheritem.canvas != null)
            {
                otheritem.canvas.sortingOrder = (i + 1)*10;
			}
		}

        
        if (item.BigButton != null)
            item.BigButton.SetActive(true);
        if (item.SmallButton != null)
            item.SmallButton.SetActive(false);
        
        item.canvas.GetComponent<DefaultPage_Manager>().isMinimized = false;
    }

    /// <summary>
    /// 最小化触发函数：Canvas关闭，BigButton激活，SmallButton关闭
    /// </summary>
    public void Minimize(CanvasType targetType)
    {
        CanvasLayerItem item = canvasItemList.Find(x => x.type == targetType);
        if (item == null) return;

        if (item.canvas != null)
            item.canvas.gameObject.SetActive(false);

        if (item.BigButton != null)
            item.BigButton.SetActive(true);
        if (item.SmallButton != null)
            item.SmallButton.SetActive(false);

        item.canvas.GetComponent<DefaultPage_Manager>().isMinimized = true; // 通知其为最小化
    }

    /// <summary>
    /// 关闭界面触发函数：Canvas关闭，BigButton关闭，SmallButton激活
    /// </summary>
    public void Close(CanvasType targetType)
    {
        CanvasLayerItem item = canvasItemList.Find(x => x.type == targetType);
        if (item == null) return;

        if (item.canvas != null)
            item.canvas.gameObject.SetActive(false);

        if (item.BigButton != null)
            item.BigButton.SetActive(false);
        if (item.SmallButton != null)
            item.SmallButton.SetActive(true);
        
        item.canvas.GetComponent<DefaultPage_Manager>().isMinimized = false;
    }

    /// <summary>
    /// 最大化：设置RectTransform位置与宽高
    /// </summary>
    public void Maximize(CanvasType targetType)
    {
        CanvasLayerItem item = canvasItemList.Find(x => x.type == targetType);
        if (item == null || item.canvas == null) return;

        RectTransform rt = item.canvas.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchoredPosition3D = maximizePos;
        rt.sizeDelta = maximizeSize;
        
        item.canvas.GetComponent<DefaultPage_Manager>().isMinimized = false;
        item.canvas.GetComponent<DefaultPage_Manager>().isMaximized = true; // 通知其为最大化
    }

    /// <summary>
    /// 初始化触发函数：读取DefaultPageManager的原始位置尺寸并应用
    /// </summary>
    public void ApplyOriginal(CanvasType targetType)
    {
        CanvasLayerItem item = canvasItemList.Find(x => x.type == targetType);
        if (item == null || item.canvas == null) return;

        DefaultPage_Manager pageManager = item.canvas.GetComponent<DefaultPage_Manager>();
        if (pageManager == null)
        {
            Debug.LogWarning("找不到DefaultPageManager组件");
            return;
        }

        RectTransform rt = item.canvas.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchoredPosition3D = pageManager.OriPos;
        rt.sizeDelta = new Vector2(pageManager.OriWidth, pageManager.OriHeight);
        
        item.canvas.GetComponent<DefaultPage_Manager>().isMinimized = false;
        item.canvas.GetComponent<DefaultPage_Manager>().isMaximized = false; // 通知最大化撤销，最大化会跨开关窗口保存
    }
    
    // 根据类型获取DefaultPage_Manager，给Maximize按钮调用
    public DefaultPage_Manager GetPageManagerByType(CanvasType targetType)
    {
        var item = canvasItemList.Find(x => x.type == targetType);
        if (item == null || item.canvas == null) return null;
        return item.canvas.GetComponent<DefaultPage_Manager>();
    }
}
