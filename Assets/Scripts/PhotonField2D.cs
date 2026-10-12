// ============================================================================
//  PhotonField2D —— 光子场：你唯一的「眼睛」
//
//  复刻探路游戏里最核心的那套机制：
//    · 每次脉冲从自身位置向四周撒出一圈光子
//    · 光子飞到哪儿，哪块地块就亮起来（LitCell2D.Illuminate）
//    · 撞到墙上会反射 —— 镜面墙几乎不损能量，于是窄走廊变成「光纤」，
//      一次脉冲能点亮整条走廊；代价是走廊尽头的东西也会被一起照亮
//    · 能量每反射一次就衰减，所以「照多远」不是写死的常数，
//      而是地图几何 + 墙材质的计算结果
//
//  碰撞用「线段 vs 包围盒」(slab 算法) 而不是「点是否在框内」——
//  否则速度快的光子会直接穿墙过去，看起来像光漏了。
//
//  渲染：所有光子合成一个 Mesh，一次 DrawMesh 画完（加色混合）。
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PhotonField2D : MonoBehaviour
{
    // ------------------------------------------------------------------
    [Header("脉冲发射")]
    [Tooltip("关掉就只在按键时发射一次")]
    public bool  autoPulse       = true;
    [Tooltip("每几秒撒一圈。96 BPM 的一拍 = 0.625 秒")]
    public float pulseInterval   = 0.625f;
    [Tooltip("每圈撒多少颗")]
    public int   photonsPerPulse = 30;
    public KeyCode manualPulseKey = KeyCode.Space;

    [Header("光子")]
    [Tooltip("世界单位/秒。对应原游戏的 175px/s ÷ 32px = 5.47")]
    public float speed = 5.5f;
    public float life  = 1.9f;
    [Tooltip("光子的边长")]
    public float size  = 0.09f;
    [Tooltip("上限，防止极端情况卡死")]
    public int   maxPhotons = 600;

    [Header("反射（能量逐次衰减）")]
    public int   maxBounce  = 4;
    public float minEnergy  = 0.2f;
    public float mirrorLoss = 0.15f;   // 镜面墙：损得少，弹得远
    public float wallLoss   = 0.55f;   // 普通墙：两三代就耗尽

    [Header("渲染")]
    public Color tint = new Color(0.55f, 0.78f, 1f, 0.55f);

    // ------------------------------------------------------------------
    struct Photon
    {
        public Vector2 pos;
        public Vector2 vel;
        public float   life;
        public float   energy;   // 剩多少能量（决定点亮时长、还能弹几次）
        public int     gen;      // 反射代数
    }

    readonly List<Photon> photons = new List<Photon>();
    float nextPulseTime;

    Mesh     mesh;
    Material mat;
    readonly List<Vector3> verts = new List<Vector3>(4096);
    readonly List<Color>   cols  = new List<Color>(4096);
    readonly List<int>     tris  = new List<int>(6144);

    /// <summary>当前存活光子数（诊断/调试用）。</summary>
    public int PhotonCount { get { return photons.Count; } }

    // ====================================================================
    void OnEnable()
    {
        nextPulseTime = Time.time;
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (mat  != null) Destroy(mat);
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(manualPulseKey)) Pulse();
#endif

        if (autoPulse && Time.time >= nextPulseTime)
        {
            Pulse();
            nextPulseTime = Time.time + Mathf.Max(0.05f, pulseInterval);
        }

        Step(Time.deltaTime);
        Draw();
    }

    // ====================================================================
    /// <summary>从自身位置向四周撒一圈光子。</summary>
    public void Pulse()
    {
        Vector2 c = transform.position;
        for (int i = 0; i < photonsPerPulse; i++)
        {
            float ang = (i / (float)photonsPerPulse) * Mathf.PI * 2f + Random.value * 0.05f;
            float sp  = speed * (0.86f + Random.value * 0.28f);
            photons.Add(new Photon
            {
                pos    = c,
                vel    = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp,
                life   = life,
                energy = 1f,
                gen    = 0,
            });
        }

        if (photons.Count > maxPhotons)
            photons.RemoveRange(0, photons.Count - maxPhotons);
    }

    // ====================================================================
    void Step(float dt)
    {
        var cells = LitCell2D.All;
        if (cells.Count == 0) { photons.Clear(); return; }

        for (int i = photons.Count - 1; i >= 0; i--)
        {
            Photon p = photons[i];

            Vector2 from = p.pos;
            Vector2 to   = p.pos + p.vel * dt;

            p.life -= dt;
            if (p.life <= 0f) { photons.RemoveAt(i); continue; }

            // ---- 找这一段路程里撞到的第一个地块 ----
            LitCell2D hitCell = null;
            LitCell2D floorCell = null;
            float  hitT = float.MaxValue;
            float  floorT = float.MaxValue;
            bool   hitVertical = false;
            Vector2 hitPoint = to;

            for (int c = 0; c < cells.Count; c++)
            {
                LitCell2D cell = cells[c];
                if (cell == null) continue;

                if (SegmentHitsBounds(from, to, cell.CellBounds, out float t, out bool vert))
                {
                    if (cell.BlocksLight)
                    {
                        if (t < hitT) { hitT = t; hitCell = cell; hitVertical = vert; }
                    }
                    else if (t < floorT)
                    {
                        floorCell = cell; floorT = t;
                    }
                }
            }

            // ---- 撞墙：点亮 + 反射 ----
            if (hitCell != null)
            {
                // 能量越高照得越久（复刻原游戏 0.35 + 0.65 × 能量）
                float strength = 0.35f + 0.65f * p.energy;
                if (hitCell.kind == LitCell2D.Kind.Moss)
                    strength *= LitCell2D.MossMul;
                hitCell.Illuminate(strength);

                float chance = ChanceOf(hitCell.kind);
                if (p.gen < maxBounce && p.energy > minEnergy && Random.value < chance)
                {
                    float loss    = hitCell.kind == LitCell2D.Kind.Mirror ? mirrorLoss : wallLoss;
                    bool  mirror  = hitCell.kind == LitCell2D.Kind.Mirror;
                    float spdMul  = mirror ? 1.18f : 0.90f;   // 镜面反射的光跑得更快…
                    float lifeMul = mirror ? 1.00f : 0.88f;   // …也活得更久 → 才能沿走廊推下去

                    Vector2 rv = hitCell.Reflect(p.vel, hitVertical);
                    Vector2 n  = rv.normalized;

                    // 把光子推离墙面时，用的是「墙面外法线」而不是速度方向：
                    // 光子斜擦墙面时速度几乎平行于墙，沿它推推不出去，
                    // 下一帧又判定「还在墙里」，光子就会卡在墙缝里反复反射。
                    Vector2 outN = hitVertical
                        ? new Vector2(-Mathf.Sign(p.vel.x), 0f)
                        : new Vector2(0f, -Mathf.Sign(p.vel.y));

                    photons.Add(new Photon
                    {
                        pos    = hitPoint + outN * (size * 2f),
                        vel    = n * (speed * spdMul),
                        life   = p.life * lifeMul,
                        energy = p.energy * (1f - loss),
                        gen    = p.gen + 1,
                    });
                }

                photons.RemoveAt(i);
                continue;
            }

            // ---- 地板：留一层更淡的痕迹，但光继续飞 ----
            if (floorCell != null) floorCell.Illuminate(1f);

            p.pos = to;
            photons[i] = p;
        }
    }

    static float ChanceOf(LitCell2D.Kind k)
    {
        switch (k)
        {
            case LitCell2D.Kind.Mirror: return LitCell2D.MirrorChance;
            case LitCell2D.Kind.Moss:   return LitCell2D.MossChance;
            default:                    return LitCell2D.WallChance;
        }
    }

    /// <summary>
    /// 线段 a→b 是否穿过包围盒（slab 算法）。
    /// t 是首次接触点的插值参数；verticalFace = 从左/右边进入（决定反射翻哪个轴）。
    /// </summary>
    static bool SegmentHitsBounds(Vector2 a, Vector2 b, Bounds bd,
                                  out float t, out bool verticalFace)
    {
        t = 0f;
        verticalFace = false;

        Vector2 d = b - a;
        float tminX = 0f, tmaxX = 1f, tminY = 0f, tmaxY = 1f;

        if (Mathf.Abs(d.x) < 1e-6f)
        {
            if (a.x < bd.min.x || a.x > bd.max.x) return false;
        }
        else
        {
            float t1 = (bd.min.x - a.x) / d.x;
            float t2 = (bd.max.x - a.x) / d.x;
            if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
            tminX = t1; tmaxX = t2;
        }

        if (Mathf.Abs(d.y) < 1e-6f)
        {
            if (a.y < bd.min.y || a.y > bd.max.y) return false;
        }
        else
        {
            float t1 = (bd.min.y - a.y) / d.y;
            float t2 = (bd.max.y - a.y) / d.y;
            if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
            tminY = t1; tmaxY = t2;
        }

        float enter = Mathf.Max(tminX, tminY);
        float exit  = Mathf.Min(tmaxX, tmaxY);
        if (enter > exit || exit < 0f || enter > 1f) return false;

        t = Mathf.Max(0f, enter);
        // 入射面 = 真正决定「进入时间」的那个轴。注意 d.x ≈ 0 时（垂直飞行）
        // 必须判成上下边，否则会翻错轴 —— 光子会沿着墙"滑"而不是弹回来。
        verticalFace = Mathf.Abs(d.x) >= 1e-6f
                    && (Mathf.Abs(d.y) < 1e-6f || tminX >= tminY);
        return true;
    }

    // ====================================================================
    void Draw()
    {
        if (photons.Count == 0)
        {
            if (mesh != null) mesh.Clear();
            return;
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "PhotonField" };
            mesh.MarkDynamic();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        if (mat == null) mat = CreateMaterial();

        verts.Clear(); cols.Clear(); tris.Clear();

        float h = size * 0.5f;
        const float Z = -0.05f;   // 稍微朝相机方向偏一点，保证盖在地块之上

        for (int i = 0; i < photons.Count; i++)
        {
            Photon p = photons[i];
            float la = Mathf.Clamp01(p.life / Mathf.Max(0.0001f, life));
            Color c  = new Color(tint.r, tint.g, tint.b, tint.a * la);

            int b0 = verts.Count;
            verts.Add(new Vector3(p.pos.x - h, p.pos.y - h, Z));
            verts.Add(new Vector3(p.pos.x - h, p.pos.y + h, Z));
            verts.Add(new Vector3(p.pos.x + h, p.pos.y + h, Z));
            verts.Add(new Vector3(p.pos.x + h, p.pos.y - h, Z));
            for (int k = 0; k < 4; k++) cols.Add(c);

            tris.Add(b0); tris.Add(b0 + 1); tris.Add(b0 + 2);
            tris.Add(b0); tris.Add(b0 + 2); tris.Add(b0 + 3);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);

        Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, gameObject.layer);
    }

    Material CreateMaterial()
    {
        Shader sh = Shader.Find("Dodex/2D/Photon Dot");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");

        var m = new Material(sh) { name = "PhotonDot (runtime)" };
        m.color = Color.white;
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        return m;
    }
}
