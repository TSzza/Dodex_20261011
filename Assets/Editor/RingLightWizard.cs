// ============================================================================
//  RingLightWizard —— 一键把场景搭成「环形脉冲光 + 受光地块」
//
//  做三件事：
//    ① 确保场景里有 Global Light 2D —— 很暗的环境光。
//       没有它，未照亮的区域会是纯黑，连路都看不见；有了它才是"暗但可辨"的探路氛围。
//    ② 在脉冲中心挂上 RingPulseLight —— 光一圈圈往外扩散、扫过地块。
//    ③ 把用 SpriteGlow（Unlit）的渲染器换成受光材质 ——
//       这是「地块能被照亮」的**前提**：Unlit 材质不受任何光照影响。
//
//  用法：菜单 Tools/Dodex/搭建环形光照（照亮地块）  或直接按 F10
//  全部操作都注册了 Undo，不满意可以 Ctrl+Z 整组撤销。
// ============================================================================

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class RingLightWizard
{
    // ---------------- 想改默认氛围就动这几个数 ----------------
    const float AmbientIntensity = 0.16f;                                  // 越小越黑，越有探路感
    static readonly Color AmbientColor = new Color(0.35f, 0.45f, 0.75f, 1f); // 冷蓝夜色
    // ---------------------------------------------------------

    const string RingObjectName    = "Ring Pulse Light";
    const string GlobalObjectName  = "Global Light 2D";
    const string GlowShaderName    = "Dodex/2D/Sprite Glow";               // 本工程的自发光 shader（Unlit）
    const string LitShaderName     = "Universal Render Pipeline/2D/Sprite-Lit-Default";
    const string UrpDefaultLitGuid = "a97c105638bdf8b4a8650670310a4cd3";  // URP 内置受光材质

    [MenuItem("Tools/Dodex/搭建环形光照（照亮地块） _F10")]
    static void Setup()
    {
        // ---------- 0. 定脉冲中心 ----------
        var sel = Selection.activeGameObject;
        Transform center = sel != null ? sel.transform : null;
        if (center == null)
        {
            var psgo = GameObject.Find("Particle System");
            if (psgo != null) center = psgo.transform;
        }
        Vector3 centerPos = center != null ? center.position : Vector3.zero;

        Undo.SetCurrentGroupName("搭建环形光照");
        int grp = Undo.GetCurrentGroup();

        // ---------- 1. Global Light 2D ----------
        Light2D global = null;
        foreach (var l in Object.FindObjectsByType<Light2D>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l != null && l.lightType == Light2D.LightType.Global) { global = l; break; }
        }
        if (global == null)
        {
            var go = new GameObject(GlobalObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create Global Light 2D");
            global = go.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
        }
        global.color     = AmbientColor;
        global.intensity = AmbientIntensity;
        EditorUtility.SetDirty(global);

        // ---------- 2. 环形脉冲光 ----------
        var ringGo = GameObject.Find(RingObjectName);
        RingPulseLight ring;
        if (ringGo == null)
        {
            ringGo = new GameObject(RingObjectName);
            Undo.RegisterCreatedObjectUndo(ringGo, "Create Ring Pulse Light");
            ringGo.transform.position = centerPos;
            ring = ringGo.AddComponent<RingPulseLight>();
        }
        else
        {
            ring = ringGo.GetComponent<RingPulseLight>();
            if (ring == null) ring = ringGo.AddComponent<RingPulseLight>();
        }

        // 节奏跟着粒子系统走，省得两边对不上
        float intervalNote = ring.pulseInterval;
        if (center != null)
        {
            var ps = center.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float d = ps.main.duration;
                if (d > 0.01f)
                {
                    ring.pulseInterval = d;
                    intervalNote = d;
                }
            }
        }
        EditorUtility.SetDirty(ring);

        // ---------- 3. Unlit 自发光材质 → 受光材质 ----------
        int switched = 0;
        var lit = ResolveLitMaterial();
        if (lit != null)
        {
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial;
                if (m == null || m.shader == null) continue;
                if (m.shader.name != GlowShaderName) continue;   // 只动自己写的那个 Unlit 材质

                Undo.RecordObject(r, "Switch sprite to Lit material");
                r.sharedMaterial = lit;
                EditorUtility.SetDirty(r);
                switched++;
            }
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(grp);
        SceneView.RepaintAll();

        Debug.Log(
            "[环形光照] 搭好了：\n" +
            "  ① Global Light 2D    强度 " + AmbientIntensity + "（环境光，别调太亮）\n" +
            "  ② " + RingObjectName + "  脉冲间隔 " + intervalNote + " 秒" +
            (center != null ? "，中心 = " + center.name : "，中心 = 世界原点（没找到参考物体）") + "\n" +
            "  ③ 换成受光材质的地块：" + switched + " 个\n" +
            "  下一步：按 Play 看效果。要保存场景记得 Ctrl+S。\n" +
            "  提示：光圈效果只在运行时出现，编辑器里看不到是正常的。");
    }

    /// <summary>拿到一个受光材质：优先复用 URP 自带的，拿不到就新建一个。</summary>
    static Material ResolveLitMaterial()
    {
        var path = AssetDatabase.GUIDToAssetPath(UrpDefaultLitGuid);
        if (!string.IsNullOrEmpty(path))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
        }

        var shader = Shader.Find(LitShaderName);
        if (shader == null)
        {
            Debug.LogWarning("[环形光照] 找不到受光 shader「" + LitShaderName +
                             "」。如果你用的是 3D 渲染管线，2D 光照用不了。");
            return null;
        }

        const string dir = "Assets/Materials";
        const string p   = dir + "/GroundLit.mat";
        var exist = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (exist != null) return exist;

        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var mat = new Material(shader) { name = "GroundLit" };
        AssetDatabase.CreateAsset(mat, p);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
