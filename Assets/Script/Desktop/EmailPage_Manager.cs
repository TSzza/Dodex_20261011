using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmailPage_Manager : DefaultPage_Manager
{
	public GameObject bossLetter, myLetter;
	public Transform LetterParent;
	public Button EnterButton;
	public string TargetString;
	public float TypeSpeed;
	public TMP_Text UnconcernedText, TypingText;
	public bool TypeActivated = false;
	private float processPoint = 0; // 文本输入进度
	private Coroutine cursorCoroutine;
	private bool showCursor = false;
	private bool hasInputStarted = false; // 标记玩家是否已经输入第一个字符

	public void SendBossLetter(string words)
	{
		GameObject newLetter = Instantiate(bossLetter, LetterParent);
		Desktop_LetterFBX letterComp = newLetter.GetComponent<Desktop_LetterFBX>();
		if (letterComp != null)
		{
			letterComp.wordText.text = words;
		}
	}

	public void SendMyLetter(string words)
	{
		GameObject newLetter = Instantiate(myLetter, LetterParent);
		Desktop_LetterFBX letterComp = newLetter.GetComponent<Desktop_LetterFBX>();
		if (letterComp != null)
		{
			letterComp.wordText.text = words;
		}
	}

	public void OnClickInterBoard()
	{
		TypeActivated = true;
		
		// 启动光标闪烁协程
		if (cursorCoroutine != null) StopCoroutine(cursorCoroutine);
		cursorCoroutine = StartCoroutine(CursorBlinkCoroutine());
	}

	protected override void Update()
	{
		base.Update();

		if (string.IsNullOrEmpty(TargetString))
		{
			UnconcernedText.text = "没有什么要回复的了……";
		}
		else if (TypeActivated == false)
		{
			UnconcernedText.text = "（点击输入文本）";
		}
		else if (processPoint == 0 && !hasInputStarted)
		{
			UnconcernedText.text = "（请输入文本）";
		}

		// Enter按钮可交互时，检测键盘Enter按键
		if (EnterButton.interactable && Input.GetKeyDown(KeyCode.Return))
		{
			OnClickEnter();
		}

		// 打字模拟：TypeActivated开启，且进度未满
		if (TypeActivated && processPoint < 1f && !string.IsNullOrEmpty(TargetString))
		{
			// 检测任意字母按键按下
			if (Input.anyKeyDown)
			{
				if (IsLetterKeyDown())
				{
					processPoint += TypeSpeed;
					processPoint = Mathf.Clamp01(processPoint);

					// 第一次按下字母，标记输入已开始，隐藏提示文本
					if (!hasInputStarted)
					{
						hasInputStarted = true;
						UnconcernedText.gameObject.SetActive(false);
					}
				}
			}

			// 根据进度截取文本
			int charCount = Mathf.FloorToInt(TargetString.Length * processPoint);
			string displayedText = TargetString.Substring(0, charCount);
			// 拼接光标
			TypingText.text = displayedText + (showCursor ? "|" : "");

			Debug.Log(processPoint);
			// 进度满，开启发送按钮
			if (processPoint >= 1f)
			{
				Debug.Log("AAA");

				TypingText.text = TargetString;
				EnterButton.interactable = true;
			}
		}
	}

	public void OnClickEnter()
	{
		EnterButton.interactable = false;
		SendMyLetter(TargetString);
		// 重置全部输入状态
		processPoint = 0;
		TypeActivated = false;
		hasInputStarted = false; // 重置输入标记
		if (cursorCoroutine != null)
		{
			StopCoroutine(cursorCoroutine);
			cursorCoroutine = null;
		}
		TargetString = "";
		TypingText.text = "";
		UnconcernedText.gameObject.SetActive(true); // 把提示重新打开
	}

	public void SetTargetString(string target)
	{
		TargetString = target;
		processPoint = 0;
		hasInputStarted = false; // 重置输入标记
		TypingText.text = "";
		EnterButton.interactable = false;
		UnconcernedText.gameObject.SetActive(true);
	}

	// 光标闪烁协程：1秒切换一次显示隐藏
	private IEnumerator CursorBlinkCoroutine()
	{
		while (TypeActivated)
		{
			showCursor = !showCursor;
			yield return new WaitForSeconds(0.5f);
		}
		showCursor = false;
	}

	// 判断是否按下A-Z字母键
	private bool IsLetterKeyDown()
	{
		for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
		{
			if (Input.GetKeyDown(k))
				return true;
		}
		return false;
	}
}
