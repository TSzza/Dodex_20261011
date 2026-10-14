using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_hideButton : MonoBehaviour
{
    [Header("需要控制显隐的物体列表")]
    public List<GameObject> targetObjects;

    [Header("当前是否显示")]
    public bool isShow = true;

    // 按钮点击回调
    public void OnClick()
    {
        // 翻转状态
        isShow = !isShow;

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isShow);
            }
        }
    }
}
