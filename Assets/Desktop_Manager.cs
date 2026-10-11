using System.Collections.Generic;
using UnityEngine;

public class Desktop_Manager : MonoBehaviour
{
    [Header("桌面网格设置【先列后行：一列从上到下排满，再右移下一列】")]
    [Tooltip("每一列有多少行")]
    public int rowCount = 4;
    [Tooltip("总共有多少列")]
    public int columnCount = 5;

    [Tooltip("网格水平间距（列之间距离）")]
    public float cellXSpacing = 120f;
    [Tooltip("网格垂直间距（行之间距离）")]
    public float cellYSpacing = 120f;

    [Tooltip("网格【左上角起点】Canvas世界坐标")]
    public Vector2 gridTopLeftWorldPos = new Vector2(50, -50);

    [Header("Gizmos绘制")]
    public bool drawGizmos = true;
    private Color gizmoPointColor = Color.yellow;
    public float gizmoPointSize = 4f;

    // key:网格全局索引，value:该点存放的UI
    private Dictionary<int, RectTransform> _gridObjects = new Dictionary<int, RectTransform>();

    public int TotalGridCount => rowCount * columnCount;

    #region 索引   行列   世界坐标转换
    /// <summary>
    /// 索引规则：先填满一列（从上到下row增加），再下一列（col增加）
    /// index = col * rowCount + row
    /// </summary>
    public (int col, int row) IndexToColRow(int index)
    {
        int col = index / rowCount;
        int row = index % rowCount;
        return (col, row);
    }

    public int ColRowToIndex(int col, int row)
    {
        if (col < 0 || col >= columnCount || row < 0 || row >= rowCount)
            return -1;
        return col * rowCount + row;
    }

    /// <summary>行列 → Canvas世界坐标</summary>
    public Vector2 ColRowToWorldPos(int col, int row)
    {
        float x = gridTopLeftWorldPos.x + col * cellXSpacing;
        float y = gridTopLeftWorldPos.y - row * cellYSpacing;
        return new Vector2(x, y);
    }

    /// <summary>全局索引 → Canvas世界坐标</summary>
    public Vector2 IndexToWorldPos(int index)
    {
        var (col, row) = IndexToColRow(index);
        return ColRowToWorldPos(col, row);
    }
    #endregion

    #region 对外接口1：输入2D世界坐标，返回最近网格点世界坐标
    public Vector2 GetNearestGridWorldPoint(Vector2 worldPos)
    {
        int nearestIndex = GetNearestGridIndex(worldPos);
        return IndexToWorldPos(nearestIndex);
    }

    private int GetNearestGridIndex(Vector2 worldPos)
    {
        int bestIndex = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < TotalGridCount; i++)
        {
            Vector2 pt = IndexToWorldPos(i);
            float dist = Vector2.Distance(worldPos, pt);
            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
    #endregion

    #region 图标放置逻辑：放入目标索引，冲突则递归向后挤，到末尾循环回到0
    /// <summary>把UI放到最近网格点，自动处理占位挤压逻辑</summary>
    public void PlaceUI(RectTransform uiRect, Vector2 targetWorldPos)
    {
        int targetIndex = GetNearestGridIndex(targetWorldPos);
        PlaceUIAtIndex(uiRect, targetIndex);
    }

    private void PlaceUIAtIndex(RectTransform ui, int targetIndex)
    {
        if (_gridObjects.TryGetValue(targetIndex, out RectTransform existUi))
        {
            // 当前位置已有物体，取出旧物体，把新UI放进来
            _gridObjects[targetIndex] = ui;
            // 旧物体向后移动一格，循环
            int nextIndex = (targetIndex + 1) % TotalGridCount;
            PlaceUIAtIndex(existUi, nextIndex);
        }
        else
        {
            // 空位，直接放置
            _gridObjects[targetIndex] = ui;
            ui.position = IndexToWorldPos(targetIndex);
        }
    }
    #endregion

    #region 对外接口2：清除【距离该坐标最近网格点】存储的物体
    public void ClearAtPosition(Vector2 worldPos)
    {
        int idx = GetNearestGridIndex(worldPos);
        if (_gridObjects.ContainsKey(idx))
        {
            _gridObjects.Remove(idx);
        }
    }
    #endregion

    #region Gizmos绘制网格点
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoPointColor;

        for (int i = 0; i < TotalGridCount; i++)
        {
            Vector2 pos = IndexToWorldPos(i);
            Gizmos.DrawSphere(pos, gizmoPointSize);
        }
    }
    #endregion

    // 辅助：获取某个索引上当前存放的UI
    public RectTransform GetObjectAtGridIndex(int index)
    {
        _gridObjects.TryGetValue(index, out var rt);
        return rt;
    }
}
