using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anim_BeatPalse : MonoBehaviour
{
    [Header("4个路径控制点")]
    public GameObject pos1;
    public GameObject pos2;
    public GameObject pos3;
    public GameObject pos4;

    [Header("目标物体（修改它的X）")]
    public GameObject targetObj;

    /// <summary>
    /// 输入value [0~4]，沿折线插值，设置targetObj的X坐标
    /// 分段区间：
    /// 0   ~ 0.5   : pos1 → pos2
    /// 0.5 ~ 3.5   : pos2 → pos3
    /// 3.5 ~ 4.0   : pos3 → pos4
    /// </summary>
    public float palseDelay = 0.25f;
    public void SetByValue(float value)
    {
        // 钳位输入，防止越界
        float v = (value + palseDelay) % 4f;
        float outX = 0f;

        if (v <= 0.5f)
        {
            // 0~0.5：pos1 -> pos2
            float t = Mathf.InverseLerp(0f, 0.5f, v);
            outX = Mathf.Lerp(pos1.transform.position.x, pos2.transform.position.x, t);
        }
        else if (v <= 3.5f)
        {
            // 0.5~3.5：pos2 -> pos3
            float t = Mathf.InverseLerp(0.5f, 3.5f, v);
            outX = Mathf.Lerp(pos2.transform.position.x, pos3.transform.position.x, t);
        }
        else
        {
            // 3.5~4.0：pos3 -> pos4
            float t = Mathf.InverseLerp(3.5f, 4f, v);
            outX = Mathf.Lerp(pos3.transform.position.x, pos4.transform.position.x, t);
        }

        // 修改目标物体X，Y、Z保持不变
        Vector3 targetPos = targetObj.transform.position;
        targetPos.x = outX;
        targetObj.transform.position = targetPos;
    }
}

