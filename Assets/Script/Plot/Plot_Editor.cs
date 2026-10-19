using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_Editor : MonoBehaviour
{
    public bool Lazy = false;
    public List<Plot_Base> plotNodes = new List<Plot_Base>();
    [Tooltip("当前执行索引")]
    public int Index = 0;

    private void Start()
    {
        CollectAllChildPlot();
        //RunPlay(); // 启动第一个节点（实际是Play按钮启动）
    }

    [ContextMenu("收集子Plot节点 Set Index 为0")]
    public void CollectAllChildPlot()
    {
        plotNodes.Clear();
        RecursiveCollectPlot(transform);
        Index = 0;
    }

    // 递归遍历子孙物体，只收集Active且带Plot_Base的物体，保持Hierarchy顺序
    private void RecursiveCollectPlot(Transform t)
    {
        foreach (Transform child in t)
        {
            // 跳过未激活物体
            if (!child.gameObject.activeSelf)
                continue;

            Plot_Base plot = child.GetComponent<Plot_Base>();
            if (plot != null)
            {
                plotNodes.Add(plot);
            }
            // 继续递归更深层级
            RecursiveCollectPlot(child);
        }
    }
    public void ReRun() // 中止进程并从头开跑
    {
        if (plotNodes == null || plotNodes.Count == 0)
        {
            Debug.LogWarning($"[{name}] ReRun：plotNodes列表为空，初始化错误？");
            return;
        }

        if (Index >= 0 && Index < plotNodes.Count && plotNodes[Index].isPlaying)
        {
            plotNodes[Index].StopPlot();
            Debug.LogWarning($"[{name}] 剧情序列出现强制重启现象");
        }

        Index = 0;
        RunPlay();
    }

    // 执行当前Play
    private void RunPlay()
    {
        if (plotNodes == null || plotNodes.Count == 0)
        {
            Debug.LogWarning($"[{name}] 没有可用Plot节点");
            return;
        }
        if (Index >= plotNodes.Count)
        {
            Debug.Log($"[{name}] 剧情序列全部播放完成");

            Index = 0; // 重要！！这样就可以重播了

            return;
        }
        Plot_Base targetNode = plotNodes[Index];
        if (targetNode != null)
        {
            targetNode.Play();
        }
        else
        {
            Debug.LogWarning($"[{name}] 当前索引{Index}的Plot节点为空");
        }
    }

    public void PlayNext()
    {
        if (!Lazy)
        {
            Index++;
            RunPlay();
        }
    }

    // ---------------------- 视觉处理 ---------------------- //
    [Header("Gizmos总开关")]
    public bool drawGizmos = true;
    [Header("连线参数")]
    public float lineThickness = 10f;
    public float arrowHeadSize = 1f;
    public float minDistanceThreshold = 0.5f;
    [Header("颜色")]
    public Color lineColor = Color.green;
    public Color closePointCircleColor = Color.red;

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        List<Plot_Base> validPlots = new List<Plot_Base>();
        RecursiveGizmoCollect(transform, validPlots);

        if (validPlots.Count < 2)
            return;

        for (int i = 0; i < validPlots.Count - 1; i++)
        {
            Transform fromTf = validPlots[i].transform;
            Transform toTf = validPlots[i + 1].transform;

            Vector2 fromPos = fromTf.position;
            Vector2 toPos = toTf.position;
            float dist = Vector2.Distance(fromPos, toPos);

            if (dist < minDistanceThreshold)
            {
                Gizmos.color = closePointCircleColor;
                Vector3 mid = (Vector3)((fromPos + toPos) * 0.5f);
                Gizmos.DrawWireSphere(mid, Mathf.Max(arrowHeadSize, 0.5f));
                continue;
            }

            Gizmos.color = lineColor;
            DrawThickArrowGizmo(fromPos, toPos, arrowHeadSize, lineThickness);
        }
    }

    // Gizmos递归采集
    public void RecursiveGizmoCollect(Transform t, List<Plot_Base> result)
    {
        foreach (Transform child in t)
        {
            if (!child.gameObject.activeSelf)
                continue;

            Plot_Base plot = child.GetComponent<Plot_Base>();
            if (plot != null)
            {
                result.Add(plot);
            }
            RecursiveGizmoCollect(child, result);
        }
    }
    public void DrawThickArrowGizmo(Vector2 start, Vector2 end, float headSize, float thickness)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 side = new Vector2(-dir.y, dir.x);
        Vector2 halfSide = side * thickness * 0.5f;
        // 箭头根部位置
        Vector2 shaftEnd = end - dir * headSize;
        // ---------- 主体粗线（平行四边形） ----------
        Vector2 p0 = start - halfSide;
        Vector2 p1 = start + halfSide;
        Vector2 p2 = shaftEnd + halfSide;
        Vector2 p3 = shaftEnd - halfSide;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
        // ---------- 箭头三角形（完整闭合） ----------
        Vector2 triA = shaftEnd;                       // 三角形底边中点
        Vector2 triB = triA + side * headSize * 0.6f;  // 底边一侧
        Vector2 triC = triA - side * headSize * 0.6f;  // 底边另一侧
        Vector2 tip  = end;                            // 尖端
        Gizmos.DrawLine(triA, triB);   // 底边
        Gizmos.DrawLine(triA, triC);
        Gizmos.DrawLine(triB, triC);
        Gizmos.DrawLine(triB, tip);    // 两条斜边
        Gizmos.DrawLine(triC, tip);
    }
}
