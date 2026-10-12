using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beat_Manager : MonoBehaviour
{
    [Header("节拍设置")]
    [Tooltip("BPM，每分钟拍数，可调节")]
    public float bpm = 90f;
    [Header("4个节拍物体，按顺序轮流触发")]
    public GameObject[] beatTargets = new GameObject[4];
    public Anim_BeatPalse Palse;

    // currentBeat：**已经叠加玩家DelaySet偏移后的拍数**，判定和音符共用
    public float currentBeat { get; private set; }
    private float _secondsPerBeat;
    private int _lastTriggerBeat = -1;
    private int _currentTargetIndex = 0;
    private float _pendingFireBeat = float.MaxValue;

    // 缓存上一帧的真实时间，用来增量计算
    private float _realTimeAcc;

    void RecalcBeatInterval()
    {
        _secondsPerBeat = 60f / bpm;
    }

    void Update()
    {
        RecalcBeatInterval();
        float offsetMs = Root.Instance.DelaySet;
        float offsetBeat = offsetMs / 1000f / _secondsPerBeat;

        // realTimeAcc：真实游戏累计时间（不带偏移）
        _realTimeAcc += Time.deltaTime;
        // currentBeat = 真实时间转拍数 + 玩家偏移拍数
        float rawBeat = _realTimeAcc / _secondsPerBeat;
        currentBeat = rawBeat + offsetBeat;

        Palse.SetByValue(currentBeat % 4f);
        CheckBeatTrigger();
    }

    void CheckBeatTrigger()
    {
        int currentWholeBeat = Mathf.FloorToInt(currentBeat);
        if (currentWholeBeat > _lastTriggerBeat)
        {
            // 现在currentBeat本身已经带offset，不需要额外再加triggerBeatOffset
            _pendingFireBeat = currentWholeBeat;
            _lastTriggerBeat = currentWholeBeat;
        }

        if (currentBeat >= _pendingFireBeat)
        {
            var target = beatTargets[_currentTargetIndex];
            if (target != null)
            {
                var anim = target.GetComponent<Anim_Beat>();
                if (anim != null)
                {
                    anim.Play();
                }
            }
            _currentTargetIndex = (_currentTargetIndex + 1) % beatTargets.Length;
            _pendingFireBeat = float.MaxValue;
        }
    }

    /// <summary>
    /// 返回 当前拍数距离最近整数拍的时间（毫秒）
    /// 正数：晚；负数：早
    /// 【现在这个函数自动带上玩家DelaySet偏移，判定窗口整体平移，符合耳机延迟调整逻辑】
    /// </summary>
    public float GetMsToNearestBeat()
    {
        float msPerBeat = _secondsPerBeat * 1000f;
        float nearestBeat = Mathf.Round(currentBeat);
        float beatDelta = currentBeat - nearestBeat;
        return beatDelta * msPerBeat;
    }

    /// <summary>
    /// 重置节拍，歌曲从头播放
    /// </summary>
    public void ResetBeat()
    {
        _realTimeAcc = 0;
        currentBeat = 0f;
        _lastTriggerBeat = -1;
        _currentTargetIndex = 0;
        _pendingFireBeat = float.MaxValue;
    }
}
