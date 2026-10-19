using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class Plot_Base : MonoBehaviour
{
    public Plot_Editor parentPlot_Editor;

    [Tooltip("是否正在执行剧情协程")]
    public bool isPlaying;
    public bool hasPlayed;
    public bool defaultTransparent = true;

    private Coroutine _plotCoroutine;
    
    public void Play()
    {
        //Debug.Log(isPlaying.ToString());
        if (isPlaying)
        {
            Debug.LogWarning($"[{name}] 剧情正在运行，忽略本次Play调用");
            return;
        }
        //Debug.LogWarning($"[{name}] 开始运行");
        _plotCoroutine = StartCoroutine(PlotCoroutineWrapper());
    }


    // --------------------------------------------
    private void Awake()
    {
        Plot_Base[] components = GetComponents<Plot_Base>();
        if (components.Length > 1)
        {
            Debug.LogWarning($"[{name}] 挂载了多个Plot_Base组件");
        }
    }
    protected virtual void Start()
    {

        if (transform.parent == null)
        {
            Debug.LogError($"[{name}] No parent");
        }
        if (transform.parent.GetComponent<Plot_Editor>() == null)
        {
            Debug.LogWarning($"[{name}] No parent Plot editor");
        }

        Transform current = transform.parent;
        while (current != null)
        {
            if (current.TryGetComponent<Plot_Editor>(out Plot_Editor editor))
            {
                parentPlot_Editor = editor;
                break;
            }
            current = current.parent; // 继续往上一层
        }
        if (parentPlot_Editor == null)
        {
            Debug.LogError($"[{name}] Plot editor Not Found");
        }




        if (defaultTransparent)
        {
            Image img = GetComponent<Image>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        }
        // 不透明
        hasPlayed = false;
    }
    
    protected virtual IEnumerator PlotCoroutine() // 剧情协程，子类重写
    {
        Debug.LogWarning("Default Plot Coroutine");
        yield return null;
    }

    protected virtual void End()
    {
        isPlaying = false;
        //StopCoroutine(_plotCoroutine);
        _plotCoroutine=null;

        if (defaultTransparent)
        {
            Image img = GetComponent<Image>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0.2f);
        }
        // 透明
        hasPlayed = true;

        if (parentPlot_Editor != null)
            parentPlot_Editor.PlayNext(); // 触发下一个
    }

    // 协程包装
    private IEnumerator PlotCoroutineWrapper()
    {
        // 临时新建一个迭代器实例，避免复用已被耗尽的 IEnumerator
        IEnumerator plot = PlotCoroutine();

        //Debug.Log($"[Plot] Wrapper 进入，isPlaying={isPlaying}，frame={Time.frameCount}，" +
        //          $"plot 实例哈希={plot.GetHashCode()}，plot 类型={plot.GetType().Name}");

        isPlaying = true;
        //Debug.Log($"[Plot] isPlaying 已设为 true，准备 yield plot，frame={Time.frameCount}");

        yield return plot;

        //Debug.Log($"[Plot] plot 已结束，准备调用 End()，frame={Time.frameCount}");
        End();
    }

    // 强制停止当前剧情
    public void StopPlot()
    {
        if (_plotCoroutine != null)
        {
            StopCoroutine(_plotCoroutine);
            _plotCoroutine = null;
        }
        isPlaying = false;
    }
}
