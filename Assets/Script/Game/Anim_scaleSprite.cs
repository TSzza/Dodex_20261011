using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Anim_scaleSprite : MonoBehaviour
{
    private SpriteRenderer _sr;

    private Vector3 _originScale;
    private Color _originColor;

    private bool _isPlaying;
    private float _timer;

    public GameObject targetPos;

    [Header("动画总时长(秒)")]
    public float duration = 0.8f;

    [Header("缩放曲线：X=0~1(时间归一化), Y=缩放倍数")]
    public AnimationCurve scaleCurve;
    [Header("透明度曲线：X=0~1(时间归一化), Y=alpha 0~1")]
    public AnimationCurve alphaCurve;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        SaveOriginState();
    }

    private void SaveOriginState()
    {
        _originScale = transform.localScale;
        _originColor = _sr.color;
    }

    public void Play()
    {
        // 重置回初始状态
        transform.localScale = _originScale;

        if(targetPos!=null)
        {
            transform.position = targetPos.transform.position;
        }


        _sr.color = _originColor;

        _timer = 0f;
        _isPlaying = true;
    }

    void Update()
    {
        if (!_isPlaying) return;

        _timer += Time.deltaTime;
        // 重点：把真实时间缩放到曲线横轴 [0,1]
        float t = Mathf.Clamp01(_timer / duration);

        // 曲线求值，曲线X轴就是0~1
        float scaleFactor = scaleCurve.Evaluate(t);
        float alpha = alphaCurve.Evaluate(t);

        transform.localScale = _originScale * scaleFactor;
        _sr.color = new Color(_originColor.r, _originColor.g, _originColor.b, alpha);

        if (t >= 1f)
        {
            _isPlaying = false;
        }
    }
}
