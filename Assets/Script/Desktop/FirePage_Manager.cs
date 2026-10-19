using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirePage_Manager : DefaultPage_Manager
{
	public GameObject dangerLetter;
	public Transform LetterParent;
	public TMP_Text countText;
	public TMP_Text proccessText;
	public GameObject loadingObject;
	public Button ScanButton;
	
	public int maxCount = 100;
	public Color colorLow = Color.white;
	public Color colorHigh = Color.red;

	private int storedDangerCount = 0;
	private Coroutine scanCoroutine;

	public void SendDangerLetter(string words)
	{
		storedDangerCount++;

		if (LetterParent.childCount >= maxCount)
		{
			int idx = storedDangerCount % maxCount;
			Transform child = LetterParent.GetChild(idx);
			Desktop_LetterFBX letterComp = child.GetComponent<Desktop_LetterFBX>();
			if (letterComp != null)
			{
				letterComp.wordText.text = words;
			}
		}
		else
		{
			GameObject newLetter = Instantiate(dangerLetter, LetterParent);
			Desktop_LetterFBX letterComp = newLetter.GetComponent<Desktop_LetterFBX>();
			if (letterComp != null)
			{
				letterComp.wordText.text = words;
			}
		}

		UpdateCountDisplay();
	}

	void UpdateCountDisplay()
	{
		countText.text = $"发现高危风险 {storedDangerCount} 条";
		float rawVal = storedDangerCount;
		Color targetColor;

		// 把亮红色作为中间过渡点，不硬编码其他颜色
		Color brightRed = Color.red;

		if (rawVal <= 10)
		{
			// 0~10：colorLow(白色) → brightRed(鲜红色)
			float t = Mathf.Clamp01(rawVal / 10f);
			Color.RGBToHSV(colorLow, out float h0, out float s0, out float v0);
			Color.RGBToHSV(brightRed, out float h1, out float s1, out float v1);
			float h = Mathf.Lerp(h0, h1, t);
			float s = Mathf.Lerp(s0, s1, t);
			float v = Mathf.Lerp(v0, v1, t);
			targetColor = Color.HSVToRGB(h, s, v);
		}
		else if (rawVal <= 20)
		{
			//10~20：brightRed(鲜红色) → colorHigh(暗红色)
			float t = Mathf.Clamp01((rawVal - 10f) / 10f);
			Color.RGBToHSV(brightRed, out float h0, out float s0, out float v0);
			Color.RGBToHSV(colorHigh, out float h1, out float s1, out float v1);
			float h = Mathf.Lerp(h0, h1, t);
			float s = Mathf.Lerp(s0, s1, t);
			float v = Mathf.Lerp(v0, v1, t);
			targetColor = Color.HSVToRGB(h, s, v);
		}
		else
		{
			// >20 保持 colorHigh
			targetColor = colorHigh;
		}
		countText.color = targetColor;
	}

	public void OnClickScan()
	{
		if (scanCoroutine != null) return;
		scanCoroutine = StartCoroutine(ScanCoroutine());
	}

	private IEnumerator ScanCoroutine()
	{
		ScanButton.interactable = false;
		proccessText.text = "正在查杀……";
		proccessText.gameObject.SetActive(true);
		loadingObject.SetActive(true);

		float totalTime = 10f;
		float timer = 0f;

		while (timer < totalTime && storedDangerCount > 0)
		{
			// 随机间隔：0.25 ~ 1秒
			float waitTime = Random.Range(0.25f, 1f);
			yield return new WaitForSeconds(waitTime);

			timer += waitTime;

			if (storedDangerCount > maxCount)
			{
				storedDangerCount--;
			}
			else
			{
				if (LetterParent.childCount > 0)
				{
					Transform firstChild = LetterParent.GetChild(0);
					Destroy(firstChild.gameObject);
				}
				storedDangerCount--;
			}
			UpdateCountDisplay();

			// loading：每隔一段时间直接旋转45度，不是每帧旋转
			loadingObject.transform.Rotate(0,0,-45f);
		}

		proccessText.text = "查杀结束";
		loadingObject.SetActive(false);
		yield return new WaitForSeconds(2f);
		ScanButton.interactable = true;
		proccessText.gameObject.SetActive(false);
		scanCoroutine = null;
	}

	protected override void Update()
	{
		base.Update();
		if(LetterParent.childCount > storedDangerCount)
		{
			storedDangerCount = LetterParent.childCount;
			UpdateCountDisplay();
		}
	}
}
