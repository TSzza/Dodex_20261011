using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_ResetBeat :  Plot_Base
{
    public float BPM;

    protected override IEnumerator PlotCoroutine()
    {
        Root.Instance.game_Controller.beat_Manager.ResetBeat();
        Root.Instance.game_Controller.beat_Manager.bpm = BPM;
        yield return null;
    }
}
