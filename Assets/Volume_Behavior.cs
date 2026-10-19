using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class Volume_Behavior : MonoBehaviour
{[Header("正弦波动参数")]

    public float speed = 1.0f;        // 正弦变化速度
    public float amplitude = 0.2f;    // 最大色差幅度（Chromatic Aberration范围一般0~1）
    public float offset = 0f;         // 正弦偏移量，可调整基准值

    private Volume _volume;
    private ChromaticAberration _chromaticAberration;

    void Awake()
    {
        _volume = GetComponent<Volume>();
        // 尝试从Volume的Profile读取ChromaticAberration
        if (!_volume.profile.TryGet(out _chromaticAberration))
        {
            Debug.LogWarning("Volume Profile中没有ChromaticAberration Override，请手动添加！");
            enabled = false;
        }
    }

    void Update()
    {
        // 正弦计算：sin(时间*速度) → [-1 ~ +1]，乘以幅度，叠加偏移
        float sinValue = Mathf.Sin(Time.time * speed);
        float finalValue = sinValue * amplitude + offset;
        
        // 限制在0~1区间，色差不支持负数
        finalValue = Mathf.Clamp01(finalValue);
        
        _chromaticAberration.intensity.value = finalValue;
    }
}
