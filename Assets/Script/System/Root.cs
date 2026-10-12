using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root : MonoBehaviour
{
    // 用于联系各大控制器，拥有唯一单例
    // 用于触发全游戏的初始化
    
    //
    
    public System_Controller system_Controller;
    public Data_Controller data_Controller;
    public Audio_Controller audio_Controller;

    [HideInInspector]
    public float MouseMult = 0.4f; // 由于1920*1080变成了 192*4的宽，变成了0.4倍

     
    public static Root Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"Root Instance 初始化 MouseMult {MouseMult}");
    }

	private void Start() // 在此处触发各大系统的初始化
	{
        data_Controller.Init_Data();
	}

    private void OnApplicationQuit() // 退出时存档
    {
        data_Controller.Save_SaveData();
    }
}
