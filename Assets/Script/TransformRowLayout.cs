using UnityEngine;

/// <summary>
/// Transform 版的"网格排列"组件 —— 给普通 2D 物体（Sprite / tile）用的。
///
/// 为什么需要它：Unity 的 Horizontal/Vertical/Grid Layout Group 是 UGUI 组件，
/// **只处理带 RectTransform 的 UI 子物体**；普通 Transform 的子物体
/// （比如 _tile 这类 2D prefab）会被完全无视，所以挂在 line1 上"不自动排序"。
///
/// 用法：把本组件挂到 line1 上（和任何 Layout Group 二选一，别同时挂）。
///   · Columns       每行放几个；设成很大的数（如 99）就是单行横排
///   · Spacing X/Y   相邻两个的横向 / 纵向中心距
///   · Alignment     Centered = 整个网格居中在父物体原点（第一行在上）；
///                   TopLeft  = 从父物体原点向右、向下排
/// 子物体增删、Inspector 改参数时自动重排；也可右键组件标题选"立即排列"。
/// </summary>
[ExecuteAlways]
public class TransformRowLayout : MonoBehaviour
{
    public enum Alignment { Centered, TopLeft }

    [Min(1), Tooltip("每行放几个（行数 = 子物体总数向上取整 ÷ 列数）")]
    public int columns = 3;

    [Min(0), Tooltip("横向中心距")]
    public float spacingX = 1f;

    [Min(0), Tooltip("纵向中心距")]
    public float spacingY = 1f;

    [Tooltip("Centered = 网格整体居中；TopLeft = 从原点向右下排")]
    public Alignment alignment = Alignment.Centered;

    void Awake()
    {
        Apply();
    }

    void OnTransformChildrenChanged()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    [ContextMenu("立即排列")]
    public void Apply()
    {
        int n = transform.childCount;
        if (n == 0) return;

        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt((float)n / cols);

        for (int i = 0; i < n; i++)
        {
            int row = i / cols;   // 第几行
            int col = i % cols;   // 行内第几个

            float x, y;
            if (alignment == Alignment.Centered)
            {
                float halfW = (cols - 1) * spacingX * 0.5f;
                float halfH = (rows - 1) * spacingY * 0.5f;
                x = col * spacingX - halfW;   // 水平居中
                y = halfH - row * spacingY;   // 第一行在上，向下增长，垂直居中
            }
            else // TopLeft：从父物体原点向右、向下
            {
                x = col * spacingX;
                y = -row * spacingY;
            }

            Transform c = transform.GetChild(i);
            c.localPosition = new Vector3(x, y, c.localPosition.z);
        }
    }
}
