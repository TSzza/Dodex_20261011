using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Audio_Controller : MonoBehaviour
{
    // 管理音乐与音效播放的控制器

    // 开放音乐播放接口（默认有渐入渐出）和音效播放接口
    // PlayBGM PlaySFX StopBGM

    // 通过在inspector中此程序下预设的名字（ClipName）来切换播放音乐

    [Serializable]
    public class AudioClipEntry
    {
        public string clipName;
        public AudioClip clip;
    }

    public List<AudioClipEntry> bgmList;
    public List<AudioClipEntry> sfxList;
    [SerializeField][Min(0f)] private float bgmFadeDuration = 1.0f; // 音乐渐入渐出

    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public GameObject HomePageSfxSource;
    
    private float bgmTargetVolume=0.5f;
    [Header("玩家调节的音量")]
    public float bgmVolumeMult=0.5f;
    public float bgmVolumeSingleMult=1f; //每一首都重新设置
    public float sfxVolumeMult=1f;
    public bool isChangingBGM = false;
    public int nowBGMIndex;
    public string nextBGMName;
    private Coroutine bgmFadeCoroutine;

    public void PlayBGM(string clipName)
    {
        bgmVolumeSingleMult = 1f;
        AudioClipEntry entry = bgmList.Find(item => item.clipName == clipName);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"BGM：找不到名为【{clipName}】的音频");
            return;
        }
        nowBGMIndex = bgmList.FindIndex(item => item.clipName == clipName);

        // 停止正在进行的渐变
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        nextBGMName = clipName;
        bgmFadeCoroutine = StartCoroutine(CoroutineFadeBGM(entry.clip));
    }
    public void StopBGM()
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            isChangingBGM = false;
        }
        bgmFadeCoroutine = StartCoroutine(CoroutineFadeBGM(null));
    }
    
    public void SetBgmVolumeMult(float volume)
    {
        bgmVolumeMult = Mathf.Clamp01(volume);
        if (!isChangingBGM)
            bgmSource.volume = bgmVolumeMult*bgmVolumeSingleMult;
    }
    public void SetSfxVolumeMult(float volume)
    {
        sfxVolumeMult = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolumeMult;
        AudioSource[] audioSources = HomePageSfxSource.GetComponents<AudioSource>();
        foreach (var source in audioSources)
        {
            source.volume = sfxVolumeMult;
        }
    }

    public void PlaySFX(string clipName)
    {
        AudioClipEntry entry = sfxList.Find(item => item.clipName == clipName);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"SFX：找不到名为【{clipName}】的音频");
            return;
        }
        sfxSource.PlayOneShot(entry.clip);
    }

    private IEnumerator CoroutineFadeBGM(AudioClip nextClip)
    {
        isChangingBGM = true;

        // 第一步：渐出当前BGM
        float startVol = bgmSource.volume;
        float timePass = 0f;
        while (timePass < bgmFadeDuration)
        {
            timePass += Time.deltaTime;
            float t = timePass / bgmFadeDuration;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        bgmSource.volume = 0f;
        bgmSource.Stop();
        isChangingBGM = false;

        // 如果有下一段BGM，加载并渐入
        if (nextClip != null)
        {
            bgmSource.clip = nextClip;
            bgmSource.Play();
            timePass = 0f;
            while (timePass < bgmFadeDuration)
            {
                timePass += Time.deltaTime;
                float t = timePass / bgmFadeDuration;
                bgmSource.volume = Mathf.Lerp(0f, bgmTargetVolume*bgmVolumeMult*bgmVolumeSingleMult, t);
                yield return null;
            }
            bgmSource.volume = bgmTargetVolume*bgmVolumeMult*bgmVolumeSingleMult;
        }
    }
}
