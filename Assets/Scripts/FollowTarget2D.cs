// ============================================================================
//  FollowTarget2D —— 让这个物体跟着目标走（2D 俯视角常用：光跟着玩家跑）
//
//  挂在哪：
//    · Photon Field         → 光子就从玩家身上撒出来，而不是死在世界原点
//    · Particle System      → 你那圈粒子也跟着玩家跑
//    · Main Camera          → 想做成"摄像机跟拍"也行
//
//  为什么用独立组件而不是给每个脚本都加一个字段：
//    一个概念、到处能用。哪个物体要跟随，挂上它、把 player 拖进 Target 就行。
//
//  平滑时间 = 0 时是「硬跟随」，LateUpdate 里直接对齐坐标 —— 光会稳稳贴在玩家身上。
//  给一点值（0.05~0.15）则会略微拖后，看起来更"软"。
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class FollowTarget2D : MonoBehaviour
{
    [Header("跟随目标（把 player 拖进来）")]
    public Transform target;

    [Header("偏移（想让光偏在玩家前方就改这里）")]
    public Vector2 offset = Vector2.zero;

    [Header("平滑时间（秒）：0 = 硬跟随")]
    [Range(0f, 1f)]
    public float smoothTime = 0f;

    [Header("跟随的轴")]
    public bool followX = true;
    public bool followY = true;

    Vector3 vel;

    void LateUpdate()
    {
        if (target == null || target == transform) return;

        Vector3 want = transform.position;
        if (followX) want.x = target.position.x + offset.x;
        if (followY) want.y = target.position.y + offset.y;

        // Z 轴不动 —— 2D 工程里 Z 决定渲染层级，跟过去反而会出怪事
        transform.position = smoothTime > 0.001f
            ? Vector3.SmoothDamp(transform.position, want, ref vel, smoothTime)
            : want;
    }

    /// <summary>给编辑器脚本用的直接绑定。</summary>
    public void Bind(Transform t)
    {
        target = t;
    }

    /// <summary>
    /// 目标为空时自动找玩家：先找名字正好是 player 的，再找名字里带 player / 玩家 的。
    /// 找到返回 true。运行时兜底用（编辑器里有向导会自动绑）。
    /// </summary>
    public bool AutoBind()
    {
        if (target != null) return true;

        var go = GameObject.Find("player");
        if (go == null) go = GameObject.Find("Player");
        if (go == null) go = GameObject.Find("玩家");
        if (go != null) { target = go.transform; return true; }

        foreach (var t in FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t == transform) continue;
            string n = t.name;
            if (n.ToLowerInvariant().Contains("player") || n.Contains("玩家"))
            {
                target = t;
                return true;
            }
        }
        return false;
    }
}
