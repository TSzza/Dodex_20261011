using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Plot_setTransparent : Plot_Base
{
    public float animTime = 0.5f;
    [Range(0f,1f)] public float targetTransparent;

    public List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    public List<TMP_Text> tmpTexts = new List<TMP_Text>();
    public List<Image> images = new List<Image>();

    // 缓存每个对象初始alpha
    private Dictionary<Object, float> _originAlphaDict = new Dictionary<Object, float>();

    protected override IEnumerator PlotCoroutine()
    {
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

        float timer = 0f;
        while(timer < animTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);

            // 更新所有对象
            foreach(var kvp in _originAlphaDict)
            {
                float a = Mathf.Lerp(kvp.Value, targetTransparent, t);
                if(kvp.Key is SpriteRenderer spr)
                {
                    Color c = spr.color;
                    c.a = a;
                    spr.color = c;
                }
                else if(kvp.Key is TextMeshProUGUI tmp)
                {
                    Color c = tmp.color;
                    c.a = a;
                    tmp.color = c;
                }
                else if(kvp.Key is Image img)
                {
                    Color c = img.color;
                    c.a = a;
                    img.color = c;
                }
            }
            yield return null;
        }

        // 设置最终透明度
        foreach(var kvp in _originAlphaDict)
        {
            if(kvp.Key is SpriteRenderer spr)
            {
                Color c = spr.color;
                c.a = targetTransparent;
                spr.color = c;
            }
            else if(kvp.Key is TextMeshProUGUI tmp)
            {
                Color c = tmp.color;
                c.a = targetTransparent;
                tmp.color = c;
            }
            else if(kvp.Key is Image img)
            {
                Color c = img.color;
                c.a = targetTransparent;
                img.color = c;
            }
        }
    }
}
