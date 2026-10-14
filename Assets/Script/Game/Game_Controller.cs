using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Game_Controller : MonoBehaviour
{
    [Header("游戏")]
    public bool isGaming = false;
    public bool hasWon = false;
    public GameObject textWin;
    public GameObject textLose;
    [Header("地图")]
    public float GridSize = 1f; // 一格距离

    [Header("主人公")]
    public GameObject Peo; // 主人公，Inspector拖入物体
    private Vector3 SpawnPos;
    public float jumpHeight=0.2f;
    public float moveDuration = 0.2f; // 移动耗时
    private bool isMoving = false; // 防止连续移动
    public float getRadius = 0.2f; // 夺旗/被怪抓到的范围
    
    public void SetSpawnPos(Vector3 pos)
    {
        SpawnPos = pos;
    }
    public void StartLevel() // 开始新关卡
    {
        isGaming = true;
        hasWon = false;
        Peo.transform.position = SpawnPos;
        Debug.Log("新游戏开始！");
    }
    public void RestartLevel() // 重新开始关卡
    {
        isGaming = true;
        Peo.transform.position = SpawnPos;
        Debug.Log("游戏重新开始！");
    }
    public void WinLevel()
    {
        Audio.PlaySFX("win");
        isGaming = false;
        StartCoroutine(GameOverCoroutine(true));
        Debug.Log("成功，游戏结束！");
    }
    public void WinLevelNext()
    {
        hasWon = true;
        Debug.Log("按下按钮，游戏正式结束！");
    }
    public void LoseLevel()
    {
        Audio.PlaySFX("lose");
        isGaming = false;
        StartCoroutine(GameOverCoroutine(false));
        Debug.Log("失败，游戏结束！");
    }
    protected IEnumerator GameOverCoroutine(bool isWin)
    {
        textWin.SetActive(false);
        textLose.SetActive(false);
        float timer = 0;
        while (true)
        {
            // 每一帧都检测R键，按下立刻退出
            if (!isWin && Input.GetKeyDown(KeyCode.R)) // 输了重开
            {
                textLose.SetActive(false);
                RestartLevel();
                yield break;
            }
            if (isWin && Input.GetKeyDown(KeyCode.Return))
            {
                textWin.SetActive(false);
                WinLevelNext();
                yield break;
            }

            timer += Time.deltaTime;
            if (textWin.activeSelf && timer >= 1f)
            {
                textWin.SetActive(false);
                timer = 0f;
            }
            if (textWin.activeSelf && timer >= 0.6f)
            {
                textWin.SetActive(true);
                timer = 0f;
            }
            yield return null;
        }
    }

    private Audio_Controller Audio;
    protected virtual void Start()
    {
        Audio = Root.Instance.audio_Controller;
        peoLight = Peo.GetComponent<Light2D>();
    }
    void Update()
    {
        if (!isGaming) // 不进行游戏时取消一切输入
            return;


        if (!isMoving)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                StartCoroutine(MoveTarget(Vector3.right * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                StartCoroutine(MoveTarget(Vector3.left * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                StartCoroutine(MoveTarget(Vector3.up * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                StartCoroutine(MoveTarget(Vector3.down * GridSize));
            }
        }
    }


    public enum Score
    {
        perfect,good,early,late,miss,
    }
    [Header("节拍")]
    public Beat_Manager beat_Manager;
    public Anim_floatText anim_FloatText;
    public Anim_scaleSprite pulse_FloatText;
    public float perfectTime = 25f;
    public float goodTime = 50f;
    public float lateTime = 120f;

    [Header("——游戏物体——")]
    public Transform MapParent;
    public GameObject lightParticleFBX;
    public Transform lightParent;
    [Header("颤抖")]
    public float shakeDuration = 0.3f;
    public float shakeAmplitude = 0.15f;
    public float shakeSmoothSpeed = 8f;

    [Header("照亮")]
    public float lightUpRaiseTime = 0.2f;
    public float lightUpHoldTime = 1.5f;
    public float lightUpFadeTime = 0.8f;

    [Header("推动回弹")]
    public float pushRadius = 0.5f;
    public float pushDuration = 0.25f;
    public float pushMaxOffset = 0.08f;
    public float pushClearDuration = 0.1f;
    /// <summary>
    /// 主人公移动时的协程
    /// </summary>
    IEnumerator MoveTarget(Vector3 offset)
    {
        // 开始移动：先播放动画并进行判定

        float deltaTime = beat_Manager.GetMsToNearestBeat();
        Score score = Score.perfect;
        if (Mathf.Abs(deltaTime) <= perfectTime)
            score = Score.perfect;
        else if (Mathf.Abs(deltaTime) <= goodTime)
            score = Score.good;
        else if (Mathf.Abs(deltaTime) <= lateTime && deltaTime > 0)
            score = Score.late;
        else if (Mathf.Abs(deltaTime) <= lateTime && deltaTime < 0)
            score = Score.early;
        else
            score = Score.miss;

        anim_FloatText.Play(score);
        pulse_FloatText.Play();
        if (score == Score.miss)
        {
            Audio.PlaySFX("miss");
            ShootPeoLight(false); // 错误闪亮
            yield break;
        }
        else
        {
            ShootPeoLight(true); // 立即闪亮
        }

        Vector3 startPos = Peo.transform.position;
        Vector3 endPos = startPos + offset;

        // 检测endPos位置是否存在2D碰撞体
        bool hitBlock = false;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(endPos, pushRadius);
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider != null)
            {
                if (hitCollider.TryGetComponent<Game_objectBehavior>(out var objBehavior))
                {
                    if (objBehavior.myType == ObjectType.block)
                    {
                        Debug.Log("hit Block!");
                        // 计算方向：从Peo指向被碰撞物体（转为Vector2）
                        Vector2 pushDir = (hitCollider.transform.position - Peo.transform.position).normalized;

                        // 取消移动，调用Block的推动回弹动画
                        objBehavior.PushBounce(pushDir);
                        hitBlock = true;
                    }
                }
            }
        }
        if (hitBlock) // 运行一个代码后结束协程：让整个Game进行一次推动，非常有趣
        {
            Audio.PlaySFX("push");
            ShootLights();

            Vector3 baseWorldPos = transform.position;
            Vector2 pushDir = offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector2.right;
            Vector3 startOffset = pushDir * pushMaxOffset;
            Vector3 currentOffset = Vector3.zero;
            float t = 0;

            while (t < pushDuration)
            {
                t += Time.deltaTime;
                float p = t / pushDuration;
                float s = Mathf.Sin(p * Mathf.PI) * (1 - p);
                Vector3 targetOffset = startOffset * s;
                currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * 12f);
                transform.position = baseWorldPos + currentOffset;
                yield return null;
            }

            float clearTimer = 0;
            while (clearTimer < pushClearDuration)
            {
                clearTimer += Time.deltaTime;
                currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * 12f);
                transform.position = baseWorldPos + currentOffset;
                yield return null;
            }
            transform.position = baseWorldPos;

            yield break;
        }

        // 开始移动
        Audio.PlaySFX("notmiss");

        isMoving = true;
        float timer = 0f;
        float height = jumpHeight; // 抛物线最大高度，按需调整

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);

            // 水平：EaseOut 先快后慢
            float tEase = 1 - Mathf.Pow(1 - t, 2);
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, tEase);

            // Y抛物线：t=0.5时到达最高点
            float parabolaY = height * 4 * t * (1 - t);

            // 合成最终位置
            Peo.transform.position = new Vector3(
                horizontalPos.x,
                startPos.y + parabolaY,
                horizontalPos.z
            );

            yield return null;
        }

        Peo.transform.position = endPos;
        isMoving = false;


        ShootLights(); // 成功移动之后再发射一次
        
        
        bool getFlag = false;
        bool getEnemy = false;
        // 检测此位置是否存在2D碰撞体
        hitColliders = Physics2D.OverlapCircleAll(Peo.transform.position, getRadius);
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider != null)
            {
                if (hitCollider.TryGetComponent<Game_objectBehavior>(out var objBehavior))
                {
                    if (objBehavior.myType == ObjectType.flag) // 预定胜利
                    {
                        getFlag = true;
                    }
                    if (objBehavior.myType == ObjectType.enemy)
                    {
                        getFlag = false;
                    }
                }
            }
        }
        
        if (getFlag) // 获得物结算
        {
            WinLevel();
        }
        if (getEnemy)
        {
            LoseLevel();
        }
    }

    [Header("灯光粒子发射")]
    public int splitCount = 8;         // 360°分成8份
    public float splitStartAngle = 0;         // 起始角
    public float innerRadius = 0.3f;   // 生成内径，每个实例从这个环形位置生成
    public float shootSpeed = 5f;      // 发射速度
    public float maxDistance = 10f;      // 发射速度

    void ShootLights()
    {
        if (lightParticleFBX == null)
        {
            Debug.LogWarning($"[{name}] lightParticleFBX 未赋值，无法发射");
            return;
        }
        if (lightParent == null)
        {
            Debug.LogWarning($"[{name}] lightParent 未赋值");
            return;
        }
        if (splitCount <= 0)
        {
            Debug.LogWarning($"[{name}] splitCount 不能小于等于0");
            return;
        }

        float angleStep = 360f / splitCount;

        for (int i = 0; i < splitCount; i++)
        {
            float deg = (splitStartAngle + angleStep * i) % 360f;
            float rad = deg * Mathf.Deg2Rad;

            // 方向向量
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            // 生成位置：自身位置 + 沿着角度方向偏移innerRadius内径
            Vector3 spawnPos = Peo.transform.position + new Vector3(dir.x * innerRadius, dir.y * innerRadius, 0);

            // 实例化
            GameObject inst = Instantiate(lightParticleFBX, spawnPos, Quaternion.identity, lightParent);
            if (inst.TryGetComponent<Game_lightBehavior>(out var lightBehavior))
            {
                lightBehavior.ShootLight(spawnPos, dir, shootSpeed, maxDistance);
            }
            else
            {
                Debug.LogWarning($"[{name}] 实例化物体没有 Game_lightBehavior");
            }
        }
    }


    private Light2D peoLight;

    [Header("人物移动发光动画")]
    public Color RightColor;
    public Color WrongColor;
    public float wrongMult = 0.5f;

    public float peoLightRaiseTime = 0.1f;
    public float peoLightHoldTime = 0f;
    public float peoLightFadeTime = 0.5f;

    public float minOuterRadius = 0.9f;
    public float maxOuterRadius = 3f;

    public float minShadowStrength = 0f;
    public float maxShadowStrength = 1f;

    public float minIntensity = 15f;
    public float maxIntensity = 30f;

    private Coroutine _lightAnimCoroutine;


    /// <summary>
    /// 启动灯光动画
    /// </summary>
    public void ShootPeoLight(bool NotWrong)
    {
        if (_lightAnimCoroutine != null)
        {
            StopCoroutine(_lightAnimCoroutine);
            _lightAnimCoroutine = null;
        }
        if (NotWrong)
        {
            peoLight.color = RightColor;
            wrongMult = 1f;
        }
        else
        {
            peoLight.color = WrongColor;
            wrongMult = 0.5f;
        }

        _lightAnimCoroutine = StartCoroutine(LightAnimationCoroutine());
    }

    protected IEnumerator LightAnimationCoroutine()
    {
        // 捕获当前实时灯光值 作为本次动画起点
        float startRadius = peoLight.pointLightOuterRadius;
        float startShadow = peoLight.shadowIntensity;
        float startIntensity = peoLight.intensity;

        float timer = 0f;
        // 上升
        while (timer < peoLightRaiseTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / peoLightRaiseTime);
            SetLightLerp(t, startRadius, maxOuterRadius * wrongMult, startShadow, maxShadowStrength * wrongMult, startIntensity, maxIntensity * wrongMult);
            yield return null;
        }

        // 保持
        yield return new WaitForSeconds(peoLightHoldTime);

        // 淡出
        timer = 0f;
        while (timer < peoLightFadeTime * wrongMult)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / peoLightFadeTime);
            SetLightLerp(t, maxOuterRadius * wrongMult, minOuterRadius, maxShadowStrength * wrongMult, minShadowStrength, maxIntensity * wrongMult, minIntensity);
            yield return null;
        }

        peoLight.color = RightColor;
    }

    // 泛用插值函数
    private void SetLightLerp(float t, float startR, float endR, float startS, float endS, float startI, float endI)
    {
        if (peoLight == null) return;
        peoLight.pointLightOuterRadius = Mathf.Lerp(startR, endR, t);
        peoLight.shadowIntensity = Mathf.Lerp(startS, endS, t);
        peoLight.intensity = Mathf.Lerp(startI, endI, t);
    }

    /// <summary>
    /// 强制停止动画，把灯光参数重置到最小值
    /// </summary>
    public void ForceResetLight()
    {
        if (_lightAnimCoroutine != null)
        {
            StopCoroutine(_lightAnimCoroutine);
            _lightAnimCoroutine = null;
        }
        if (peoLight != null)
        {
            peoLight.pointLightOuterRadius = minOuterRadius;
            peoLight.shadowIntensity = minShadowStrength;
            peoLight.intensity = minIntensity;
        }
    }

    protected virtual void OnDisable()
    {
        ForceResetLight();
    }
}
