using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_simpleShake : MonoBehaviour
{
    [Header("===== 总开关与状态 =====")]
    [Tooltip("是否自动启动一轮抖动周期")]
    public bool autoStartCycle = false;
    [Tooltip("当前是否正在运行抖动周期")]
    public bool isRunning = false;

    [Header("===== 时间参数（带随机范围） =====")]
    [Tooltip("安静待机时长 最小")]
    public float idleMin = 3f;
    [Tooltip("安静待机时长 最大")]
    public float idleMax = 6f;
    [Tooltip("抖动渐强阶段时长 最小")]
    public float rampUpMin = 2f;
    [Tooltip("抖动渐强阶段时长 最大")]
    public float rampUpMax = 3f;

    [Header("===== 幅度参数 =====")]
    [Tooltip("抖动最大位移幅度")]
    public Vector3 maxShakeAmplitude = new Vector3(0.12f, 0.12f, 0f);
    [Tooltip("抖动旋转最大角度幅度(欧拉角)")]
    public Vector3 maxRotationAmplitude = new Vector3(2f, 2f, 0f);
    [Tooltip("抖动振动频率，越高抖动越快")]
    public float shakeFrequency = 18f;

    [Header("===== 曲线控制 =====")]
    [Tooltip("强度增长曲线，大于1前期慢后期猛")]
    [Range(0.2f, 3f)] public float strengthCurvePower = 1.8f;

    [Header("===== 相位与随机种子 =====")]
    [Tooltip("每轮启动时自动随机相位偏移，避免每次抖动图案一样")]
    public bool randomPhaseEachCycle = true;
    [Tooltip("手动固定相位偏移，关闭随机时生效")]
    public float fixedPhaseOffset = 0f;

    [Header("===== 重置选项 =====")]
    [Tooltip("抖动结束后是否还原物体原始位置与旋转")]
    public bool resetTransformAfterShake = true;

    // 内部缓存
    private Vector3 _originPos;
    private Quaternion _originRot;
    private float _cycleTimer;
    private float _currentShakeStrength;
    private float _phaseOffset;
    private float _idleDuration;
    private float _rampUpDuration;

    void Start()
    {
        SaveOriginTransform();
        if (autoStartCycle) StartShakeCycle();
    }

    void Update()
    {
        if (!isRunning) {
            StartShakeCycle();
                }
        _cycleTimer += Time.deltaTime;

        if (_cycleTimer < _idleDuration)
        {
            _currentShakeStrength = 0f;
        }
        else
        {
            float t = (_cycleTimer - _idleDuration) / _rampUpDuration;
            if (t >= 1f)
            {
                StopShakeCycle();
                return;
            }
            float rawSin = Mathf.Sin(t * Mathf.PI * 0.5f);
            _currentShakeStrength = Mathf.Pow(rawSin, strengthCurvePower);
        }

        ApplyShake();
    }

    /// <summary>
    /// 启动完整一轮【安静→渐强抖动→瞬间消失】周期
    /// </summary>
    [ContextMenu("Start Shake Cycle")]
    public void StartShakeCycle()
    {
        SaveOriginTransform();
        isRunning = true;
        _cycleTimer = 0f;
        _currentShakeStrength = 0f;

        // 每轮随机待机、渐长时间
        _idleDuration = Random.Range(idleMin, idleMax);
        _rampUpDuration = Random.Range(rampUpMin, rampUpMax);

        // 相位：每轮随机偏移Perlin噪声采样起点，抖动纹理不重复
        if(randomPhaseEachCycle)
        {
            _phaseOffset = Random.Range(0f, 1000f);
        }
        else
        {
            _phaseOffset = fixedPhaseOffset;
        }
    }

    /// <summary>
    /// 立刻终止抖动，恢复原始状态
    /// </summary>
    [ContextMenu("Stop Shake Cycle")]
    public void StopShakeCycle()
    {
        isRunning = false;
        _currentShakeStrength = 0f;
        if (resetTransformAfterShake)
        {
            transform.localPosition = _originPos;
            transform.localRotation = _originRot;
        }
    }

    void ApplyShake()
    {
        if (_currentShakeStrength <= 0f) return;

        // 相位叠加到噪声采样，每轮抖动图案不同
        float baseTime = Time.time * shakeFrequency + _phaseOffset;

        float noiseX = Mathf.PerlinNoise(baseTime, 1f);
        float noiseY = Mathf.PerlinNoise(baseTime, 2f);
        float noiseZ = Mathf.PerlinNoise(baseTime, 3f);

        Vector3 offsetPos = new Vector3(
            (noiseX - 0.5f) * maxShakeAmplitude.x * _currentShakeStrength,
            (noiseY - 0.5f) * maxShakeAmplitude.y * _currentShakeStrength,
            (noiseZ - 0.5f) * maxShakeAmplitude.z * _currentShakeStrength
        );

        Vector3 offsetRot = new Vector3(
            (noiseX - 0.5f) * maxRotationAmplitude.x * _currentShakeStrength,
            (noiseY - 0.5f) * maxRotationAmplitude.y * _currentShakeStrength,
            (noiseZ - 0.5f) * maxRotationAmplitude.z * _currentShakeStrength
        );

        transform.localPosition = _originPos + offsetPos;
        transform.localRotation = _originRot * Quaternion.Euler(offsetRot);
    }

    void SaveOriginTransform()
    {
        _originPos = transform.localPosition;
        _originRot = transform.localRotation;
    }
}
