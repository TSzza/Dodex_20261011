using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_startPlots : Plot_Base
{
    public List<Plot_Editor> plotEditors = new List<Plot_Editor>();

    [Header("Gizmos")]
    public bool drawGizmos = true;
    private Color lineColor = new Color(1f, 1f, 0.6f);
    private Color closePointCircleColor = Color.red;
    public bool StartAtStart = false;
    
    protected override void Start()
    {
        base.Start();
        if (StartAtStart) // 开局自启动
            Play();
    }
    protected override IEnumerator PlotCoroutine()
    {
        if (StartAtStart)
            yield return null; // 先等待第一轮Start完成
        foreach (var editor in plotEditors)
        {
            if (editor != null)
            {
                editor.ReRun();
            }
            else
            {
                Debug.LogWarning($"[{name}] 列表内存在空的Plot_Editor");
            }
        }
        yield return null;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        foreach (var editor in plotEditors)
        {
            if (editor == null) continue;

            List<Plot_Base> validPlots = new List<Plot_Base>();
            editor.RecursiveGizmoCollect(editor.transform, validPlots);

            if (validPlots.Count == 0) continue;
            Plot_Base firstTargetPlot = validPlots[0];

            Vector2 fromPos = transform.position;
            Vector2 toPos = firstTargetPlot.transform.position;
            float dist = Vector2.Distance(fromPos, toPos);

            // 取用当前PlotEditor身上的Gizmos参数：minDistanceThreshold、arrowHeadSize、lineThickness
            if (dist < editor.minDistanceThreshold)
            {
                Gizmos.color = closePointCircleColor;
                Vector3 mid = (Vector3)((fromPos + toPos) * 0.5f);
                Gizmos.DrawWireSphere(mid, Mathf.Max(editor.arrowHeadSize, 0.5f));
                continue;
            }

            Gizmos.color = lineColor;
            editor.DrawThickArrowGizmo(fromPos, toPos, editor.arrowHeadSize, editor.lineThickness);
        }
    }
}
