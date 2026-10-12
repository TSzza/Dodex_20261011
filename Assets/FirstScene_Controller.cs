using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class FirstScene_Controller : MonoBehaviour
{
    [Header("BKG_fore 淡入淡出参数")]
    public Image _bkgFore;
    public float foreWaitTime = 2f;
    public float foreFadeDuration = 1.5f;
    [Tooltip("BKG_fore淡出到alpha0后，必须保持透明的最小等待时间")]
    public float foreMinHoldZeroTime = 1.0f;

    [Header("BKG_down 淡入淡出参数")]
    public Image _bkgDown;
    public float downWaitTime = 3f;
    public float downFadeDuration = 2f;

    [Header("场景设置")]
    public TMP_Text _loadingText;
    public string mainSceneName = "MainScene";

    private AsyncOperation _sceneLoadOp;
    private bool _isForeFadeOutComplete; // BKG_fore已经完成淡出到0
    private float _foreFadeOutFinishTime; // BKG_fore淡出完成的时间戳

    void Start()
    {
        // BKG_fore初始透明度满
        if (_bkgFore != null)
        {
            Color c = _bkgFore.color;
            c.a = 1f;
            _bkgFore.color = c;
        }
        if (_bkgDown != null)
        {
            Color c = _bkgDown.color;
            c.a = 1f;
            _bkgDown.color = c;
        }

        _isForeFadeOutComplete = false;
        _foreFadeOutFinishTime = 0f;

        StartCoroutine(LoadMainSceneCoroutine());
        StartCoroutine(FadeOutBKGFore());
        StartCoroutine(FadeOutBKGDown());
    }

    // BKG_fore：等待 -> 线性淡出到0，标记完成时间
    private IEnumerator FadeOutBKGFore()
    {
        if (_bkgFore == null) yield break;
        yield return new WaitForSeconds(foreWaitTime);

        Color col = _bkgFore.color;
        float timer = 0f;
        while (timer < foreFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / foreFadeDuration;
            col.a = Mathf.Lerp(1f, 0f, t);
            _bkgFore.color = col;
            yield return null;
        }
        col.a = 0f;
        _bkgFore.color = col;

        // 记录淡出完成
        _isForeFadeOutComplete = true;
        _foreFadeOutFinishTime = Time.time;
    }

    // BKG_down：等待 -> 线性淡出到0
    private IEnumerator FadeOutBKGDown()
    {
        if (_bkgDown == null) yield break;
        yield return new WaitForSeconds(downWaitTime);

        Color col = _bkgDown.color;
        float timer = 0f;
        while (timer < downFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / downFadeDuration;
            col.a = Mathf.Lerp(1f, 0f, t);
            _bkgDown.color = col;
            yield return null;
        }
        col.a = 0f;
        _bkgDown.color = col;
    }

    // 加载主场景，更新进度
    private IEnumerator LoadMainSceneCoroutine()
    {
        _sceneLoadOp = SceneManager.LoadSceneAsync(mainSceneName);
        _sceneLoadOp.allowSceneActivation = false;

        while (!_sceneLoadOp.isDone)
        {
            float progress = Mathf.Clamp01(_sceneLoadOp.progress / 0.9f);
            int percent = Mathf.RoundToInt(progress * 100);
            if (_loadingText != null)
            {
                _loadingText.text = $"LOADING ... {percent}%";
            }

            if (_sceneLoadOp.progress >= 0.9f)
            {
                if (_loadingText != null)
                    _loadingText.text = "LOADING ... 100%";

                // 【核心改动】等待两个条件：
                // 1. BKG_fore已经淡出到0
                // 2. 已经满足最小hold透明时间
                yield return new WaitUntil(() =>
                {
                    return _isForeFadeOutComplete && (Time.time - _foreFadeOutFinishTime >= foreMinHoldZeroTime);
                });

                // 开始淡入BKG_fore，必须等淡入完全结束，才激活场景
                yield return StartCoroutine(FadeInBKGFore());
                _sceneLoadOp.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    // BKG_fore淡入到alpha=1
    private IEnumerator FadeInBKGFore()
    {
        if (_bkgFore == null) yield break;
        Color col = _bkgFore.color;
        float timer = 0f;
        while (timer < foreFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / foreFadeDuration;
            col.a = Mathf.Lerp(0f, 1f, t);
            _bkgFore.color = col;
            yield return null;
        }
        col.a = 1f;
        _bkgFore.color = col;
    }
}
