using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Desktop_MusicButton : MonoBehaviour
{
	public TMP_Text musNameText;
	public string musName;
	public void OnClick()
	{
		Root.Instance.audio_Controller.PlayBGM(musName);
	}
}
