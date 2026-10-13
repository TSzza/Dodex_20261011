using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_delayTime : Plot_Base
{
    public float delay = 1f;

    protected override IEnumerator PlotCoroutine()
    {
        yield return new WaitForSeconds(delay);
    }
}
