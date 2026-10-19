using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_PlayMusic : Plot_Base
{
    public string musicName;
    public bool isBGM;

    protected override IEnumerator PlotCoroutine()
    {
        if(isBGM)
        {
            Root.Instance.audio_Controller.PlayBGM(musicName);
        }
        if(!isBGM)
        {
            Root.Instance.audio_Controller.PlaySFX(musicName);
        }
        yield return null;
    }
}
