using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Desktop_LetterFBX : MonoBehaviour
{
	public TMP_Text timeText, wordText;
	public bool NotAutoTime;
	private void Start()
	{
		if (!NotAutoTime)
		{
			// 获取系统当前时间
			DateTime now = DateTime.Now;
			// 24小时制，格式 HH:mm
			if (timeText != null)
				timeText.text = $"{now.Hour:D2}:{now.Minute:D2}";
		}
	}
}
