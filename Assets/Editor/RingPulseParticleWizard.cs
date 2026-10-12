// ============================================================================
//  RingPulseParticleWizard — 一键把选中的粒子系统调成「一圈一圈向外扩散」的环形脉冲
//
//  用法：Hierarchy 里选中带 ParticleSystem 的物体
//        → 顶部菜单「Tools / Dodex / 把选中粒子调成环形脉冲」（快捷键 F9）
//
//  想调手感就改下面那 5 个常量，不用进 Inspector 翻十几个模块。
//
//  这个文件是纯工具，不需要了可以直接删掉，不影响其他东西。
// ============================================================================

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RingPulseParticleWizard
{
    // ---------------- 想改效果就动这几个数 ----------------
    const float PulseInterval = 1.4f;    // 每圈之间的间隔（秒）—— 想更快就调小
    const short PerRing       = 28;      // 每圈撒多少颗粒子 —— 想更密就调大
    const float ExpandSpeed   = 3.5f;    // 向外扩散的速度（单位/秒）
    const float StartSize     = 0.22f;   // 粒子出生时的大小
    const float StartRadius   = 0.08f;   // 出生圆环的半径（0.05~0.15 之间比较自然）
    // ------------------------------------------------------

    static readonly Color SparkColor = new Color(0.65f, 0.90f, 1f, 1f);   // 冷青白，像光
    static readonly Color EmissiveColor = new Color(2.2f, 2.8f, 3.4f, 1f); // HDR，交给 Bloom 溢出

    [MenuItem("Tools/Dodex/把选中粒子调成环形脉冲 _F9")]
    static void Configure()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogError("[环形脉冲] 先在 Hierarchy 里选中一个粒子系统物体，再按 F9");
            return;
        }

        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError("[环形脉冲] 选中的「" + go.name + "」上面没有 ParticleSystem 组件");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(go, "Configure Ring Pulse");
        var rend = go.GetComponent<ParticleSystemRenderer>();

        // ---------- 主模块：一圈的时长 = 一颗粒子的寿命 ----------
        var main = ps.main;
        main.duration        = PulseInterval;
        main.loop            = true;
        main.prewarm         = false;
        main.startLifetime   = PulseInterval * 0.95f;  // 略短于间隔，两圈之间留一点空隙
        main.startSpeed      = 0f;                     // 速度全部交给"生命周期内速度"的径向
        main.startSize       = StartSize;
        main.startColor      = SparkColor;
        main.startRotation   = 0f;
        main.gravityModifier = 0f;                     // 有重力圈就会掉成椭圆
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 波纹留在原地扩散
        main.maxParticles    = 4000;
        main.scalingMode     = ParticleSystemScalingMode.Hierarchy;

        // ---------- 发射：关掉持续发射，改成"每圈爆发一次" ----------
        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, PerRing) });

        // ---------- 形状：圆周 + 均匀铺开（这是"一圈"的关键）----------
        var shape = ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Circle;
        shape.radius          = StartRadius;
        shape.radiusThickness = 0f;    // 0 = 只在圆周边缘发射，不是整个圆盘
        shape.arc             = 360f;
        shape.rotation        = new Vector3(90f, 0f, 0f);  // 把圆从水平面立起来朝向 2D 相机
        // 注意：属性名叫 arcMode，不是 mode（Inspector 里显示成 Mode 而已，写 mode 会 CS1061）
        shape.arcMode         = ParticleSystemShapeMultiModeValue.BurstSpread; // 圆周上均匀分布
        shape.arcSpread       = 0f;    // 0 = 圆周上任意位置都能生成（注意属性名是 arcSpread，没有 spread）
        shape.alignToDirection = false;

        // ---------- 速度：沿半径向外扩散 ----------
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x = 0f;
        vel.y = 0f;
        vel.z = 0f;
        vel.radial = new ParticleSystem.MinMaxCurve(ExpandSpeed);

        // ---------- 把可能干扰的模块关掉，避免叠出奇怪效果 ----------
        // 坑：ParticleSystem 的模块属性返回的是 struct 副本，
        //     「ps.xxx.enabled = false」这种链式写法会报 CS1612，必须先取到局部变量再改。
        var force     = ps.forceOverLifetime;            force.enabled     = false;
        var noise     = ps.noise;                        noise.enabled     = false;
        var coll      = ps.collision;                    coll.enabled      = false;
        var sizeBySp  = ps.sizeBySpeed;                  sizeBySp.enabled  = false;
        var colorBySp = ps.colorBySpeed;                 colorBySp.enabled = false;
        var rotOL     = ps.rotationOverLifetime;         rotOL.enabled     = false;
        var rotBySp   = ps.rotationBySpeed;              rotBySp.enabled   = false;
        var trails    = ps.trails;                       trails.enabled    = false;
        var extForce  = ps.externalForces;               extForce.enabled  = false;
        var limitVel  = ps.limitVelocityOverLifetime;    limitVel.enabled  = false;
        var inherit   = ps.inheritVelocity;              inherit.enabled   = false;

        // ---------- 大小：越往外越小，圈才有"扩散散去"的感觉 ----------
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.30f)));

        // ---------- 颜色：淡入 → 保持 → 淡出 ----------
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f,    0f),
                new GradientAlphaKey(1f,    0.12f),   // 快速亮起
                new GradientAlphaKey(0.85f, 0.50f),
                new GradientAlphaKey(0f,    1f)       // 结尾淡出
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // ---------- 渲染器 ----------
        if (rend != null)
        {
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.sortMode   = ParticleSystemSortMode.None;
            rend.alignment  = ParticleSystemRenderSpace.View;
            rend.maxParticleSize = 1f;   // 默认 0.5 会把大粒子截断

            if (!IsParticleMaterial(rend.sharedMaterial))
            {
                var mat = CreateOrGetParticleMaterial();
                if (mat != null)
                {
                    rend.sharedMaterial = mat;
                    Debug.Log("[环形脉冲] 材质已换成 " + mat.name +
                              "（原来是 Sprite-Lit-Default，那个是受光照的精灵材质，不会自发光）");
                }
            }
        }

        EditorUtility.SetDirty(go);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        SceneView.RepaintAll();

        Debug.Log("[环形脉冲] 配好了。想调手感改脚本顶部的 PulseInterval / PerRing / ExpandSpeed。");
    }

    static bool IsParticleMaterial(Material m)
    {
        return m != null && m.shader != null && m.shader.name.Contains("Particles");
    }

    /// <summary>
    /// 生成一个 URP 粒子材质（加色混合，适合发光粒子）。
    /// 已经存在就直接复用。
    /// </summary>
    static Material CreateOrGetParticleMaterial()
    {
        const string dir  = "Assets/Materials";
        const string path = dir + "/RingPulseParticle.mat";

        var exist = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (exist != null) return exist;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("[环形脉冲] 没找到 URP 的粒子 shader，请手动给渲染器指定一个粒子材质");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var mat = new Material(shader) { name = "RingPulseParticle" };

        // 内置粒子贴图（软圆点）。资源名带不带扩展名各版本不一样，两个都试
        var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        if (tex == null) tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle");
        if (tex != null) mat.SetTexture("_BaseMap", tex);

        mat.SetColor("_BaseColor", SparkColor);
        // 再走一遍 emission 通道，确保亮度能超过 1 —— Bloom 只认 >1 的像素
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", EmissiveColor);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        // URP 粒子 shader 默认是"不透明"，必须手动改成加色透明混合
        mat.SetFloat("_Surface", 1f);                                   // Transparent
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);                // SrcAlpha + One = 加色
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
