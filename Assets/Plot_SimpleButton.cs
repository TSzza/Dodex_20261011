using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_SimpleButton : MonoBehaviour
{
	public Plot_Base plot;
	public void OnClick()
	{
        Root.Instance.audio_Controller.PlaySFX("bubble");

		plot.Play();
	}
}
