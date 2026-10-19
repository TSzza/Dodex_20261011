using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering.Universal;

public class Plot_setActive : Plot_Base
{
    public bool reverse = false;
    public List<GameObject> targetObjects;
    public Light2D setLight;
    public float lightIntensity = 0.2f;

    protected override IEnumerator PlotCoroutine()
    {
        //Debug.Log("SetActive");


        //Debug.Log($"[Plot] PlotCoroutine 开始执行，frame={Time.frameCount}");
        //Debug.Log("Set Active");
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null && obj.GetComponent<DefaultPage_Manager>() != null) // 对于页面的特殊判断
            {
                if(!reverse)
                    CanvasLayer_Manager.Instance.Open(obj.GetComponent<DefaultPage_Manager>().myType);
                else
                {
                    CanvasLayer_Manager.Instance.Close(obj.GetComponent<DefaultPage_Manager>().myType);
                }
            }
            else if (obj != null)
            {
                obj.SetActive(!reverse);
				//Debug.Log((!reverse).ToString()+" "+obj.name);
            }
        }
        if (setLight != null)
        {
            setLight.intensity = lightIntensity;
        }
        //Debug.Log("SetActive1");
        yield return null;
        //Debug.Log("SetActive2");
    }
}