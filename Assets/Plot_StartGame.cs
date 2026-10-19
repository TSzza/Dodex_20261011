using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_StartGame : Plot_Base
{
	public GameObject myGame;
	public float Scale = 1f;
	public Camera gameCamera;
	public GameObject beatManager;
	public float MusicMult = 1f;
	public Plot_Base nextLevelPlot;
	
    protected override IEnumerator PlotCoroutine()
	{
		Root.Instance.game_Controller.nowLevel = myGame;
		myGame.SetActive(false);
		myGame.transform.localPosition = Vector3.zero;
		myGame.SetActive(true);
		beatManager.transform.localScale = Vector3.one * Scale;
		gameCamera.orthographicSize = 5.4f * Scale;
		Root.Instance.audio_Controller.bgmVolumeSingleMult = MusicMult;
        yield return null;
		Root.Instance.game_Controller.StartLevel();
		Root.Instance.game_Controller.nextLevelPlot = nextLevelPlot;
	}
}
