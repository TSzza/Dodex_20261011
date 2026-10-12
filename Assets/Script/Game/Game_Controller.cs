using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_Controller : MonoBehaviour
{
    public GameObject MainCircle; // 主人公，在Inspector拖入物体
    public float GridSize = 1f; // 一格距离
    public float moveDuration = 0.2f; // 移动耗时
    private bool isMoving = false; // 防止连续移动
    
    public enum Score
    {
        perfect,good,early,late,miss,
    }
    public Beat_Manager beat_Manager;
    public float perfectTime = 25f;
    public float goodTime = 50f;
    public float lateTime = 120f;

    void Update()
    {



        if (!isMoving)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                StartCoroutine(MoveTarget(Vector3.right * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                StartCoroutine(MoveTarget(Vector3.left * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                StartCoroutine(MoveTarget(Vector3.up * GridSize));
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                StartCoroutine(MoveTarget(Vector3.down * GridSize));
            }
        }
    }

    public float jumpHeight=0.2f;
    public Anim_floatText anim_FloatText;
    public Anim_scaleSprite pulse_FloatText;
    /// <summary>
    /// 主人公移动时的协程
    /// </summary>
    IEnumerator MoveTarget(Vector3 offset)
    {
        // 开始移动：先播放动画并进行判定

        float deltaTime = beat_Manager.GetMsToNearestBeat();
        Score score = Score.perfect;
        if (Mathf.Abs(deltaTime) <= perfectTime)
            score = Score.perfect;
        else if (Mathf.Abs(deltaTime) <= goodTime)
            score = Score.good;
        else if (Mathf.Abs(deltaTime) <= lateTime&& deltaTime>0)
            score = Score.late;
        else if (Mathf.Abs(deltaTime) <= lateTime&& deltaTime<0)
            score = Score.early;
        else
            score = Score.miss;

        anim_FloatText.Play(score);
        pulse_FloatText.Play();
        if (score == Score.miss)
        {
            yield break;
        }

        isMoving = true;
        Vector3 startPos = MainCircle.transform.position;
        Vector3 endPos = startPos + offset;
        float timer = 0f;
        float height = jumpHeight; // 抛物线最大高度，按需调整

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);

            // 水平：EaseOut 先快后慢
            float tEase = 1 - Mathf.Pow(1 - t, 2);
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, tEase);

            // Y抛物线：t=0.5时到达最高点
            float parabolaY = height * 4 * t * (1 - t);

            // 合成最终位置
            MainCircle.transform.position = new Vector3(
                horizontalPos.x,
                startPos.y + parabolaY,
                horizontalPos.z
            );

            yield return null;
        }

        MainCircle.transform.position = endPos;
        isMoving = false;
    }
}
