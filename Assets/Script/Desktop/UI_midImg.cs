using UnityEngine;

public class UI_midImg : MonoBehaviour
{
    [Header("宽高比 宽/高，16:9")]
    public float aspectRatio = 16f / 9f;

    private RectTransform _selfRt;
    private RectTransform _parentRt;

    void Awake()
    {
        _selfRt = GetComponent<RectTransform>();
        //锚点强制设为中心，保证居中
        _selfRt.anchorMin = new Vector2(0.5f, 0.5f);
        _selfRt.anchorMax = new Vector2(0.5f, 0.5f);
        _selfRt.pivot = new Vector2(0.5f, 0.5f);
    }

    void Update()
    {
        if (_parentRt == null)
        {
            if (transform.parent == null) return;
            _parentRt = transform.parent.GetComponent<RectTransform>();
            if (_parentRt == null) return;
        }
        UpdateSize();
    }

    void UpdateSize()
    {
        //获取父物体矩形宽高
        Rect parentRect = _parentRt.rect;
        float parentW = parentRect.width;
        float parentH = parentRect.height;

        //按比例计算目标尺寸，FitInside：有一边撑满父物体，另一边小于等于父物体，保持aspectRatio
        float targetW, targetH;
        float testH = parentW / aspectRatio;

        if (testH <= parentH)
        {
            //宽度撑满父物体，高度按比例缩小
            targetW = parentW;
            targetH = testH;
        }
        else
        {
            //高度撑满父物体，宽度按比例缩小
            targetH = parentH;
            targetW = parentH * aspectRatio;
        }

        //设置自身尺寸
        _selfRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);
        _selfRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetH);
        //保持在父物体中心
        _selfRt.anchoredPosition = Vector2.zero;
    }

    //编辑器模式预览
    void OnValidate()
    {
        _selfRt = GetComponent<RectTransform>();
        if (transform.parent != null)
        {
            _parentRt = transform.parent.GetComponent<RectTransform>();
            UpdateSize();
        }
    }
}
