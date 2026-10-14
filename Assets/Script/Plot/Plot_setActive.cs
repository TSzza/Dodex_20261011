using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_setActive : Plot_Base
{
    public bool reverse = false;
    public List<GameObject> targetObjects;

    protected override IEnumerator PlotCoroutine()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(!reverse);
            }
        }
        yield return null;
    }
}