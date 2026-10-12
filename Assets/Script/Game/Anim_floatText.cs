using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Anim_floatText : MonoBehaviour
{
    [Header("动画总时长")]
    public float duration = 1f;
    
    [Header("Y位移曲线：输入t(0~1)，输出Y偏移")]
    public AnimationCurve yCurve;
    [Header("缩放曲线：输入t(0~1)，输出缩放倍数")]
    public AnimationCurve scaleCurve;

    private TextMesh _tmp;
    private Vector3 _originPos;
    private Vector3 _originScale;
    private Color _originColor;
    private bool _isPlaying;
    private float _timer;

    public Color perfect,good,early_late,miss;

    void Awake()
    {
        _tmp = GetComponent<TextMesh>();
        SaveOriginState();
    }

    /// <summary>保存初始位置、缩放、颜色</summary>
    private void SaveOriginState()
    {
        _originPos = transform.position;
        _originScale = transform.localScale;
        _originColor = _tmp.color;
    }

    /// <summary>外部调用，重新播放动画</summary>
    public void Play(Game_Controller.Score score)
    {
        // 重置到初始状态
        transform.position = _originPos;
        transform.localScale = _originScale;
        switch (score)
        {
            case Game_Controller.Score.perfect:
                _tmp.text = "perfect";
                _tmp.color = perfect;
                break;
            case Game_Controller.Score.good:
                _tmp.text = "good";
                _tmp.color = good;
                break;
            case Game_Controller.Score.early:
                _tmp.text = "too early";
                _tmp.color = early_late;
                break;
            case Game_Controller.Score.late:
                _tmp.text = "too late";
                _tmp.color = early_late;
                break;
            case Game_Controller.Score.miss:
                _tmp.text = "miss";
                _tmp.color = miss;
                break;
        }

        _timer = 0f;
        _isPlaying = true;
    }

    void Update()
    {
        if (!_isPlaying) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration);

        // 第一段：0~0.3 淡入（完全透明→不透明）
        float fadeInRange = 0.3f;
        float alpha;
        if (t < fadeInRange)
        {
            alpha = t / fadeInRange;
        }
        else
        {
            // 0.3~1 一边向上飞一边淡出
            float tFadeOut = (t - fadeInRange) / (1f - fadeInRange);
            alpha = 1f - tFadeOut;
        }
        _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, alpha);

        // Y位移 由动画曲线控制
        float yOffset = yCurve.Evaluate(t);
        // 缩放 = 原始缩放 * 曲线值
        float scaleFactor = scaleCurve.Evaluate(t);

        transform.position = _originPos + new Vector3(0, yOffset, 0);
        transform.localScale = _originScale * scaleFactor;

        if (t >= 1f)
        {
            _isPlaying = false;
        }
    }
}
