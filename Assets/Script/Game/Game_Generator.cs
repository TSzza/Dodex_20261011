using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

public class Game_Generator : MonoBehaviour
{
    [Header("地图文本")]
    [Multiline(6)] public string mapText = "";

    [Header("网格配置")]
    public int rows = 8;
    public int cols = 8;
    public float gridSpacing = 1f;
    public Vector3 originOffset = Vector3.zero;

    [Header("预制体")]
    public GameObject prefabBlock;
    public GameObject prefabTile;

    [Header("触发开关（Inspector点击用）")]
    public bool create = false;

    /// <summary>
    /// 【1】从mapText解析并实例化子物体，**手动输入模式不执行任何迷宫约束校验**
    /// 忽略空格、Tab、换行；非法字符/字符不足位置填充Tile并输出警告
    /// </summary>
    public void BuildMapFromText()
    {
        // 清理旧子物体
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform t in transform) toDestroy.Add(t);
        foreach (var t in toDestroy) DestroyImmediate(t.gameObject);

        // 过滤：剔除空格、Tab、回车换行等空白字符
        StringBuilder cleanSb = new StringBuilder();
        foreach (char c in mapText)
        {
            if (!char.IsWhiteSpace(c))
                cleanSb.Append(c);
        }
        string cleanStr = cleanSb.ToString();

        char[,] parseGrid = new char[rows, cols];
        bool hasWarning = false;
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (idx >= cleanStr.Length)
                {
                    // 字符数量不足，填充Tile
                    parseGrid[r, c] = 'T';
                    hasWarning = true;
                }
                else
                {
                    char ch = char.ToUpper(cleanStr[idx]);
                    if (ch == 'B' || ch == 'T' || ch == 'S' || ch == 'E')
                    {
                        parseGrid[r, c] = ch;
                    }
                    else
                    {
                        // 非法字符，填充Tile
                        parseGrid[r, c] = 'T';
                        hasWarning = true;
                    }
                    idx++;
                }
            }
        }
        if (hasWarning)
        {
            Debug.LogWarning($"地图文本解析警告：字符不足或者包含非法字符，缺失/非法位置自动填充为Tile");
        }

        // 实例化物体
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = parseGrid[r, c];
                GameObject pf = null;
                if (ch == 'B') pf = prefabBlock;
                if (ch == 'T' || ch == 'S' || ch == 'E') pf = prefabTile;
                if (pf == null) continue;

                Vector3 worldPos = transform.position + originOffset + new Vector3(c * gridSpacing, -r * gridSpacing, 0f);
                GameObject inst = Instantiate(pf, worldPos, Quaternion.identity, transform);
                inst.name = $"{ch}_{r}_{c}";
            }
        }
        Debug.Log("地图生成完成");
    }
    private char[,] _gizmoGrid = new char[100, 100];
    // 解析文本，填充到_gizmoGrid，供Gizmos绘制
    void ParseGridForGizmos()
    {
        StringBuilder cleanSb = new StringBuilder();
        foreach (char c in mapText)
        {
            if (!char.IsWhiteSpace(c))
                cleanSb.Append(c);
        }
        string cleanStr = cleanSb.ToString();
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (idx >= cleanStr.Length)
                {
                    _gizmoGrid[r, c] = 'T';
                }
                else
                {
                    char ch = char.ToUpper(cleanStr[idx]);
                    if (ch == 'B' || ch == 'T' || ch == 'S' || ch == 'E')
                        _gizmoGrid[r, c] = ch;
                    else
                        _gizmoGrid[r, c] = 'T';
                    idx++;
                }
            }
        }
        Debug.Log($"OnDrawGizmos() {rows} {cols}");
    }

    void OnDrawGizmos()
    {
        if (_gizmoGrid == null)
        {
            ParseGridForGizmos();
        }
        float cellSize = gridSpacing;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = _gizmoGrid[r, c];
                // 格子中心坐标，和实例化位置完全对齐
                Vector3 center = transform.position + originOffset + new Vector3(c * cellSize, -r * cellSize, 0f);

                // 设置颜色
                switch (ch)
                {
                    case 'B': Gizmos.color = Color.red; break;
                    case 'T': Gizmos.color = Color.white; break;
                    case 'S': Gizmos.color = Color.green; break;
                    case 'E': Gizmos.color = Color.blue; break;
                    default: Gizmos.color = Color.gray; break;
                }
                // 绘制线框方块
                Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
            }
        }
    }
}

