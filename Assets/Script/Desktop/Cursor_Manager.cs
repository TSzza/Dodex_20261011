using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 光标类型枚举，和你图片名字一一对应
public enum CursorType
{
    busy,
    dgn1,
    dgn2,
    help,
    horz,
    link,
    pin,
    pointer,
    text,
    unavailable,
    vert,
    work
}

public class Cursor_Manager : MonoBehaviour
{
    // 单例
    public static Cursor_Manager Instance { get; private set; }

    [Header("光标UI物体")]
    public Image cursorImage;
    public RectTransform _cursorRt;
    public bool HideCursor = false;
    public Vector2 defaultPos = new Vector2(60, 0);
    public Vector2 upPos = new Vector2(60, 30);
    public Vector2 leftPos = new Vector2(30, 0);
    public Vector2 midPos = new Vector2(30, 30);

    [Header("所有光标贴图，顺序和枚举保持一致")]
    public Texture2D busyTex;
    public Texture2D dgn1Tex;
    public Texture2D dgn2Tex;
    public Texture2D helpTex;
    public Texture2D horzTex;
    public Texture2D linkTex;
    public Texture2D pinTex;
    public Texture2D pointerTex;
    public Texture2D textTex;
    public Texture2D unavailableTex;
    public Texture2D vertTex;
    public Texture2D workTex;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //_cursorRt = cursorImage.GetComponent<RectTransform>();

        // 隐藏系统原生鼠标
        if (HideCursor)
        {
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // 跟随鼠标位置
        _cursorRt.anchoredPosition = Input.mousePosition;

        //Debug.Log(Input.mousePosition);
    }

    // 外部调用这个函数切换光标
    public void SetCursorType(CursorType type)
    {
        Sprite targetSprite = GetSpriteByType(type);

        // 微调位置
        
        if (type == CursorType.dgn1 || type == CursorType.dgn2 )
            cursorImage.GetComponent<RectTransform>().anchoredPosition = midPos;
        else if (type == CursorType.horz)
            cursorImage.GetComponent<RectTransform>().anchoredPosition = leftPos;
        else if (type == CursorType.vert)
            cursorImage.GetComponent<RectTransform>().anchoredPosition = upPos;
        else
            cursorImage.GetComponent<RectTransform>().anchoredPosition = defaultPos;
        
        if (targetSprite != null)
        {
            cursorImage.sprite = targetSprite;
        }
    }

    private Sprite GetSpriteByType(CursorType type)
    {
        Texture2D tex = type switch
        {
            CursorType.busy => busyTex,
            CursorType.dgn1 => dgn1Tex,
            CursorType.dgn2 => dgn2Tex,
            CursorType.help => helpTex,
            CursorType.horz => horzTex,
            CursorType.link => linkTex,
            CursorType.pin => pinTex,
            CursorType.pointer => pointerTex,
            CursorType.text => textTex,
            CursorType.unavailable => unavailableTex,
            CursorType.vert => vertTex,
            CursorType.work => workTex,
            _ => pointerTex // 默认pointer
        };
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0,0));
    }

    void OnDestroy()
    {
        // 退出时恢复系统鼠标
        Cursor.visible = true;
    }
}
