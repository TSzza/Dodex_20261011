using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data;
using System;
using System.IO;
using System.Text;

public class Data_Controller : MonoBehaviour
{
    // 负责游戏数据的 读取 与 存储（包括表格的数据读取），包括存档和读档
    // 指定 UTF - 8 编码，表格内不能有回车
    

    public void SetPersistentDataPath() { SavePath = Path.Combine(Application.persistentDataPath, "Dodex.save"); }
    string SavePath;


    public void Init_Data()
    {
        Read_SaveData();

    }
    public void Read_SaveData()
    {
        SetPersistentDataPath();
        System_Controller sys = Root.Instance.system_Controller;
        Vector2Int Res = new Vector2Int(1920, 1080);
        bool Full = sys.DefaultFullscreen;

        if (!File.Exists(SavePath))
        {
            Debug.Log("没有找到存档");
        }
        else
        {

            string json = File.ReadAllText(SavePath, Encoding.UTF8);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Res.x = data.screenWidth;
            Res.y = data.screenHeight;
            Full = data.fullScreen;
            Root.Instance.DelaySet = data.delaySet;
        }
        sys.SetResolution(Res.x, Res.y);
        sys.SetFullScreen(Full);
    }
    public void Save_SaveData()
    {
        SaveData data = new SaveData
        {
            screenWidth = Screen.width,
            screenHeight = Screen.height,
            fullScreen = (Screen.fullScreenMode != FullScreenMode.Windowed),
            delaySet = Root.Instance.DelaySet,
        };
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json, Encoding.UTF8);
        Debug.Log($"画面设置已保存");
    }

}
