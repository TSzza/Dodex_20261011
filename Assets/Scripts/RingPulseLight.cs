// ============================================================================
//  RingPulseLight —— 环形脉冲光（让光一圈一圈扫出去，照亮扫过的地块）
//
//  原理：在自身周围摆一圈 Point Light2D，按固定节奏从中心向外扩散并淡出。
//        光扫过【受光材质】的地块时，地块被照亮。
//
//  ⚠️ 关键前提：要被照亮的东西必须用「受光材质」。
//      · URP 内置受光材质：Universal Render Pipeline/2D/Sprite-Lit-Default
//      · 本工程自写的 SpriteGlow（Dodex/2D/Sprite Glow）是 Unlit，
//        它不受任何光照影响，挂上去永远不会被照亮——两回事，别混。
//
//  用法：挂在任意空物体上 → 把物体放在脉冲中心 → 运行 Play。
//        参数建议和粒子系统对齐（pulseInterval 设成粒子系统的 Duration）。
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class RingPulseLight : MonoBehaviour
{
    [Header("脉冲节奏（和粒子系统对齐）")]
    [Tooltip("每圈之间的间隔（秒）。建议设成粒子系统的 Duration")]
    public float pulseInterval = 1.4f;

    [Tooltip("一圈用几个点光源拼成。越多越圆，开销越大")]
    [Range(3, 24)] public int lightsPerRing = 12;

    [Header("扩散范围")]
    [Tooltip("光的出生半径")]
    public float startRadius = 0.15f;

    [Tooltip("光扩散到多远就消散")]
    public float endRadius = 4.9f;

    [Header("光照")]
    public Color lightColor = new Color(0.62f, 0.86f, 1f, 1f);

    [Tooltip("峰值强度")]
    public float peakIntensity = 2.5f;

    [Tooltip("每个点光源的照亮半径")]
    public float lightRadius = 1.4f;

    [Tooltip("扩散到末尾时照亮半径放大到几倍（光晕越铺越开）。1 = 不放大")]
    public float radiusGrowth = 1.6f;

    [Range(0f, 1f)]
    [Tooltip("边缘柔和度：0 = 硬边，1 = 非常柔")]
    public float falloff = 0.6f;

    [Tooltip("0 = Multiply（推荐：暗处保持暗）；1 = Additive（整体发亮）")]
    public int blendStyleIndex = 0;

    Light2D[] m_Lights;

    void OnEnable()
    {
        BuildLights();
        Apply(0f);
    }

    void OnDisable()
    {
        if (m_Lights == null) return;
        for (int i = 0; i < m_Lights.Length; i++)
            if (m_Lights[i] != null) m_Lights[i].intensity = 0f;
    }

    /// <summary>生成一圈 Point Light2D 子物体。</summary>
    void BuildLights()
    {
        // 清掉上一次生成的（万一在同一个会话里重复调用）
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name.StartsWith("RingLight_")) Destroy(c.gameObject);
        }

        int n = Mathf.Clamp(lightsPerRing, 3, 24);
        m_Lights = new Light2D[n];

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("RingLight_" + i);
            go.transform.SetParent(transform, false);

            var L = go.AddComponent<Light2D>();
            L.lightType = Light2D.LightType.Point;
            L.color = lightColor;
            L.intensity = 0f;
            L.falloffIntensity = falloff;
            L.blendStyleIndex = blendStyleIndex;
            L.pointLightOuterAngle = 360f;
            L.pointLightInnerAngle = 360f;
            L.pointLightInnerRadius = lightRadius * 0.25f;
            L.pointLightOuterRadius = lightRadius;

            // 注意：不要碰 pointLightDistance / pointLightQuality，
            //       它们被标了 [Obsolete(..., true)]，写上去直接编译失败。

            m_Lights[i] = L;
        }
    }

    void Update()
    {
        float t = pulseInterval > 0.01f
            ? Mathf.Repeat(Time.time, pulseInterval) / pulseInterval
            : 0f;
        Apply(t);
    }

    /// <param name="t">这一圈的进度，0 = 刚出生，1 = 该消散了</param>
    void Apply(float t)
    {
        if (m_Lights == null || m_Lights.Length == 0) return;

        float ringR = Mathf.Lerp(startRadius, endRadius, t);

        // 强度包络：前 12% 快速亮起 → 中段保持 → 后 40% 淡出
        float rise = Mathf.Clamp01(t / 0.12f);
        float fall = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, t));
        float k = rise * fall;

        float glowR = lightRadius * Mathf.Lerp(1f, Mathf.Max(1f, radiusGrowth), t);

        int n = m_Lights.Length;
        for (int i = 0; i < n; i++)
        {
            var L = m_Lights[i];
            if (L == null) continue;

            float a = (i / (float)n) * Mathf.PI * 2f;
            L.transform.localPosition = new Vector3(Mathf.Cos(a) * ringR, Mathf.Sin(a) * ringR, 0f);
            L.intensity = peakIntensity * k;
            L.color = lightColor;
            L.blendStyleIndex = blendStyleIndex;
            L.falloffIntensity = falloff;

            // Point 光源的网格是固定的四边形，改半径只影响 boundingSphere（每帧更新），
            // 不会触发网格重建 —— 所以每帧改半径是安全的、也是唯一让它"长大"的办法。
            L.pointLightOuterRadius = glowR;
            L.pointLightInnerRadius = glowR * 0.25f;
        }
    }
}
