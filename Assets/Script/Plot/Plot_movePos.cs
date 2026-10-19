using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimType
{
    linear,
    easeIn,
    easeOut
}

public class Plot_movePos : Plot_Base
{
    public List<GameObject> targetObjects = new List<GameObject>();
    public AnimType animType;
    public float animTime = 1f;

    [Header("移动模式")]
    public bool useOffset = true;      // true: 用 startOffset/endOffset 偏移；false: 用 endPos 绝对坐标
    public Vector2 startOffset;
    public Vector2 endOffset;
    public Vector2 endPos;

    private Dictionary<GameObject, Vector3> _originPosDict = new Dictionary<GameObject, Vector3>();

    protected override IEnumerator PlotCoroutine()
    {
        // 清空并缓存每个物体的原始位置
        _originPosDict.Clear();
        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;
            _originPosDict[obj] = obj.transform.position;
        }

        // 预计算每个物体的起点/终点，避免循环里反复算
        var startDict = new Dictionary<GameObject, Vector3>();
        var endDict = new Dictionary<GameObject, Vector3>();

        foreach (var kvp in _originPosDict)
        {
            GameObject obj = kvp.Key;
            Vector3 origin = kvp.Value;

            Vector3 startWorld;
            Vector3 endWorld;

            if (useOffset)
            {
                // 偏移模式：基于各自原点加偏移
                startWorld = origin + new Vector3(startOffset.x, startOffset.y, 0f);
                endWorld = origin + new Vector3(endOffset.x, endOffset.y, 0f);
            }
            else
            {
                // 绝对坐标模式：起点用各自原点（或当前位置），终点统一到 endPos
                // 这里起点用 origin，保证从“缓存时的位置”开始；若想从当前实时位置开始，把 origin 换成 obj.transform.position
                startWorld = origin;
                endWorld = new Vector3(endPos.x, endPos.y, origin.z);
            }

            startDict[obj] = startWorld;
            endDict[obj] = endWorld;
        }

        float timer = 0f;
        while (timer < animTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);
            float easedT = GetEaseValue(t, animType);

            foreach (var kvp in startDict)
            {
                GameObject obj = kvp.Key;
                if (obj == null) continue;

                obj.transform.position = Vector3.Lerp(kvp.Value, endDict[obj], easedT);
            }
            yield return null;
        }

        // 最后一帧，确保精确落到终点
        foreach (var kvp in endDict)
        {
            GameObject obj = kvp.Key;
            if (obj == null) continue;
            obj.transform.position = kvp.Value;
        }
    }

    // 缓动函数实现
    private float GetEaseValue(float t, AnimType type)
    {
        switch (type)
        {
            case AnimType.linear:
                return t;
            case AnimType.easeIn:
                return t * t;
            case AnimType.easeOut:
                return 1f - (1f - t) * (1f - t);
            default:
                return t;
        }
    }
}