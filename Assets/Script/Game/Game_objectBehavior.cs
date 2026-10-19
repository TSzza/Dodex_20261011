using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType
{
    none,
    block,
    tile,
    spawn,
    flag,
    enemy,
}

public class Game_objectBehavior : MonoBehaviour
{
    public ObjectType myType = ObjectType.none;

    private Game_Controller game_Controller;
    [Header("颤抖")]
    private float shakeDuration = 0.3f;
    private float shakeAmplitude = 0.15f;
    private float shakeSmoothSpeed = 8f;

    [Header("照亮")]
    private float lightUpRaiseTime = 0.2f;
    private float lightUpHoldTime = 1.5f;
    private float lightUpFadeTime = 0.8f;  // 这六个参数去 Game Controller 拿

    [Header("推动回弹")]
    private float pushDuration = 0.25f;
    private float pushMaxOffset = 0.08f;
    private float pushClearDuration = 0.1f;

    // 颤抖偏移量
    protected Vector3 _shakeOffset;
    protected Vector3 _targetShakeOffset;
    protected Coroutine _shakeCoroutine;
    protected Coroutine _lightCoroutine;
    protected Coroutine _bounceCoroutine;// 协程缓存
    protected Coroutine _whenenableCoroutine;

    private void Start()
    {
        game_Controller = Root.Instance.game_Controller;

        shakeDuration = game_Controller.shakeDuration;
        shakeAmplitude = game_Controller.shakeAmplitude;
        shakeSmoothSpeed = game_Controller.shakeSmoothSpeed;
        lightUpRaiseTime = game_Controller.lightUpRaiseTime;
        lightUpHoldTime = game_Controller.lightUpHoldTime;
        lightUpFadeTime = game_Controller.lightUpFadeTime;
        pushDuration = game_Controller.pushDuration;
        pushMaxOffset = game_Controller.pushMaxOffset;
        pushClearDuration = game_Controller.pushClearDuration;

        SetTransparent(0);
    }

    private void OnEnable() // 向Game_Controller发送信息
    {
        _whenenableCoroutine = StartCoroutine(WhenEnable());
    }
    protected virtual IEnumerator WhenEnable()
    {
        yield return null;
        switch (myType)
        {
            case ObjectType.spawn: // 重设出生点
                //Debug.Log(transform.position); // 
                game_Controller.SetSpawnPos(transform.position);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 1. 被推动，原地回弹，dir：推动方向向量
    /// </summary>
    public virtual void PushBounce(Vector2 dir)
    {
        if (_bounceCoroutine != null)
            StopCoroutine(_bounceCoroutine);
        _bounceCoroutine = StartCoroutine(PushBounceCoroutine(dir));
    }
    protected virtual IEnumerator PushBounceCoroutine(Vector2 worldDir)
    {
        Vector3 baseLocalPos = transform.localPosition;
        // 把【世界空间方向】转为父物体的局部空间方向
        Transform parentTf = transform.parent;
        Vector2 localDir = parentTf != null ? parentTf.InverseTransformVector(worldDir) : worldDir;

        float t = 0;
        Vector3 startOffset = localDir * pushMaxOffset;
        Vector3 currentOffset = Vector3.zero;

        while (t < pushDuration)
        {
            t += Time.deltaTime;
            float p = t / pushDuration;
            float s = Mathf.Sin(p * Mathf.PI) * (1 - p);
            Vector3 targetOffset = startOffset * s;
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * shakeSmoothSpeed);
            transform.localPosition = baseLocalPos + currentOffset;
            yield return null;
        }

        float clearTimer = 0;
        while (clearTimer < pushClearDuration)
        {
            clearTimer += Time.deltaTime;
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * shakeSmoothSpeed);
            transform.localPosition = baseLocalPos + currentOffset;
            yield return null;
        }

        transform.localPosition = baseLocalPos;
    }

