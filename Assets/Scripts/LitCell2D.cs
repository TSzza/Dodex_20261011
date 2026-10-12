// ============================================================================
//  LitCell2D —— 挂在「地块」上：光扫过 → 亮起 → 停留一会儿 → 线性暗下去
//
//  这是整个「探路」效果的核心，也是它和 Light2D 受光材质最本质的区别：
//
//     Light2D 受光材质    光即亮、光走即暗（没有记忆），而且是软边圆形光
//     LitCell2D           存的是「到期时间戳」(litUntil)，光照过之后地块还会
//                         停留一会儿再线性暗下去 —— 一浪接一浪的余晖感，
//                         正是那个游戏最抓人的视觉特征
//
//  具体亮度不自己算颜色，而是每帧算出 0~1 的强度写进材质的 _LitAmount，
//  由 shader 去混合「环境色 → 光色」。这样明暗过渡完全在 GPU 上做，稳定。
//
//  ⚠ 配套材质必须是 "Dodex/2D/Sprite Lit By Field"，否则 _LitAmount 无效。
//    一键换成这个材质：菜单 Tools/Dodex/搭建探路光照（或按 F11）。
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class LitCell2D : MonoBehaviour
{
    /// <summary>地块材质类型 —— 决定光色、亮多久、以及光子会不会被它反射。</summary>
    public enum Kind
    {
        Floor  = 0,   // 地板：不挡光，留一层淡淡的痕迹（1.1 秒）
        Wall   = 1,   // 普通墙：反射但能量砍半（2.0 秒）
        Mirror = 2,   // 镜面墙：几乎全反射、几乎不损光 → 窄走廊变光纤（2.0 秒）
        Moss   = 3,   // 毛石墙：全吸收不反射，但亮得特别久（2.0 × 1.8 秒）
    }

    // ------------------------------------------------------------------
    //  下面这组常量直接对标原游戏 beat-light.html 里的 T 参数表，改这里
    //  就等于改原游戏的数值，方便两边对齐手感。
    // ------------------------------------------------------------------
    public const float FloorSeconds = 1.10f;   // 原 floorLightMs = 1100ms
    public const float WallSeconds  = 2.00f;   // 原 wallLightMs  = 2000ms
    public const float MossMul      = 1.80f;   // 原 mossLightMul = 1.8
    public const float MirrorChance = 0.95f;   // 镜面墙反射概率
    public const float WallChance   = 0.50f;   // 普通墙反射概率
    public const float MossChance   = 0.00f;   // 毛石墙全吸收

    // 四种材质的光色（取自原游戏的 rgba 填充常量）
    public static readonly Color FloorColor  = new Color(0.184f, 0.369f, 0.569f); // rgba(47,94,145)
    public static readonly Color WallColor   = new Color(0.471f, 0.647f, 0.922f); // rgba(120,165,235)
    public static readonly Color MirrorColor = new Color(0.588f, 0.882f, 1.000f); // rgba(150,225,255)
    public static readonly Color MossColor   = new Color(0.769f, 0.659f, 0.439f); // rgba(196,168,112)

    // 四种材质的描边色（原游戏的 stroke 常量，比填充亮得多 —— 亮的是「边」不是「面」）
    public static readonly Color FloorRim   = new Color(0.450f, 0.660f, 0.850f);
    public static readonly Color WallRim    = new Color(0.706f, 0.831f, 1.000f); // rgba(180,212,255)
    public static readonly Color MirrorRim  = new Color(0.824f, 0.973f, 1.000f); // rgba(210,248,255)
    public static readonly Color MossRim    = new Color(0.933f, 0.824f, 0.588f); // rgba(238,210,150)

    // ------------------------------ 可调项 ------------------------------
    [Header("类型（决定光色 / 亮多久 / 反不反射）")]
    public Kind kind = Kind.Floor;

    [Header("被照亮后停留多久（秒）")]
    [Tooltip("地板 1.1 / 墙 2.0 / 毛石 3.6。点一次「按类型套用默认值」即可自动填。")]
    public float litSeconds = FloorSeconds;

    // ---------------------------- 运行时状态 ----------------------------
    float litUntil = float.NegativeInfinity;   // 亮到什么时候（TTL 语义的核心）
    float lastApplied = -1f;                   // 上次写进材质的强度，避免每帧重复写

    SpriteRenderer sr;
    MaterialPropertyBlock mpb;
    Bounds cachedBounds;
    bool boundsReady;

    static readonly int IdLitAmount    = Shader.PropertyToID("_LitAmount");
    static readonly int IdLitColor     = Shader.PropertyToID("_LitColor");
    static readonly int IdRimColor     = Shader.PropertyToID("_RimColor");
    static readonly int IdFillStrength = Shader.PropertyToID("_FillStrength");
    static readonly int IdRimStrength  = Shader.PropertyToID("_RimStrength");

    /// <summary>场景里所有地块。光子每帧要拿它做碰撞检测。</summary>
    public static readonly List<LitCell2D> All = new List<LitCell2D>();

    /// <summary>空间上是否是个能挡光的实体（地板不挡）。</summary>
    public bool BlocksLight { get { return kind != Kind.Floor; } }

    /// <summary>该地块的包围盒（每帧缓存一次，避免光子逐个查询时反复调 native）。</summary>
    public Bounds CellBounds
    {
        get
        {
            if (!boundsReady) { cachedBounds = sr.bounds; boundsReady = true; }
            return cachedBounds;
        }
    }

    // ====================================================================
    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        // 先按类型把时长对齐一次，省得忘了填
        if (litSeconds <= 0.01f) ApplyKindDefaults();

        CheckMaterial();
        PushToShader(0f);
    }

    void Update()
    {
        float a = Mathf.Clamp01((litUntil - Time.time) / Mathf.Max(0.0001f, litSeconds));

        // 只有「正在亮」或「刚暗下去那一帧」才需要写，静止的暗地块零开销
        if (a <= 0f && lastApplied <= 0f) { boundsReady = false; return; }

        PushToShader(a);
        boundsReady = false;
    }

    // ====================================================================
    /// <summary>
    /// 点亮这个格子。strength 是 0~1 的「能量」——
    /// 原游戏里能量越高的光子撞上来，格子亮得越久（0.35 + 0.65 × 能量）。
    /// </summary>
    public void Illuminate(float strength)
    {
        float dur = litSeconds * Mathf.Max(0.05f, strength);
        float v   = Time.time + dur;
        if (litUntil < v) litUntil = v;      // 取最大值：后来的光只能延长，不能缩短
    }

    /// <summary>
    /// 世界坐标点是否落在这个地块的包围盒内。
    /// 注意：光子碰撞**不走**这里 —— 它用的是「线段 vs 包围盒」，
    /// 否则速度快的光子会直接穿过去。这个方法留给别的系统查询用。
    /// </summary>
    public bool Contains(Vector2 p)
    {
        Bounds b = CellBounds;
        return p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y;
    }

    /// <summary>
    /// 求反射方向。verticalFace = 光子是从左/右边进入的（也就是撞在竖直面上）。
    /// 用「入射的是哪一面」而不是「比速度分量大小」—— 后者在斜擦进角落时会翻错轴。
    /// </summary>
    public Vector2 Reflect(Vector2 v, bool verticalFace)
    {
        return verticalFace ? new Vector2(-v.x, v.y) : new Vector2(v.x, -v.y);
    }

    /// <summary>按当前类型套用推荐时长（地板 1.1 / 墙 2.0 / 毛石 ×1.8）。</summary>
    public void ApplyKindDefaults()
    {
        litSeconds = DefaultSecondsFor(kind);
    }

    public static float DefaultSecondsFor(Kind k)
    {
        switch (k)
        {
            case Kind.Floor:  return FloorSeconds;
            case Kind.Mirror: return WallSeconds;
            case Kind.Moss:   return WallSeconds * MossMul;
            default:          return WallSeconds;
        }
    }

    public static Color ColorFor(Kind k)
    {
        switch (k)
        {
            case Kind.Mirror: return MirrorColor;
            case Kind.Moss:   return MossColor;
            case Kind.Wall:   return WallColor;
            default:          return FloorColor;
        }
    }

    public static Color RimColorFor(Kind k)
    {
        switch (k)
        {
            case Kind.Mirror: return MirrorRim;
            case Kind.Moss:   return MossRim;
            case Kind.Wall:   return WallRim;
            default:          return FloorRim;
        }
    }

    /// <summary>
    /// 填充占整格的多少强度。原游戏的填充 alpha 只有 0.20~0.30（很淡），
    /// 亮的是描边；这里为了「一眼看出被照亮」把填充抬到了 0.34~0.55，
    /// 但保持「描边比填充亮」的层次关系不变。
    /// </summary>
    public static float FillStrengthFor(Kind k)
    {
        switch (k)
        {
            case Kind.Mirror: return 0.38f;
            case Kind.Moss:   return 0.34f;
            case Kind.Wall:   return 0.34f;
            default:          return 0.55f;
        }
    }

    /// <summary>描边强度。地板给弱一点（原游戏地板没有描边，这里补一圈较亮的边，好认格）。</summary>
    public static float RimStrengthFor(Kind k)
    {
        switch (k)
        {
            case Kind.Mirror: return 1.10f;
            case Kind.Moss:   return 1.00f;
            case Kind.Wall:   return 1.00f;
            default:          return 0.70f;
        }
    }

    // ====================================================================
    void PushToShader(float amount)
    {
        if (sr == null) return;
        mpb.SetFloat(IdLitAmount, amount);
        mpb.SetColor(IdLitColor, ColorFor(kind));
        mpb.SetColor(IdRimColor, RimColorFor(kind));
        mpb.SetFloat(IdFillStrength, FillStrengthFor(kind));
        mpb.SetFloat(IdRimStrength, RimStrengthFor(kind));
        sr.SetPropertyBlock(mpb);
        lastApplied = amount;
    }

    /// <summary>材质挂错了就当场喊一声，别让用户对着不亮的地块猜半天。</summary>
    void CheckMaterial()
    {
        var m = sr.sharedMaterial;
        if (m == null || m.shader == null) return;
        if (m.shader.name == "Dodex/2D/Sprite Lit By Field") return;

        Debug.LogWarning(
            "[LitCell2D] 「" + name + "」的材质是 " + m.shader.name +
            "，不是 Dodex/2D/Sprite Lit By Field —— 光照不会生效。\n" +
            "  菜单 Tools/Dodex/搭建探路光照（或按 F11）可以一键换好。", this);
    }
}
