using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class UI_simpleLight2DShake : MonoBehaviour
{
    [Header("===== 闪烁间隔 =====")]
    [Tooltip("最短间隔时间（秒）")]
    public bool On;
    public float minInterval = 0.1f;

    [Tooltip("最长间隔时间（秒）")]
    public float maxInterval = 0.3f;

    [Header("===== 灯泡亮度 =====")]
    [Tooltip("正常亮度")]
    public float normalIntensity = 1f;

    [Tooltip("闪烁瞬间的最低亮度，建议接近0")]
    public float flickerIntensity = 0.05f;

    [Tooltip("是否随机轻微亮度波动，模拟电流不稳")]
    public bool randomIntensityShift = true;

    [Header("===== 闪烁类型 =====")]
    [Tooltip("true = 随机间隔闪烁；false = 近似规律间隔")]
    public bool randomTiming = true;

    [Tooltip("是否偶尔出现短暂熄灭，模拟灯泡接触不良")]
    public bool blackoutSpikes = true;

    [Range(0f, 1f)]
    [Tooltip("出现短暂熄灭的概率")]
    public float blackoutChance = 0.15f;

    [Header("===== 调试 =====")]
    public bool showDebugInfo = false;

    private Light2D _targetLight2D;
    private float _timer;
    private float _nextInterval;
    private bool _isFlickering;

    void Start()
    {
        _targetLight2D = GetComponent<Light2D>();
        _nextInterval = GetRandomInterval();
    }

    void Update()
    {
        if (!On) return;

        _timer += Time.deltaTime;

        if (_timer >= _nextInterval)
        {
            _timer = 0f;
            _nextInterval = GetRandomInterval();
            DoFlicker();
        }

        if (randomIntensityShift && !_isFlickering)
        {
            // 模拟电流不稳的微小浮动
            _targetLight2D.intensity = Mathf.Lerp(
                _targetLight2D.intensity,
                normalIntensity + Random.Range(-0.04f, 0.04f),
                Time.deltaTime * 6f
            );
        }
    }

    private void DoFlicker()
    {
        _isFlickering = true;

        if (blackoutSpikes && Random.value < blackoutChance)
        {
            // 短暂熄灭，制造灯泡接触不良感
            _targetLight2D.intensity = 0f;
        }
        else
        {
            // 普通忽明忽暗
            _targetLight2D.intensity = flickerIntensity + Random.Range(0f, 0.2f);
        }

        // 很快恢复，但不是完全顺滑
        Invoke(nameof(RecoverLight2D), Random.Range(0.02f, 0.08f));
    }

    private void RecoverLight2D()
    {
        if (_targetLight2D != null)
        {
            _targetLight2D.intensity = normalIntensity + Random.Range(-0.05f, 0.1f);
        }

        _isFlickering = false;
    }

    private float GetRandomInterval()
    {
        if (randomTiming)
        {
            return Random.Range(minInterval, maxInterval);
        }
        else
        {
            // 近似规律，但仍带一点偏差
            return Mathf.Lerp(minInterval, maxInterval, 0.5f) + Random.Range(-0.02f, 0.02f);
        }
    }
}
