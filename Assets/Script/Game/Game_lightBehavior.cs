using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_lightBehavior : MonoBehaviour
{
    public Vector3 moveDir;
    public float moveSpeed;
    public float maxDistance = 10f;

    private CircleCollider2D _circleCollider;
    private float _traveledDistance;
    private bool isEnd;

    protected virtual void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        if (_circleCollider == null)
        {
            Debug.LogWarning($"[{name}] Game_lightBehavior 缺少CircleCollider2D组件");
        }
    }
    public void ShootLight(Vector3 Pos,Vector2 Dir,float Speed,float MaxDistance,bool isend)
    {
        transform.position = Pos;
        moveDir = Dir.normalized; // 归一化
        moveSpeed = Speed;
        maxDistance = MaxDistance;
        _traveledDistance = 0f;
        isEnd = isend;
        if (isEnd)
        {
            this.transform.localScale *= 6;
            moveSpeed /= 2;
            maxDistance *= 4;
        }

        if (moveDir.magnitude < Mathf.Epsilon || moveSpeed < Mathf.Epsilon || maxDistance < Mathf.Epsilon)
        {
            Debug.LogWarning($"[{name}] Game_lightBehavior：运动方向/速度/最远距离存在0值");
        }
    }
	protected virtual void Update()
    {
        // 无效参数直接跳过更新
        if (moveDir.magnitude < Mathf.Epsilon || moveSpeed < Mathf.Epsilon || maxDistance < Mathf.Epsilon)
            return;

        float delta = moveSpeed * Time.deltaTime;

        // 更新位置与累计移动距离
        _traveledDistance += delta;
        transform.position += moveDir * delta;

        // 超出最大距离，销毁自身
        if (_traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        // 检测所有trigger碰撞体，判断重叠
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _circleCollider.radius);
        foreach (var hit in hits)
        {
            // 跳过自身collider
            if (hit == _circleCollider) continue;

            if (hit.TryGetComponent<Game_objectBehavior>(out var objBehavior))
            {
                HandleHitObject(objBehavior);
                //break; // 不只响应第一个命中物体
            }
        }
    }

    protected virtual void HandleHitObject(Game_objectBehavior obj)
    {
        switch (obj.myType)
        {
            case ObjectType.block:
                if (!isEnd)
                {
                    obj.LightUp();
                    Destroy(gameObject);
                }
                else
                {
                    obj.LightUpEnd();
                }
                break;
            case ObjectType.tile:
            case ObjectType.flag:
            case ObjectType.enemy:
                if (isEnd)
                    obj.LightUpEnd();
                else
                    obj.LightUp();
                break;
            case ObjectType.spawn: // 永不点亮
                break;
            case ObjectType.none:
                Debug.LogWarning("hit none objectType");
                break;
        }
    }
}
