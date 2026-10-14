using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
    
public class Plot_setText : Plot_Base
{
    public TMP_Text targetTMP;
    public string newText;
    [Tooltip("每秒打出字符数量，0=瞬间完整显示")]
    public float animSpeed = 3f;

    protected override IEnumerator PlotCoroutine()
    {
        if (targetTMP == null)
        {
            Debug.LogWarning($"[{name}] targetTMP为空");
            yield break;
        }

        // animSpeed=0：直接完整显示文字
        if (animSpeed <= 0)
        {
            targetTMP.text = newText;
            yield break;
        }

        targetTMP.text = "";
        float delayPerChar = 1f / animSpeed;

        foreach (char c in newText)
        {
            targetTMP.text += c;
            yield return new WaitForSeconds(delayPerChar);
        }
    }
}