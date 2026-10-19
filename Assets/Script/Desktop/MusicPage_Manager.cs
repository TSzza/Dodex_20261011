using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Audio_Controller;

public class MusicPage_Manager : DefaultPage_Manager
{
	public Transform musButtonParent;
	public GameObject musButtonFBX;
	protected override void Start()
	{
		base.Start();
        
		foreach (var entry in Audio.bgmList) // 生成音乐按钮
        {
            //Debug.Log(entry.clipName);

            GameObject newBtn = Instantiate(musButtonFBX, musButtonParent);
            
            Desktop_MusicButton btnComp = newBtn.GetComponent<Desktop_MusicButton>();
            if (btnComp != null)
            {
                btnComp.musName = entry.clipName;
                btnComp.musNameText.text = entry.clipName+".mp3";
            }
        }
	}
    public GameObject Disk;
    public TMP_Text musName;
    public TMP_Text musTime;
    public Scrollbar musbar;
    public Sprite Play;
    public Sprite Pause;
    public Image PlayOrPauseImage;

    [Header("音频控制器与歌单")]
    public float diskRotateSpeed = -60f; // 逆时针 Z旋转，负数

    protected override void Update()
    {
        base.Update();
        if (Audio.bgmList == null || Audio.bgmList.Count == 0 || Audio.bgmSource.clip == null)
        {
            // 无音频时，圆盘停止
            if(Disk != null) Disk.transform.Rotate(0,0,0);
            return;
        }

        // 圆盘旋转：播放中才旋转
        if (Audio.bgmSource.isPlaying)
        {
            if(Disk != null)
                Disk.transform.Rotate(0, 0, diskRotateSpeed * Time.deltaTime);
        }

        if (!Audio.isChangingBGM)
        {
            // 获取时长数据
            float total = Audio.bgmSource.clip.length;
            float current = Audio.bgmSource.time;
            float progress = total > 0 ? current / total : 0f;

            // 更新进度条 size
            musbar.size = progress;

            // 时间格式化：XX:XX
            string currentStr = FormatSeconds(current);
            string totalStr = FormatSeconds(total);
            musTime.text = $"{currentStr} / {totalStr}";

            // 更新歌名文本
            musName.text = Audio.bgmList[Audio.nowBGMIndex].clipName;
        }
        else
        {
            musName.text = Audio.nextBGMName;
        }



        // 切换播放/暂停图标
        if (!Audio.bgmSource.isPlaying)
        {
            PlayOrPauseImage.sprite = Play;
        }
        else
        {
            PlayOrPauseImage.sprite = Pause;
        }
    }

    // 播放/暂停切换
    public void PlayOrPause()
    {
        if (Audio.bgmList.Count == 0) return;

        if (Audio.isChangingBGM)
            return;
        if (Audio.bgmSource.isPlaying)
        {
            Debug.Log("Pause");
            Audio.bgmSource.Pause();
        }
        else
        {
            if (Audio.bgmSource.clip == null)
            {
                PlayTrack(Audio.nowBGMIndex);
            }
            else
            {
                Audio.bgmSource.UnPause();
            }
        }
    }

    // 下一首
    public void PlayNext()
    {
        if (Audio.bgmList.Count == 0) return;
        Audio.nowBGMIndex++;
        if (Audio.nowBGMIndex >= Audio.bgmList.Count)
            Audio.nowBGMIndex = 0;
        PlayTrack(Audio.nowBGMIndex);
    }

    // 上一首
    public void PlayBefore()
    {
        if (Audio.bgmList.Count == 0) return;
        Audio.nowBGMIndex--;
        if (Audio.nowBGMIndex < 0)
            Audio.nowBGMIndex = Audio.bgmList.Count - 1;
        PlayTrack(Audio.nowBGMIndex);
    }

    // 播放指定索引曲目
    void PlayTrack(int index)
    {
        var entry = Audio.bgmList[index];

        Debug.Log($"Play BGM {entry.clipName}");
        Audio.PlayBGM(entry.clipName);
    }

    // 秒数转 MM:SS
    private string FormatSeconds(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60);
        int sec = Mathf.FloorToInt(seconds % 60);
        return $"{min:D2}:{sec:D2}";
    }
}
