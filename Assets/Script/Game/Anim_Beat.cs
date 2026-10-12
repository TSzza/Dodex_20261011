using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anim_Beat : MonoBehaviour
{
    [Header("动画参数")]
    [Tooltip("统一动画速度，越大播放越快")]
    public float Anim_beat = 1f;
    public float animDuration = 1f;

    [Header("动画曲线")]
    private float OriXY;
    private float OriY;
    public AnimationCurve curveScaleY;
    public AnimationCurve curveHeight;
    public AnimationCurve curveColorT;

    [Header("颜色范围")]
    public Color colorMin = new Color(0.25f,0.25f,0.25f); // 暗灰色（最低点）
    public Color colorMax = new Color(1f,1f,0f);         // 亮黄色（最高点）

    private SpriteRenderer _spriteRenderer;
    private Vector3 _originLocalPos;
    private Vector3 _originScale;
    private bool _isPlaying;
    private float _timer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originLocalPos = transform.localPosition;
        _originScale = transform.localScale;
        OriY = GetComponent<SpriteRenderer>().size.y;
        OriXY = GetComponent<SpriteRenderer>().size.x * OriY;
    }

    void Update()
    {
        if (!_isPlaying)
        {
            //Play();//////////////////////////////
            return;
        }

        _timer += Time.deltaTime * Anim_beat;
        float t = Mathf.Clamp01(_timer / animDuration);

        // 读取4条曲线
        float sY = curveScaleY.Evaluate(t)*OriY;
        float sX = OriXY / sY;
        float height = curveHeight.Evaluate(t);
        float colorT = curveColorT.Evaluate(t);

        // 应用缩放
        GetComponent<SpriteRenderer>().size = new Vector2(sX, sY);
        // 高度偏移
        transform.localPosition = new Vector3(_originLocalPos.x, _originLocalPos.y + height, _originLocalPos.z);
        // 颜色插值
        _spriteRenderer.color = Color.Lerp(colorMin, colorMax, colorT);

        // 动画结束
        if (t >= 1f)
        {
            _isPlaying = false;
            // 重置回初始状态
            ResetState();
        }
    }

    /// <summary>
    /// 外部调用，从头播放一遍完整动画
    /// </summary>
    public void Play()
    {
        _timer = 0f;
        _isPlaying = true;
        //Debug.Log("Play");
    }

    void ResetState()
    {
        transform.localScale = _originScale;
        transform.localPosition = _originLocalPos;
        _spriteRenderer.color = colorMin;
    }

    // 编辑器重置默认曲线预设
    void Reset()
    {
        curveScaleY = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.2f, 1.4f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.8f, 0.8f),
            new Keyframe(1f, 1f)
        );
        // curveHeight：弹跳高度，抛物线
        curveHeight = new AnimationCurve(
            new Keyframe(0f,0f),
            new Keyframe(0.5f, 2f),
            new Keyframe(1f,0f)
        );
        // curveColorT：和高度联动，最高点=1亮黄，最低点=0暗灰
        curveColorT = new AnimationCurve(
            new Keyframe(0f,0f),
            new Keyframe(0.5f,1f),
            new Keyframe(1f,0f)
        );
    }
}
