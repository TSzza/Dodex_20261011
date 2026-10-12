// ============================================================================
//  LightFieldWizard —— 一键把场景搭成「探路游戏」的光照效果
//
//  和上一版 RingLightWizard（Light2D 方案）的区别，是这两套机制本身的区别：
//    RingLightWizard   用 12 个 Light2D 排成圆环 —— 软边圆形光，光走即暗，没有记忆
//    本脚本            用光照场 (TTL) + 自定义 shader —— 格子硬边，光走了还会停留
//                      一会儿再暗下去，撞墙会反射。这才是原游戏的手感。
//
//  做四件事：
//    ① 生成/复用地块材质 Assets/Materials/SpriteLitByField.mat
//    ② 给场景里每个 SpriteRenderer 挂 LitCell2D 并换成该材质
//    ③ 建一个 PhotonField2D 负责撒光子（默认放在选中的物体上，没选就放原点）
//    ④ 把相机背景压成 #04070b（原游戏的底色 —— 未照亮的地方就是这个颜色）
//
//  用法：菜单 Tools/Dodex/搭建探路光照   或直接按 F11
//  全部操作注册了 Undo，不满意 Ctrl+Z 整组撤销。
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LightFieldWizard
{
    const string FieldShaderName  = "Dodex/2D/Sprite Lit By Field";
    const string MatFolder        = "Assets/Materials";
    const string MatPath          = MatFolder + "/SpriteLitByField.mat";
    const string CharMatPath      = MatFolder + "/SpriteGlow.mat";   // 角色用的「自己发光」材质
    const string PhotonObjectName = "Photon Field";

    // 原游戏的底色 rgba 换算
    static readonly Color SceneBackground = new Color(0.016f, 0.027f, 0.043f, 1f); // #04070b

    [MenuItem("Tools/Dodex/搭建探路光照 _F11")]
    static void Setup()
    {
        // ---------- 0. 找光子发射点 ----------
        Transform centerT = Selection.activeGameObject != null
            ? Selection.activeGameObject.transform : null;
        if (centerT == null)
        {
            var psgo = GameObject.Find("Particle System");
            if (psgo != null) centerT = psgo.transform;
        }
        Vector3 centerPos = centerT != null ? centerT.position : Vector3.zero;

        Undo.SetCurrentGroupName("搭建探路光照");
        int grp = Undo.GetCurrentGroup();

        // ---------- 1. 材质 ----------
        Material mat = ResolveFieldMaterial();
        if (mat == null)
        {
            EditorUtility.DisplayDialog("搭建探路光照",
                "找不到 shader「" + FieldShaderName + "」。\n\n" +
                "通常是 Assets/Shaders/SpriteLitByField.shader 还没编译完，\n" +
                "或者 Console 里这个 shader 报了错。\n" +
                "先看 Console，编译干净后再按一次 F11。", "知道了");
            return;
        }

        // ---------- 2. 地块：挂组件 + 换材质 ----------
        int touched = 0, skipped = 0, restored = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr == null) continue;

            // 玩家 / 敌人的精灵不能当地块 —— 注意这里看的是**整条祖先链**：
            // 玩家精灵自己的名字叫 Square，光看名字会漏掉，把玩家也照成一块地砖。
            if (ShouldSkip(sr.transform))
            {
                skipped++;
                if (RestoreCharacterSprite(sr)) restored++;
                continue;
            }

            Undo.RecordObject(sr, "Switch to light field material");
            sr.sharedMaterial = mat;
            EditorUtility.SetDirty(sr);

            var cell = sr.GetComponent<LitCell2D>();
            if (cell == null)
            {
                cell = Undo.AddComponent<LitCell2D>(sr.gameObject);
                cell.kind = LitCell2D.Kind.Floor;
                cell.ApplyKindDefaults();
            }
            EditorUtility.SetDirty(cell);
            touched++;
        }

        // ---------- 3. 光子场 ----------
        var fieldGo = GameObject.Find(PhotonObjectName);
        PhotonField2D field;
        if (fieldGo == null)
        {
            fieldGo = new GameObject(PhotonObjectName);
            Undo.RegisterCreatedObjectUndo(fieldGo, "Create Photon Field");
            fieldGo.transform.position = centerPos;
            field = fieldGo.AddComponent<PhotonField2D>();
        }
        else
        {
            field = fieldGo.GetComponent<PhotonField2D>();
            if (field == null) field = fieldGo.AddComponent<PhotonField2D>();
        }

        // 脉冲间隔对齐粒子系统（两边节奏一致，观感才对得上）
        float intervalNote = field.pulseInterval;
        if (centerT != null)
        {
            var ps = centerT.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float d = ps.main.duration;
                if (d > 0.01f) { field.pulseInterval = d; intervalNote = d; }
            }
        }
        EditorUtility.SetDirty(field);

        // ---------- 3.5 跟随玩家：光要跟着人跑，不能死在世界原点 ----------
        Transform playerT = FindPlayer();
        string followNote = "没找到 player，已跳过（可手动拖）";
        if (playerT != null)
        {
            AttachFollow(fieldGo, playerT);
            if (centerT != null && centerT.gameObject != fieldGo)
                AttachFollow(centerT.gameObject, playerT);
            followNote = playerT.name;
        }

        // ---------- 4. 相机底色 ----------
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Set camera background");
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = SceneBackground;
            EditorUtility.SetDirty(cam);
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(grp);
        SceneView.RepaintAll();

        Debug.Log(
            "[探路光照] 搭好了：\n" +
            "  ① 地块材质 → SpriteLitByField（" + touched + " 个" +
                (skipped > 0 ? "，跳过 " + skipped + " 个玩家/敌人/粒子精灵" : "") +
                (restored > 0 ? "，并修好了 " + restored + " 个被上次误换成地砖的角色" : "") + "）\n" +
            "  ② " + PhotonObjectName + "：每 " + intervalNote.ToString("0.##") + " 秒撒 " +
                field.photonsPerPulse + " 颗光子，中心 = " +
                (centerT != null ? centerT.name : "世界原点") + "\n" +
            "  ③ 跟随：光子场 / 粒子系统 → " + followNote + "\n" +
            "  ④ 相机底色 → #04070b（未照亮处就是这个颜色）\n" +
            "  按 Play 看效果。地块默认都是「地板」；把要挡光的设成墙：\n" +
            "    选中地块 → 菜单 Tools/Dodex/设为 [墙 / 镜面墙 / 毛石墙]\n" +
            "  觉得整体偏暗/偏亮：改 Assets/Materials/SpriteLitByField.mat 的 Brightness 和 Ambient。\n" +
            "  提示：光子/闪光只在运行时出现，编辑器里看不到是正常的。");
    }

    // ====================================================================
    [MenuItem("Tools/Dodex/设为 地板", true)]
    [MenuItem("Tools/Dodex/设为 普通墙", true)]
    [MenuItem("Tools/Dodex/设为 镜面墙", true)]
    [MenuItem("Tools/Dodex/设为 毛石墙", true)]
    static bool ValidateSetKind() { return Selection.activeGameObject != null; }

    [MenuItem("Tools/Dodex/设为 地板")]
    static void SetFloor()  { ApplyKind(LitCell2D.Kind.Floor); }
    [MenuItem("Tools/Dodex/设为 普通墙")]
    static void SetWall()   { ApplyKind(LitCell2D.Kind.Wall); }
    [MenuItem("Tools/Dodex/设为 镜面墙")]
    static void SetMirror() { ApplyKind(LitCell2D.Kind.Mirror); }
    [MenuItem("Tools/Dodex/设为 毛石墙")]
    static void SetMoss()   { ApplyKind(LitCell2D.Kind.Moss); }

    static void ApplyKind(LitCell2D.Kind k)
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            var cell = go.GetComponent<LitCell2D>();
            if (cell == null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                cell = Undo.AddComponent<LitCell2D>(sr.gameObject);
            }
            Undo.RecordObject(cell, "Set cell kind");
            cell.kind = k;
            cell.ApplyKindDefaults();
            EditorUtility.SetDirty(cell);
            n++;
        }
        Debug.Log("[探路光照] 已把 " + n + " 个地块设为「" + k + "」（时长 " +
                  LitCell2D.DefaultSecondsFor(k).ToString("0.##") + " 秒）");
        SceneView.RepaintAll();
    }

    // ====================================================================
    /// <summary>找玩家：先按名字精确找，再按名字包含 player / 玩家 找。</summary>
    static Transform FindPlayer()
    {
        var go = GameObject.Find("player");
        if (go == null) go = GameObject.Find("Player");
        if (go == null) go = GameObject.Find("玩家");
        if (go != null) return go.transform;

        foreach (var t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.name.ToLowerInvariant().Contains("player") || t.name.Contains("玩家"))
                return t;
        }
        return null;
    }

    /// <summary>给物体挂上 FollowTarget2D 并绑定目标（已存在就只更新绑定，不重复挂）。</summary>
    static void AttachFollow(GameObject go, Transform target)
    {
        if (go == null || target == null) return;
        if (go.transform == target) return;          // 自己跟自己是无意义的

        var f = go.GetComponent<FollowTarget2D>();
        if (f == null) f = Undo.AddComponent<FollowTarget2D>(go);
        Undo.RecordObject(f, "Bind follow target");
        f.Bind(target);
        EditorUtility.SetDirty(f);
    }

    /// <summary>
    /// 这个精灵该不该跳过（不当地块）。
    /// 关键：沿**整条祖先链**判断，而不是只看物体自己的名字 ——
    /// 玩家精灵自己叫 "Square"，但它的父物体才叫 "player"，
    /// 只看自己的名字会把玩家也当成一块地砖（旧版本的 bug）。
    /// </summary>
    static bool ShouldSkip(Transform t)
    {
        for (Transform c = t; c != null; c = c.parent)
        {
            string n = c.name;
            if (string.IsNullOrEmpty(n)) continue;
            string low = n.ToLowerInvariant();
            if (low.Contains("player") || low.Contains("enemy") || low.Contains("particle")
                || low.Contains("camera") || low.Contains("volume")
                || n.Contains("玩家") || n.Contains("敌人") || n.Contains("相机"))
                return true;
        }
        return false;
    }

    static bool IsFieldMaterial(Material m)
    {
        return m != null && m.shader != null && m.shader.name == FieldShaderName;
    }

    /// <summary>
    /// 修复上一次 F11 的误伤：旧版本可能把玩家 / 敌人的精灵也换成了光照场材质，
    /// 让它们变成一块暗色地砖。这里恢复成「自己发光」的角色材质，
    /// 并摘掉误挂上去的 LitCell2D。返回是否真的改动过。
    /// </summary>
    static bool RestoreCharacterSprite(SpriteRenderer sr)
    {
        bool changed = false;

        if (IsFieldMaterial(sr.sharedMaterial))
        {
            var charMat = AssetDatabase.LoadAssetAtPath<Material>(CharMatPath);
            if (charMat != null)
            {
                Undo.RecordObject(sr, "Restore character material");
                sr.sharedMaterial = charMat;
                EditorUtility.SetDirty(sr);
                changed = true;
            }
        }

        var wrongCell = sr.GetComponent<LitCell2D>();
        if (wrongCell != null)
        {
            Undo.DestroyObjectImmediate(wrongCell);
            changed = true;
        }
        return changed;
    }

    static Material ResolveFieldMaterial()
    {
        var exist = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (exist != null) return exist;

        var shader = Shader.Find(FieldShaderName);
        if (shader == null) return null;

        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var m = new Material(shader) { name = "SpriteLitByField" };
        ApplyLookDefaults(m);
        AssetDatabase.CreateAsset(m, MatPath);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Material>(MatPath);
    }

    /// <summary>
    /// 新建材质时写入一套「看得见、够亮」的默认观感。
    /// 只在材质不存在时执行 —— 已有材质上的手动微调不会被覆盖。
    /// </summary>
    static void ApplyLookDefaults(Material m)
    {
        if (m == null) return;
        if (m.HasProperty("_Ambient"))       m.SetFloat("_Ambient", 0.05f);
        if (m.HasProperty("_Brightness"))    m.SetFloat("_Brightness", 3.2f);
        if (m.HasProperty("_TintInfluence")) m.SetFloat("_TintInfluence", 0.25f);
        if (m.HasProperty("_RimWidth"))      m.SetFloat("_RimWidth", 0.09f);
        if (m.HasProperty("_FillStrength"))  m.SetFloat("_FillStrength", 0.55f);
        if (m.HasProperty("_RimStrength"))   m.SetFloat("_RimStrength", 0.55f);
        if (m.HasProperty("_AmbientColor"))
            m.SetColor("_AmbientColor", new Color(0.22f, 0.30f, 0.52f, 1f));
    }
}
