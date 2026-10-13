using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_changeScale : Plot_Base
{
    public GameObject targetObject;
    public float targetScale;
    public float animTime = 1f;

    private Vector3 _originScale;

    protected override IEnumerator PlotCoroutine()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[{name}] targetObject为空");
            yield break;
        }

        // 记录原始缩放
        _originScale = targetObject.transform.localScale;
        float timer = 0f;

        while (timer < animTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);

            // 线性插值X，Y、Z
            float newX = Mathf.Lerp(_originScale.x, targetScale, t);
            float ratio = newX / _originScale.x;

            targetObject.transform.localScale = new Vector3(newX, _originScale.y * ratio, _originScale.z * ratio);
            yield return null;
        }

        // 最终
        float finalRatio = targetScale / _originScale.x;
        targetObject.transform.localScale = new Vector3(targetScale, _originScale.y * finalRatio, _originScale.z * finalRatio);
    }
}
