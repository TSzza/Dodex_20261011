using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

[SelectionBase]
public class Plot_setTransparent : Plot_Base
{
    public float animTime = 0.5f;
    [Range(0f,1f)] public float targetTransparent;
    public List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    public List<TMP_Text> tmpTexts = new List<TMP_Text>();
    public List<Image> images = new List<Image>();
    public List<Light2D> Lights = new List<Light2D>();
    // 缓存每个对象初始alpha / 灯光初始亮度，以初始亮度=1作为参考基准，0=熄灭
    private Dictionary<Object, float> _originAlphaDict = new Dictionary<Object, float>();

    protected override IEnumerator PlotCoroutine()
    {
        //Debug.Log(animTime);

        _originAlphaDict.Clear();
        // 读取初始透明度
        foreach(var spr in spriteRenderers)
        {
            if(spr != null)
                _originAlphaDict[spr] = spr.color.a;
        }
        foreach(var tmp in tmpTexts)
        {
            if(tmp != null)
                _originAlphaDict[tmp] = tmp.color.a;
        }
        foreach(var img in images)
        {
            if(img != null)
                _originAlphaDict[img] = img.color.a;
        }
        // 记录灯光基准：保存原始亮度，映射为参考1
        foreach(var lig in Lights)
        {
            if(lig != null)
            {
                _originAlphaDict[lig] = lig.intensity;
                if (lig.GetComponent<UI_simpleLight2DShake>() != null)
                {
                    lig.GetComponent<UI_simpleLight2DShake>().On = false;
                }
            }
        }
        //Debug.Log("AA "+animTime);

        float timer = 0f;
        while(timer < animTime)
        {
            
        //Debug.Log("BB "+_originAlphaDict.Count);


            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);
            // 更新所有对象
            foreach(var kvp in _originAlphaDict)
            {
                float originValue = kvp.Value;
                float currentValue = Mathf.Lerp(originValue, targetTransparent, t);

                if(kvp.Key is SpriteRenderer spr)
                {

                    Color c = spr.color;
                    c.a = currentValue;
                    spr.color = c;
                    //Debug.Log("C " + c.a+" "+originValue+" "+);
                }
                else if(kvp.Key is TextMeshProUGUI tmp)
                {
                    Color c = tmp.color;
                    c.a = currentValue;
                    tmp.color = c;
                }
                else if(kvp.Key is Image img)
                {
                    Color c = img.color;
                    c.a = currentValue;
                    img.color = c;
                    //Debug.Log("D " + c.a);
                }
                else if(kvp.Key is Light2D lt)
                {
                    lt.intensity = currentValue;
                }
            }
            yield return null;
        }
        // 设置最终透明度
        foreach(var kvp in _originAlphaDict)
        {
            float originValue = kvp.Value;
            float finalValue =  targetTransparent;

            if(kvp.Key is SpriteRenderer spr)
            {
                Color c = spr.color;
                c.a = finalValue;
                spr.color = c;
            }
            else if(kvp.Key is TextMeshProUGUI tmp)
            {
                Color c = tmp.color;
                c.a = finalValue;
                tmp.color = c;
            }
            else if(kvp.Key is Image img)
            {
                Color c = img.color;
                c.a = finalValue;
                img.color = c;
            }
            else if(kvp.Key is Light2D lt)
            {
                lt.intensity = finalValue;
            }
        }
    }
}
