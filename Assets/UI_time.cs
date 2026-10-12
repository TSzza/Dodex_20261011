using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_time : MonoBehaviour
{
    public TMP_Text timeText;
    public TMP_Text dateText;

    private float blinkTimer;
    private bool colonVisible = true;
    private int lastSecond;

    void Update()
    {
        DateTime now = DateTime.Now;

        // 在整秒的时候切换冒号闪烁，严格按现实秒数，不依赖Update累计时间
        if (now.Second != lastSecond)
        {
            lastSecond = now.Second;
            colonVisible = !colonVisible;
        }

        string ampm;
        int displayHour = now.Hour;

        if (displayHour < 12)
        {
            ampm = "AM";
        }
        else
        {
            ampm = "PM";
        }

        string colonChar = colonVisible ? ":" : " ";
        string timeStr = $"{displayHour}{colonChar}{now.Minute:D2} {ampm}";
        timeText.text = timeStr;

        string dateStr = $"{now.Year}/{now.Month:D2}/{now.Day:D2}";
        dateText.text = dateStr;
    }
}
