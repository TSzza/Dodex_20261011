using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class Plot_Base : MonoBehaviour
{
    private Plot_Editor parentPlot_Editor;

    [Tooltip("是否正在执行剧情协程")]
    public bool isPlaying { get; private set; }
    public bool hasPlayed;

    private Coroutine _plotCoroutine;
    
    public void Play()
    {
        if (isPlaying)
        {
            Debug.LogWarning($"[{name}] 剧情正在运行，忽略本次Play调用");
            return;
        }

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
        if (!transform.parent.TryGetComponent<Plot_Editor>(out parentPlot_Editor))
        {
            parentPlot_Editor = null;
        }

        Image img = GetComponent<Image>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
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
        _plotCoroutine = null;
        
        Image img = GetComponent<Image>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0.2f);
        // 透明
        hasPlayed = true;

        if (parentPlot_Editor != null)
            parentPlot_Editor.PlayNext(); // 触发下一个
    }

    // 协程包装
    private IEnumerator PlotCoroutineWrapper()
    {
        isPlaying = true;
        yield return PlotCoroutine();
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
