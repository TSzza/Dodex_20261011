using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Anim_Line : MonoBehaviour
{
    [Header("基础线条设置")]
    public float lineLength = 10f;
    public int segmentCount = 80;
    public bool closeLoop = false;
    public Vector3 startOffset = Vector3.zero;

    [Header("整体缩放")]
    public float overallHeightScale = 0.6f;

    [Header("主大波浪（从左向右流动）")]
    public float waveAmplitude = 1f;
    public float waveFrequency = 1.2f;
    public float waveScrollSpeed = 2f;

    [Header("尖锐小瑕疵尖刺")]
    public float glitchStrength = 0.25f;
    public float glitchDensity = 10f;
    public float glitchSharpness = 2.5f;
    public float glitchScrollSpeed = 1.4f;

    [Header("平缓细微起伏（代替之前高频抖动）")]
    public float subtleNoiseStrength = 0.08f;
    public float subtleNoiseDensity = 2.5f;
    public float subtleScrollSpeed = 0.6f;

    [Header("随机种子")]
    public int randomSeed = 777;

    private LineRenderer _lr;
    public float _waveOffset;
    private float _glitchOffset;
    private float _subtleOffset;
    private float lineWidthDefault;
    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        lineWidthDefault = _lr.startWidth;
        RegenerateLine();
    }

    void Update()
    {
        // 根据父物体放缩线宽
        _lr.startWidth = _lr.endWidth = lineWidthDefault * transform.parent.localScale.x;

        // 三层噪声独立时间偏移，各自滚动
        _waveOffset += Time.deltaTime * waveScrollSpeed;
        _glitchOffset += Time.deltaTime * glitchScrollSpeed;
        _subtleOffset += Time.deltaTime * subtleScrollSpeed;

        UpdateLinePoints();
    }

    [ContextMenu("重新生成线条")]
    public void RegenerateLine()
    {
        Random.InitState(randomSeed);
        //_waveOffset = 0f;
        //_glitchOffset = 0f;
        //_subtleOffset = 0f;
        UpdateLinePoints();
    }

    void UpdateLinePoints()
    {
        _lr.positionCount = closeLoop ? segmentCount + 1 : segmentCount;
        float step = lineLength / (segmentCount - 1);

        for (int i = 0; i < _lr.positionCount; i++)
        {
            float t = i * step;

            // 1.主波浪
            float mainWave = Mathf.Sin((t - _waveOffset) * waveFrequency) * waveAmplitude;

            // 2.尖锐瑕疵尖刺
            float glitchRaw = Mathf.PerlinNoise((t - _glitchOffset) * glitchDensity, randomSeed * 0.1f);
            float glitch = Mathf.Pow(glitchRaw, glitchSharpness) * glitchStrength;

            // 3.平缓细微起伏（低频，不再疯狂抖动）
            float subtleRaw = Mathf.PerlinNoise((t - _subtleOffset) * subtleNoiseDensity, randomSeed * 0.2f);
            float subtle = (subtleRaw * 2f - 1f) * subtleNoiseStrength;

            float finalY = (mainWave + glitch + subtle) * overallHeightScale;

            Vector3 pos = startOffset + new Vector3(t, finalY, 0f);
            _lr.SetPosition(i, pos);
        }

        if (closeLoop)
        {
            _lr.SetPosition(_lr.positionCount - 1, _lr.GetPosition(0));
        }
    }
}
