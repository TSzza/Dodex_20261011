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

    [SerializeField] private List<AudioClipEntry> bgmList;
    [SerializeField] private List<AudioClipEntry> sfxList;
    [SerializeField][Min(0f)] private float bgmFadeDuration = 1.0f; // 音乐渐入渐出

    [SerializeField]private AudioSource bgmSource;
    [SerializeField]private AudioSource sfxSource;

    private float bgmTargetVolume;
    private Coroutine bgmFadeCoroutine;

    public void PlayBGM(string clipName)
    {
        AudioClipEntry entry = bgmList.Find(item => item.clipName == clipName);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"BGM：找不到名为【{clipName}】的音频");
            return;
        }

        // 停止正在进行的渐变
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(CoroutineFadeBGM(entry.clip));
    }
    public void StopBGM()
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(CoroutineFadeBGM(null));
    }

    public void SetBgmVolume(float volume)
    {
        bgmTargetVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmTargetVolume;
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

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    private IEnumerator CoroutineFadeBGM(AudioClip nextClip)
    {
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
                bgmSource.volume = Mathf.Lerp(0f, bgmTargetVolume, t);
                yield return null;
            }
            bgmSource.volume = bgmTargetVolume;
        }
    }
}
