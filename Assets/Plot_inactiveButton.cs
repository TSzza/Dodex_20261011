using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Plot_inactiveButton : Plot_Base
{
    public bool SetInactive = false;
    public List<Button> targetObjects;

    protected override IEnumerator PlotCoroutine()
    {
        foreach (Button obj in targetObjects)
        {
            if (obj != null)
            {
                obj.interactable = !SetInactive;
            }
        }
        yield return null;
    }
}