    /// <summary>
    /// 2. 照亮：自身+子物体SpriteRenderer
    /// </summary>
    public virtual void LightUp()
    {
        if (_lightCoroutine != null)
            StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LightUpCoroutine(false));
    }
    public virtual void LightUpEnd()
    {
        if (_lightCoroutine != null)
            StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LightUpCoroutine(true));
    }
    protected virtual void SetTransparent(float t)
    {
        List<SpriteRenderer> sprs = new List<SpriteRenderer>();
        GetComponentsInChildren<SpriteRenderer>(includeInactive: true, sprs);
        foreach (var spr in sprs)
        {
            Color c = spr.color;
            c.a = t;
            spr.color = c;
        }
    }
    protected virtual IEnumerator LightUpCoroutine(bool isEnd)
    {
        List<SpriteRenderer> sprs = new List<SpriteRenderer>();
        GetComponentsInChildren<SpriteRenderer>(includeInactive: true, sprs);

        Dictionary<SpriteRenderer, float> originAlpha = new Dictionary<SpriteRenderer, float>();
        foreach (var s in sprs)
        {
            originAlpha[s] = s.color.a;
        }

        float timer = 0;
        // 升起
        while (timer < lightUpRaiseTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / lightUpRaiseTime);
            foreach (var kvp in originAlpha)
            {
                Color c = kvp.Key.color;
                c.a = Mathf.Lerp(kvp.Value, 1f, t);
                kvp.Key.color = c;
            }
            yield return null;
        }
        if (!isEnd)
        {
            // 保持
            yield return new WaitForSeconds(lightUpHoldTime);
            // 淡出
            timer = 0;
            while (timer < lightUpFadeTime)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / lightUpFadeTime);
                foreach (var kvp in originAlpha)
                {
                    Color c = kvp.Key.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    kvp.Key.color = c;
                }
                yield return null;
            }
        }
    }

    /// <summary>
    /// 3. 原地颤抖
    /// </summary>
    public virtual void Shake()
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    protected virtual IEnumerator ShakeCoroutine()
    {
        Vector3 basePos = transform.localPosition;
        float timer = 0f;
        Vector3 currentOffset = Vector3.zero;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            Vector3 targetOffset = new Vector3(
                Random.Range(-shakeAmplitude, shakeAmplitude),
                Random.Range(-shakeAmplitude, shakeAmplitude),
                0
            );
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * shakeSmoothSpeed);
            transform.localPosition = basePos + currentOffset;
            yield return null;
        }

        // 抖动结束，平滑把偏移归0
        float clearTimer = 0f;
        float clearDur = 0.1f;
        while (clearTimer < clearDur)
        {
            clearTimer += Time.deltaTime;
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * shakeSmoothSpeed);
            transform.localPosition = basePos + currentOffset;
            yield return null;
        }
        transform.localPosition = basePos;
    }

    #region 强制终止函数（OnDisable调用，立刻回滚状态）
    public virtual void ForceStopShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }
        // 颤抖立刻复位位置
        transform.localPosition = transform.localPosition;
    }

    public virtual void ForceStopLight()
    {
        if (_lightCoroutine != null)
        {
            StopCoroutine(_lightCoroutine);
            _lightCoroutine = null;
        }
        // 立刻把所有子物体SpriteRenderer透明度置0
        List<SpriteRenderer> sprs = new List<SpriteRenderer>();
        GetComponentsInChildren<SpriteRenderer>(includeInactive: true, sprs);
        foreach (var spr in sprs)
        {
            Color c = spr.color;
            c.a = 0f;
            spr.color = c;
        }
    }
    public virtual void ForceStopBounce()
    {
        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }
        // 回弹动画立刻复位位置
        transform.localPosition = transform.localPosition;
    }
    #endregion

    protected virtual void OnDisable()
    {
        ForceStopShake();
        ForceStopLight();
        ForceStopBounce();
        if (_whenenableCoroutine != null) // 用于停下协程
        {
            StopCoroutine(_whenenableCoroutine);
            _whenenableCoroutine = null;
        }
    }
}