using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_delaySet : MonoBehaviour
{
    public float rangeMin = -200f;
    public float rangeMax = 200f;
    public float step = 1f;

    public TMP_Text text;
    public Slider _slider;
    void Start()
    {
        Upload();
    }

    private void Upload() // 根据Root读到的数据反过来更改Scroll位置、text的text
    {
        float val = Root.Instance.DelaySet;
        int i = Mathf.RoundToInt(val);
        _slider.value = MsToScrollValue(val); 
        text.text = $"延迟调整：{i} ms";
    }
    private float ScrollValueToMs(float slider01)
    {
        return Mathf.Lerp(rangeMin, rangeMax, slider01);
    }
    private float MsToScrollValue(float ms)
    {
        float t = Mathf.InverseLerp(rangeMin, rangeMax, ms);
        return Mathf.Clamp01(t);
    }

    // 滑块拖动时触发
    public void OnScrollValueChanged()
    {
        float value01 = _slider.value;
        float val = ScrollValueToMs(value01);
        Root.Instance.DelaySet = val;
        int i = Mathf.RoundToInt(val);
        text.text = $"延迟调整：{i} ms";
    }

    /// <summary>对外：增加一步</summary>
    public void AddStep()
    {
        Root.Instance.DelaySet += step;
        Root.Instance.DelaySet = Mathf.Clamp(Root.Instance.DelaySet, rangeMin, rangeMax);
        Upload();
    }

    /// <summary>对外：减少一步</summary>
    public void SubStep()
    {
        Root.Instance.DelaySet -= step;
        Root.Instance.DelaySet = Mathf.Clamp(Root.Instance.DelaySet, rangeMin, rangeMax);
        Upload();
    }
}
