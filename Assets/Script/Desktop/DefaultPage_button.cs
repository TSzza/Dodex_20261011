using UnityEngine;

public class DefaultPage_button : MonoBehaviour
{
    public enum ButtonIconKind
    {
        MinimizeIcon,
        MaximizeIcon,
        CloseIcon,
        OpenIcon,
        Open_MinimizeIcon,
    }

    [Header("按钮图标种类")]
    public ButtonIconKind iconKind;

    [Header("画布类型设置")]
    [Tooltip("勾选：自动向上遍历父物体获取类型；取消勾选：手动在下方选择类型")]
    public bool autoFindCanvasType = true;
    public CanvasLayer_Manager.CanvasType myCanvasType;

    void Awake()
    {
        if (autoFindCanvasType)
        {
            DefaultPage_Manager pageManager = FindComponentInParentsRecursive<DefaultPage_Manager>(transform);
            if (pageManager != null)
            {
                myCanvasType = pageManager.myType;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: 向上遍历全部父物体，找不到DefaultPage_Manager");
            }
        }
    }

    // 给Unity Button的OnClick事件绑定这个函数
    public void OnClick()
    {
        CanvasLayer_Manager.CanvasType type = myCanvasType;

        switch (iconKind)
        {
            case ButtonIconKind.MinimizeIcon:
                CanvasLayer_Manager.Instance.Minimize(type);
                break;
            case ButtonIconKind.MaximizeIcon:
                {
                    DefaultPage_Manager targetPage = CanvasLayer_Manager.Instance.GetPageManagerByType(type);
                    if (targetPage != null)
                    {
                        if (targetPage.isMaximized)
                        {
                            // 已最大化，恢复原始大小
                            CanvasLayer_Manager.Instance.ApplyOriginal(type);
                        }
                        else
                        {
                            CanvasLayer_Manager.Instance.Maximize(type);
                        }
                    }
                    break;
                }
            case ButtonIconKind.CloseIcon:
                CanvasLayer_Manager.Instance.Close(type);
                break;
            case ButtonIconKind.OpenIcon:
                CanvasLayer_Manager.Instance.Open(type);
                break;
            case ButtonIconKind.Open_MinimizeIcon:
                {
                    DefaultPage_Manager targetPage = CanvasLayer_Manager.Instance.GetPageManagerByType(type);
                    if (targetPage != null)
                    {
                        if (targetPage.isMinimized)
                        {
                            // 已最小化，打开它
                            CanvasLayer_Manager.Instance.Open(type);
                        }
                        else
                        {
                            CanvasLayer_Manager.Instance.Minimize(type);
                        }
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// 从startTransform开始，一直向上遍历所有父节点，返回第一个找到的T组件
    /// </summary>
    private T FindComponentInParentsRecursive<T>(Transform startTransform) where T : Component
    {
        Transform current = startTransform.parent;
        while (current != null)
        {
            T comp = current.GetComponent<T>();
            if (comp != null)
            {
                return comp;
            }
            current = current.parent;
        }
        return null;
    }
}
