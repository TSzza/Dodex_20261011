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

    public Vector2 startOffset;
    public Vector2 endOffset;

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

        float timer = 0f;
        while (timer < animTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);
            float easedT = GetEaseValue(t, animType);

            foreach (var kvp in _originPosDict)
            {
                GameObject obj = kvp.Key;
                Vector3 origin = kvp.Value;

                Vector3 startWorld = origin + new Vector3(startOffset.x, startOffset.y, 0f);
                Vector3 endWorld = origin + new Vector3(endOffset.x, endOffset.y, 0f);

                obj.transform.position = Vector3.Lerp(startWorld, endWorld, easedT);
            }
            yield return null;
        }

        // 最后一帧
        foreach (var kvp in _originPosDict)
        {
            GameObject obj = kvp.Key;
            Vector3 origin = kvp.Value;
            Vector3 endWorld = origin + new Vector3(endOffset.x, endOffset.y, 0f);
            obj.transform.position = endWorld;
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
