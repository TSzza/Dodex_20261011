using System.Collections;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

public class System_Controller : MonoBehaviour
{
    // 负责进行游戏 exe 以及系统有关的操作，不包括文件的读取。

    public List<Vector2Int> supportedResolutions = new List<Vector2Int>()
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };
    public bool DefaultFullscreen = true;

    // 修改分辨率（数据会进入存档）
    public bool SetResolution(int width, int height)
    {
        // 必须是预定义分辨率
        bool exist = supportedResolutions.Exists(r => r.x == width && r.y == height);
        if (!exist)
        {
            Debug.LogWarning($"分辨率 {width}x{height} 不在预设列表内");
            return false;
        }
        return true;
    }

    // 全屏 / 窗口化（数据会进入存档）
    public void SetFullScreen(bool full)
    {
        if (full)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}
